using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Menu;
using SagraFacile.Domain.Features.Reservations;
using SagraFacile.Infrastructure.Identity;

namespace SagraFacile.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TableReservation> TableReservations => Set<TableReservation>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<ReservationCall> ReservationCalls => Set<ReservationCall>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<Allergen> Allergens => Set<Allergen>();
    public DbSet<MenuItemAllergen> MenuItemAllergens => Set<MenuItemAllergen>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
