using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SagraFacile.Application;
using SagraFacile.Infrastructure;
using SagraFacile.Infrastructure.Data;
using SagraFacile.Infrastructure.Identity;
using SagraFacile.Web.Components;
using SagraFacile.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");

builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=sagrafacile;Username=postgres;Password=postgres";

builder.Services.AddInfrastructureServices(connectionString);
builder.Services.AddApplicationServices();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("JWT key (Jwt:Key) must be configured via environment variables or appsettings.");

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SagraFacile",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SagraFacile",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrSupervisore", policy => policy.RequireRole("Admin", "Supervisore"));
    options.AddPolicy("Cassiere", policy => policy.RequireRole("Admin", "Supervisore", "Cassiere"));
    options.AddPolicy("Cucina", policy => policy.RequireRole("Admin", "Supervisore", "Cucina"));
});

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await DataSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var supportedCultures = new[] { "it", "en" };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("it"),
    SupportedCultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList(),
    SupportedUICultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList(),
    RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new AcceptLanguageHeaderRequestCultureProvider()
    }
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<ReservationHub>("/hubs/reservations");

app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync();
    return Results.Redirect("/login");
});

app.Run();
