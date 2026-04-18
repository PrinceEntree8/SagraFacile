using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SagraFacile.Domain.Features.Menu;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Identity;

public static class DataSeeder
{
    public static readonly string[] Roles = ["Admin", "Cassiere", "Cucina", "Supervisore"];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DataSeeder");

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await SeedAllergensAsync(serviceProvider);
        await SeedMenuCategoriesAsync(serviceProvider);

        var adminUsername = configuration["Seed:AdminUsername"] ?? "admin";
        var adminPassword = configuration["Seed:AdminPassword"];
        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@sagrafacile.local";

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Seed:AdminPassword is not configured. Skipping admin user creation.");
            return;
        }

        var adminUser = await userManager.FindByNameAsync(adminUsername);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminUsername,
                Email = adminEmail,
                DisplayName = "Amministratore",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    private static async Task SeedAllergensAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var allergens = new[]
        {
            ("GLUTEN",      "🌾"),
            ("CRUSTACEANS", "🦐"),
            ("EGGS",        "🥚"),
            ("FISH",        "🐟"),
            ("PEANUTS",     "🥜"),
            ("SOYBEANS",    "🫘"),
            ("MILK",        "🥛"),
            ("NUTS",        "🌰"),
            ("CELERY",      "🥬"),
            ("MUSTARD",     "🌭"),
            ("SESAME",      "🌿"),
            ("SULPHITES",   "🍷"),
            ("LUPIN",       "🌸"),
            ("MOLLUSCS",    "🐙"),
        };
        foreach (var (code, icon) in allergens)
        {
            if (!await db.Allergens.AnyAsync(a => a.Code == code))
                db.Allergens.Add(new Allergen { Code = code, Icon = icon });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedMenuCategoriesAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var categories = new[]
        {
            ("Starters",    1),
            ("Main Course", 2),
            ("Side Dishes", 3),
            ("Dessert",     4),
            ("Drinks",      5),
        };
        foreach (var (name, order) in categories)
        {
            if (!await db.MenuCategories.AnyAsync(c => c.Name == name))
                db.MenuCategories.Add(new MenuCategory { Name = name, DisplayOrder = order });
        }
        await db.SaveChangesAsync();
    }
}
