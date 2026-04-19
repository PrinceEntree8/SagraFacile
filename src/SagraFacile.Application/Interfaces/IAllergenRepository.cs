using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Interfaces;

public interface IAllergenRepository
{
    Task<List<Allergen>> GetAllAsync(CancellationToken ct);
}
