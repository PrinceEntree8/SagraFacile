namespace SagraFacile.Domain.Features.Menu;

public record MenuCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
