using BookingCare.Services.Discount.Enums;
using BookingCare.Services.Discount.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Discount.Data;

public class DiscountDbContext : DbContext
{
    public DiscountDbContext(DbContextOptions<DiscountDbContext> options)
        : base(options) { }

    public DbSet<DiscountEntity> Discounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DiscountEntity
        modelBuilder.Entity<DiscountEntity>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Unique constraint on code
            entity.HasIndex(e => e.Code).IsUnique();

            // Configure decimal precision
            entity.Property(e => e.Amount).HasPrecision(10, 2);

            // Configure string properties
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();

            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();

            entity.Property(e => e.DiscountType).HasMaxLength(20).IsRequired();

            entity
                .Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValue(DiscountStatus.INACTIVE);

            // Configure datetime properties
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

            // Configure default values
            entity.Property(e => e.UsesCount).HasDefaultValue(0);

            // Configure check constraints (Note: EF Core doesn't directly support check constraints,
            // but we can add them via raw SQL migrations if needed)
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<DiscountEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                // Prevent overwriting CreatedAt
                entry.Property(e => e.CreatedAt).IsModified = false;
            }
        }
    }
}
