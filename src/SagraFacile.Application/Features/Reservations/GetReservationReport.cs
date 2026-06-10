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
        string Status,
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

            var reservations = await _repository.GetByDateRangeAsync(query.EventId, startUtc, endUtc, ReservationStatusFilter.AllCompleted, cancellationToken);

            var reports = reservations.Select(r =>
            {
                var waitUntilFirstCall = r.FirstCalledAt.HasValue
                    ? r.FirstCalledAt.Value - r.CreatedAt
                    : (TimeSpan?)null;

                TimeSpan? totalWait;
                if (r.SeatedAt.HasValue)
                    totalWait = r.SeatedAt.Value - r.CreatedAt;
                else if (r.VoidedAt.HasValue)
                    totalWait = r.VoidedAt.Value - r.CreatedAt;
                else
                    totalWait = null;

                return new ReportDto(
                    r.Id, r.SequenceNumber, r.CustomerName, r.PartySize, r.Status.ToString(),
                    r.CreatedAt, r.FirstCalledAt, r.SeatedAt, r.VoidedAt,
                    r.CallCount, waitUntilFirstCall, totalWait);
            }).ToList();

            var waitTimes = reports
                .Where(r => r is { Status: nameof(ReservationStatus.Seated), TotalWaitTime: not null })
                .Select(r => r.TotalWaitTime!.Value)
                .ToList();

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
                SeatedCount: reservations.Count(r => r.Status == ReservationStatus.Seated),
                VoidedCount: reservations.Count(r => r.Status == ReservationStatus.Voided),
                WaitingCount: reservations.Count(r => r.Status is ReservationStatus.Waiting or ReservationStatus.Called),
                AverageWaitTime: averageWaitTime,
                MedianWaitTime: medianWaitTime,
                MaxWaitTime: maxWaitTime,
                MinWaitTime: minWaitTime);

            return new Result(reports, stats);
        }

        private static TimeSpan GetMedian(List<TimeSpan> values)
        {
            var sorted = values.OrderBy(t => t.Ticks).ToList();
            var count = sorted.Count;
            if (count % 2 == 0)
            {
                var mid1 = sorted[count / 2 - 1];
                var mid2 = sorted[count / 2];
                return TimeSpan.FromTicks((mid1.Ticks + mid2.Ticks) / 2);
            }
            return sorted[count / 2];
        }

        private static (DateTime StartUtc, DateTime EndUtc) GetUtcDayRange(DateTime day)
        {
            var dateOnly = day.Date;
            var startUtc = day.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc)
                : dateOnly.ToUniversalTime();
            var endUtc = startUtc.AddDays(1).AddTicks(-1);
            return (startUtc, endUtc);
        }
    }
}
