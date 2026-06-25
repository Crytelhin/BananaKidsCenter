using Microsoft.EntityFrameworkCore;
using EntertainmentCenter.API.Models;

namespace EntertainmentCenter.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Zone -> Tariffs (Cascade delete)
        modelBuilder.Entity<Zone>()
            .HasMany(z => z.Tariffs)
            .WithOne(t => t.Zone)
            .HasForeignKey(t => t.ZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        // Session -> Client (Cascade delete)
        modelBuilder.Entity<Session>()
            .HasOne(s => s.Client)
            .WithMany()
            .HasForeignKey(s => s.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Session -> Tariff (Restrict delete)
        modelBuilder.Entity<Session>()
            .HasOne(s => s.Tariff)
            .WithMany()
            .HasForeignKey(s => s.TariffId)
            .OnDelete(DeleteBehavior.Restrict);

        // Session -> Promotion (SetNull)
        modelBuilder.Entity<Session>()
            .HasOne(s => s.Promotion)
            .WithMany()
            .HasForeignKey(s => s.PromotionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Seed AppConfig
        modelBuilder.Entity<AppConfig>().HasData(
            new AppConfig { Id = 1, AdminPin = "1234", WarningEnabled = true, WarningMinutesBeforeExpiry = 5 }
        );
    }
}