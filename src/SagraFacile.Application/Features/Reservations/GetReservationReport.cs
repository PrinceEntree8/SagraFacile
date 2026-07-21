using System.Collections.Immutable;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class GetReservationReport
{
    public record Query(int? EventId = null) : IQuery<Result>;
    public record Result(List<ReportDto> Reports, StatisticsDto Statistics);

    public record ReportDto(
        int Id,
        int SequenceNumber,
        string CustomerName,
        int PartySize,
        ReservationStatus Status,
        DateTime CreatedAt,
        DateTime? FirstCalledAt,
        DateTime? SeatedAt,
        DateTime? VoidedAt,
        int CallCount,
        TimeSpan? WaitTimeUntilFirstCall,
        TimeSpan? TotalWaitTime);

    public record StatisticsDto(
        int TotalReservations,
        int TotalPeople,
        int SeatedCount,
        int VoidedCount,
        int WaitingCount,
        TimeSpan AverageWaitTime,
        TimeSpan MedianWaitTime,
        TimeSpan? MaxWaitTime,
        TimeSpan? MinWaitTime);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly IReservationRepository _repository;
        private readonly IEventRepository _eventRepository;

        public Handler(IReservationRepository repository, IEventRepository eventRepository)
        {
            _repository = repository;
            _eventRepository = eventRepository;
        }

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            DateTime? startUtc = null;
            DateTime? endUtc = null;

            if (query.EventId.HasValue)
            {
                var selectedEvent = await _eventRepository.GetByIdAsync(query.EventId.Value, cancellationToken);
                if (selectedEvent is not null)
                {
                    (startUtc, endUtc) = GetUtcDayRange(selectedEvent.Date);
                }
            }

            var reservations = await _repository.GetByDateRangeAsync(
                query.EventId, 
                startUtc, 
                endUtc, 
                ReservationStatusFilter.AllCompleted, 
                cancellationToken);

            var reports = reservations.Select(r =>
            {
                var createdAt = r.CreatedAt.AsUtc();
                var firstCalledAt = r.FirstCalledAt.AsUtc();
                var seatedAt = r.SeatedAt.AsUtc();
                var voidedAt = r.VoidedAt.AsUtc();

                var waitUntilFirstCall = r.FirstCalledAt.HasValue
                    ? firstCalledAt!.Value - createdAt
                    : (TimeSpan?)null;

                TimeSpan? totalWait;
                if (seatedAt.HasValue)
                    totalWait = seatedAt.Value - createdAt;
                else if (voidedAt.HasValue)
                    totalWait = voidedAt.Value - createdAt;
                else
                    totalWait = null;

                return new ReportDto(
                    r.Id, r.SequenceNumber, r.CustomerName, r.PartySize, r.Status,
                    createdAt, firstCalledAt, seatedAt, voidedAt,
                    r.CallCount, waitUntilFirstCall, totalWait);
            }).ToList();

            var waitTimes = reports
                .Where(r => r is { Status: ReservationStatus.Seated, TotalWaitTime: not null })
                .Select(r => r.TotalWaitTime!.Value)
                .ToImmutableHashSet();

            TimeSpan averageWaitTime;
            TimeSpan medianWaitTime;
            TimeSpan? maxWaitTime;
            TimeSpan? minWaitTime;

            if (waitTimes.Any())
            {
                averageWaitTime = TimeSpan.FromTicks((long)waitTimes.Average(t => t.Ticks));
                medianWaitTime = GetMedian(waitTimes);
                maxWaitTime = waitTimes.Max();
                minWaitTime = waitTimes.Min();
            }
            else
            {
                averageWaitTime = TimeSpan.Zero;
                medianWaitTime = TimeSpan.Zero;
                maxWaitTime = null;
                minWaitTime = null;
            }

            var stats = new StatisticsDto(
                TotalReservations: reservations.Count,
                TotalPeople: reservations.Sum(r => r.PartySize),
                SeatedCount: reservations.Count(r => ReservationStatusFilter.AllCompleted.ToStatusArray().Contains(r.Status)),
                VoidedCount: reservations.Count(r => r.Status == ReservationStatus.Voided),
                WaitingCount: reservations.Count(r => ReservationStatusFilter.AllWaiting.ToStatusArray().Contains(r.Status)),
                AverageWaitTime: averageWaitTime,
                MedianWaitTime: medianWaitTime,
                MaxWaitTime: maxWaitTime,
                MinWaitTime: minWaitTime);

            return new Result(reports, stats);
        }

        private static TimeSpan GetMedian(ISet<TimeSpan> values)
        {
            var sorted = values.OrderBy(t => t.Ticks).ToList();
            var count = sorted.Count;
            if (count % 2 != 0) return sorted[count / 2];
            var mid1 = sorted[count / 2 - 1];
            var mid2 = sorted[count / 2];
            return TimeSpan.FromTicks((mid1.Ticks + mid2.Ticks) / 2);
        }

        private static (DateTime StartUtc, DateTime EndUtc) GetUtcDayRange(DateTime day)
        {
            var startUtc = DateTime.SpecifyKind(day.Date, DateTimeKind.Utc);
            var endUtc = startUtc.AddDays(1).AddTicks(-1);
            return (startUtc, endUtc);
        }
    }
}
