using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Events;

public static class GetActiveEvent
{
    public record Query() : IQuery<Result>;
    public record Result(EventDto? ActiveEvent);
    public record EventDto(
        int Id,
        string Name,
        string Currency,
        string CurrencySymbol);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly IEventRepository _repository;

        public Handler(IEventRepository repository) => _repository = repository;

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            var ev = await _repository.GetActiveAsync(cancellationToken);
            if (ev is null)
                return new Result(null);
            return new Result(new EventDto(ev.Id, ev.Name, ev.Currency, ev.CurrencySymbol));
        }
    }
}
