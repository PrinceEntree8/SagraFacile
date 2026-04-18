namespace SagraFacile.Domain.Features.Menu;

public class MenuCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;       // English
    public string NameIt { get; set; } = string.Empty;     // Italian
    public int DisplayOrder { get; set; } = 0;
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
