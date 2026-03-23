using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Reservations;

public static class GetBestFitReservation
{
    public record Query(int TableCoverCount) : IQuery<Result>;
    public record Result(List<ReservationMatchDto> Matches);
    public record ReservationMatchDto(
        int Id,
        string QueueNumber,
        string CustomerName,
        int PartySize,
        string Notes,
        DateTime CreatedAt,
        TimeSpan WaitingTime,
        int CallCount,
        DateTime? LastCalledAt,
        string MatchQuality);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly IReservationRepository _repository;

        public Handler(IReservationRepository repository) => _repository = repository;

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var reservations = await _repository.GetCalledReservationsOrderedByCreatedAtAsync(cancellationToken);
            var matches = new List<ReservationMatchDto>();

            // If coverCount is 0, return ALL Called reservations without filtering
            if (query.TableCoverCount == 0)
            {
                foreach (var r in reservations)
                {
                    matches.Add(new ReservationMatchDto(
                        r.Id, r.QueueNumber, r.CustomerName, r.PartySize, r.Notes,
                        r.CreatedAt, now - r.CreatedAt, r.CallCount, r.LastCalledAt, "All"));
                }
                return new Result(matches);
            }

            foreach (var r in reservations)
            {
                string? matchQuality = null;

                if (r.PartySize == query.TableCoverCount)
                {
                    matchQuality = "Perfect";
                }
                else if (r.PartySize < query.TableCoverCount)
                {
                    matchQuality = "Fits";
                    if (r.PartySize >= query.TableCoverCount - 2)
                        matchQuality = "Good";
                    else if (r.PartySize >= query.TableCoverCount - 4)
                        matchQuality = "Acceptable";
                }

                if (matchQuality != null)
                {
                    matches.Add(new ReservationMatchDto(
                        r.Id, r.QueueNumber, r.CustomerName, r.PartySize, r.Notes,
                        r.CreatedAt, now - r.CreatedAt, r.CallCount, r.LastCalledAt, matchQuality));
                }
            }

            var sorted = matches
                .OrderBy(m => MatchQualityOrder(m.MatchQuality))
                .ThenBy(m => m.CreatedAt)
                .ToList();

            return new Result(sorted);
        }

        private static int MatchQualityOrder(string quality) => quality switch
        {
            "Perfect" => 0,
            "Good" => 1,
            "Acceptable" => 2,
            _ => 3
        };
    }
}
