namespace SagraFacile.Domain.Features.Menu;

public class MenuItem
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public MenuCategory Category { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<MenuItemAllergen> MenuItemAllergens { get; set; } = new List<MenuItemAllergen>();
}
