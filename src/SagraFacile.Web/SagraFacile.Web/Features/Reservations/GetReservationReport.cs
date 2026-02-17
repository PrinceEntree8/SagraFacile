using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;

namespace SagraFacile.Web.Features.Reservations;

public static class GetReservationReport
{
    public record Query(DateTime? StartDate = null, DateTime? EndDate = null);

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

    public static async Task<Result> Handle(Query query, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var queryable = context.TableReservations.AsQueryable();

        // Apply date filters - ensure UTC by specifying kind if unspecified
        if (query.StartDate.HasValue)
        {
            var startDateUtc = query.StartDate.Value.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(query.StartDate.Value, DateTimeKind.Utc)
                : query.StartDate.Value.ToUniversalTime();
            queryable = queryable.Where(r => r.CreatedAt >= startDateUtc);
        }

        if (query.EndDate.HasValue)
        {
            var endDateUtc = query.EndDate.Value.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(query.EndDate.Value, DateTimeKind.Utc)
                : query.EndDate.Value.ToUniversalTime();
            queryable = queryable.Where(r => r.CreatedAt <= endDateUtc);
        }

        var reservations = await queryable
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var reports = reservations.Select(r =>
        {
            TimeSpan? waitTimeUntilFirstCall = r.FirstCalledAt.HasValue 
                ? r.FirstCalledAt.Value - r.CreatedAt 
                : null;

            TimeSpan? totalWaitTime = null;
            if (r.SeatedAt.HasValue)
            {
                totalWaitTime = r.SeatedAt.Value - r.CreatedAt;
            }
            else if (r.VoidedAt.HasValue)
            {
                totalWaitTime = r.VoidedAt.Value - r.CreatedAt;
            }

            return new ReportDto(
                r.Id,
                r.QueueNumber,
                r.CustomerName,
                r.PartySize,
                r.Status,
                r.CreatedAt,
                r.FirstCalledAt,
                r.SeatedAt,
                r.VoidedAt,
                r.CallCount,
                waitTimeUntilFirstCall,
                totalWaitTime);
        }).ToList();

        // Calculate statistics
        var seatedReservations = reports.Where(r => r.Status == "Seated" && r.TotalWaitTime.HasValue).ToList();
        var waitTimes = seatedReservations.Select(r => r.TotalWaitTime!.Value).ToList();

        var statistics = new StatisticsDto(
            TotalReservations: reservations.Count,
            SeatedCount: reservations.Count(r => r.Status == "Seated"),
            VoidedCount: reservations.Count(r => r.Status == "Voided"),
            WaitingCount: reservations.Count(r => r.Status == "Waiting" || r.Status == "Called"),
            AverageWaitTime: waitTimes.Any() ? TimeSpan.FromTicks((long)waitTimes.Average(t => t.Ticks)) : TimeSpan.Zero,
            MedianWaitTime: waitTimes.Any() ? GetMedian(waitTimes) : TimeSpan.Zero,
            MaxWaitTime: waitTimes.Any() ? waitTimes.Max() : null,
            MinWaitTime: waitTimes.Any() ? waitTimes.Min() : null);

        return new Result(reports, statistics);
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
        else
        {
            return sorted[count / 2];
        }
    }
}
