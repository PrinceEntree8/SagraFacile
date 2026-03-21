using Microsoft.AspNetCore.Identity;

namespace SagraFacile.Web.Data;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
