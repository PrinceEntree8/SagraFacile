using Microsoft.AspNetCore.Identity;

namespace SagraFacile.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
