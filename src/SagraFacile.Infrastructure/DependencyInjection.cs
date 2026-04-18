using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SagraFacile.Application.Interfaces;
using SagraFacile.Infrastructure.Data;
using SagraFacile.Infrastructure.Identity;
using SagraFacile.Infrastructure.Repositories;

namespace SagraFacile.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers EF Core, ASP.NET Identity, and repository implementations.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddTransient<IEventRepository, EventRepository>();
        services.AddTransient<IReservationRepository, ReservationRepository>();
        services.AddTransient<ITableRepository, TableRepository>();
        services.AddTransient<IMenuRepository, MenuRepository>();
        services.AddTransient<IAllergenRepository, AllergenRepository>();

        return services;
    }
}
