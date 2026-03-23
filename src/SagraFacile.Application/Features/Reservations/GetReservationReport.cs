using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Reservations;

public static class GetReservationReport
{
    public record Query(DateTime? StartDate = null, DateTime? EndDate = null) : IQuery<Result>;
    public record Result(List<ReportDto> Reports, StatisticsDto Statistics);

    public record ReportDto(
        int Id,
        string QueueNumber,
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

        public Handler(IReservationRepository repository) => _repository = repository;

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            // Normalise dates to UTC before delegating to the repository
            DateTime? startUtc = query.StartDate.HasValue
                ? query.StartDate.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(query.StartDate.Value, DateTimeKind.Utc)
                    : query.StartDate.Value.ToUniversalTime()
                : null;

            DateTime? endUtc = query.EndDate.HasValue
                ? query.EndDate.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(query.EndDate.Value, DateTimeKind.Utc)
                    : query.EndDate.Value.ToUniversalTime()
                : null;

            var reservations = await _repository.GetByDateRangeAsync(startUtc, endUtc, cancellationToken);

            var reports = reservations.Select(r =>
            {
                var waitUntilFirstCall = r.FirstCalledAt.HasValue
                    ? r.FirstCalledAt.Value - r.CreatedAt
                    : (TimeSpan?)null;

                TimeSpan? totalWait = r.SeatedAt.HasValue
                    ? r.SeatedAt.Value - r.CreatedAt
                    : r.VoidedAt.HasValue
                        ? r.VoidedAt.Value - r.CreatedAt
                        : null;

                return new ReportDto(
                    r.Id, r.QueueNumber, r.CustomerName, r.PartySize, r.Status,
                    r.CreatedAt, r.FirstCalledAt, r.SeatedAt, r.VoidedAt,
                    r.CallCount, waitUntilFirstCall, totalWait);
            }).ToList();

            var waitTimes = reports
                .Where(r => r.Status == "Seated" && r.TotalWaitTime.HasValue)
                .Select(r => r.TotalWaitTime!.Value)
                .ToList();

            var stats = new StatisticsDto(
                TotalReservations: reservations.Count,
                SeatedCount: reservations.Count(r => r.Status == "Seated"),
                VoidedCount: reservations.Count(r => r.Status == "Voided"),
                WaitingCount: reservations.Count(r => r.Status is "Waiting" or "Called"),
                AverageWaitTime: waitTimes.Any()
                    ? TimeSpan.FromTicks((long)waitTimes.Average(t => t.Ticks))
                    : TimeSpan.Zero,
                MedianWaitTime: waitTimes.Any() ? GetMedian(waitTimes) : TimeSpan.Zero,
                MaxWaitTime: waitTimes.Any() ? waitTimes.Max() : null,
                MinWaitTime: waitTimes.Any() ? waitTimes.Min() : null);

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
    }
}
