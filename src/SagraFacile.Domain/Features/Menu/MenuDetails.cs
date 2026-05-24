namespace SagraFacile.Domain.Features.Menu;

public class MenuDetails
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string? WarningMessage { get; set; }
    public string? Header { get; set; }
    public string? Footer { get; set; }
}
