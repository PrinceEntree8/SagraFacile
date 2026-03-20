using Microsoft.AspNetCore.Identity;

namespace SagraFacile.Web.Data;

public static class DataSeeder
{
    public static readonly string[] Roles = ["Admin", "Cassiere", "Cucina", "Supervisore"];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminUsername = configuration["Seed:AdminUsername"] ?? "admin";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin@123!";
        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@sagrafacile.local";

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
}
