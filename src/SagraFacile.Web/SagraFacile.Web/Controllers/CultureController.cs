using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace SagraFacile.Web.Controllers;

/// <summary>
/// Sets the user's preferred UI culture via a cookie and redirects back.
/// </summary>
[Route("[controller]")]
[ApiController]
public class CultureController : ControllerBase
{
    [HttpGet]
    public IActionResult Set([FromQuery] string culture, [FromQuery] string redirectUri = "/")
    {
        if (string.IsNullOrWhiteSpace(culture))
            return BadRequest("culture is required");

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        return LocalRedirect(redirectUri);
    }
}
