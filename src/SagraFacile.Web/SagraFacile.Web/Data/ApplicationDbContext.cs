using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Features.Events;
using SagraFacile.Web.Features.Reservations;

namespace SagraFacile.Web.Data;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entities
        modelBuilder.Entity<TableReservation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QueueNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PartySize).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.QueueNumber).IsUnique();
            entity.HasMany(e => e.Calls)
                .WithOne(c => c.TableReservation)
                .HasForeignKey(c => c.TableReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Table>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TableNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CoverCount).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.TableNumber).IsUnique();
        });

        modelBuilder.Entity<ReservationCall>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CalledBy).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Notes).IsRequired().HasMaxLength(500);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(10);
            entity.Property(e => e.CurrencySymbol).IsRequired().HasMaxLength(5);
        });
    }
}
