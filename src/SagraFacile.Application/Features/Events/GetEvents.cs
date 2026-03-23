using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Events;

public static class GetEvents
{
    public record Query() : IQuery<Result>;
    public record Result(List<EventDto> Events);
    public record EventDto(
        int Id,
        string Name,
        string Description,
        DateTime Date,
        string Currency,
        string CurrencySymbol,
        bool IsActive,
        DateTime CreatedAt);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly IEventRepository _repository;

        public Handler(IEventRepository repository) => _repository = repository;

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            var events = await _repository.GetAllOrderedByDateDescAsync(cancellationToken);
            var dtos = events
                .Select(e => new EventDto(e.Id, e.Name, e.Description, e.Date, e.Currency, e.CurrencySymbol, e.IsActive, e.CreatedAt))
                .ToList();
            return new Result(dtos);
        }
    }
}
