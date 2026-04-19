namespace SagraFacile.Domain.Features.Menu;

public record MenuItemAllergen
{
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public int AllergenId { get; set; }
    public Allergen Allergen { get; set; } = null!;
}
