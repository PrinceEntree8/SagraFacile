using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Common;
using SagraFacile.Contracts.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class GetBestFitReservation
{
    public record Query(int EventId, int TableCoverCount) : IQuery<IList<ReservationMatchDto>>;

    public class Handler : IQueryHandler<Query, IList<ReservationMatchDto>>
    {
        private readonly IReservationRepository _repository;

        public Handler(IReservationRepository repository) => _repository = repository;

        public async Task<IList<ReservationMatchDto>> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var reservations = await _repository.GetCalledReservationsOrderedByCreatedAtAsync(query.EventId, cancellationToken);

            // If coverCount is 0, return ALL Called reservations without filtering
            if (query.TableCoverCount == 0)
            {
                return reservations.Select(r => new ReservationMatchDto(
                    r.Id,
                    r.SequenceNumber,
                    r.CustomerName,
                    r.PartySize,
                    r.Notes,
                    r.CreatedAt,
                    now - r.CreatedAt,
                    r.CallCount,
                    r.LastCalledAt,
                    "All"
                    ))
                    .ToList();
            }

            var matches = new List<ReservationMatchDto>();

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
                        r.Id, r.SequenceNumber, r.CustomerName, r.PartySize, r.Notes,
                        r.CreatedAt, now - r.CreatedAt, r.CallCount, r.LastCalledAt, matchQuality));
                }
            }

            var sorted = matches
                .OrderBy(m => MatchQualityOrder(m.MatchQuality))
                .ThenBy(m => m.CreatedAt)
                .ToList();

            return sorted;
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
