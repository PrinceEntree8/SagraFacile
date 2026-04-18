namespace SagraFacile.Domain.Features.Menu;

public class Allergen
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameIt { get; set; } = string.Empty;
    public ICollection<MenuItemAllergen> MenuItemAllergens { get; set; } = new List<MenuItemAllergen>();
}
