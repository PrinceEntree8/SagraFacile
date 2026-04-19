namespace SagraFacile.Domain.Features.Menu;

public record Allergen
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public ICollection<MenuItemAllergen> MenuItemAllergens { get; set; } = new List<MenuItemAllergen>();
}
