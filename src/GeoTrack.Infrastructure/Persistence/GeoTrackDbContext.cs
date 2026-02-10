using GeoTrack.Application.Common.Interfaces;
using GeoTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GeoTrack.Infrastructure.Persistence;

public class GeoTrackDbContext : DbContext, IGeoTrackDbContext
{
    public GeoTrackDbContext(DbContextOptions<GeoTrackDbContext> options) : base(options)
    {
    }

    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Device Configuration
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.ExternalId).IsRequired();
        });

        // Location Configuration
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);

            // FK
            entity.HasOne(d => d.Device)
                  .WithMany()
                  .HasForeignKey(d => d.DeviceId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Unique Constraint for Idempotency
            entity.HasIndex(e => new { e.DeviceId, e.Timestamp }).IsUnique();

            // Performance Index for History Queries
            entity.HasIndex(e => new { e.DeviceId, e.Timestamp }).IsDescending(false, true);
            // Note: EF Core define IsDescending per property in the index. 
            // But usually for "DeviceId, Timestamp desc", we do:
            // entity.HasIndex(e => new { e.DeviceId, e.Timestamp }); // Composite
            // fluent API for mixed direction is specific.
            // Let's use the explicit standard way for latest EF Core.
        });

        // Correct way for mixed sort order index in recent EF Core Npgsql
        modelBuilder.Entity<Location>()
            .HasIndex(x => new { x.DeviceId, x.Timestamp })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Locations_DeviceId_Timestamp_Desc");
    }
}
