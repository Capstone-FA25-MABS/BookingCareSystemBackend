using BookingCare.Services.Doctor.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Doctor.Data;

public class DoctorDbContext : DbContext
{
    public DoctorDbContext(DbContextOptions<DoctorDbContext> options) : base(options)
    {
    }

    public DbSet<DoctorEntity> Doctors { get; set; }
    public DbSet<PositionEntity> Positions { get; set; }
    public DbSet<PriceEntity> Prices { get; set; }
    public DbSet<DoctorPriceEntity> DoctorPrices { get; set; }
    public DbSet<PriceRuleEntity> PriceRules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DoctorEntity
        modelBuilder.Entity<DoctorEntity>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Unique constraints
            entity.HasIndex(e => e.AccountId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            // Configure string properties
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Gender)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasDefaultValue("https://bookingcaree.com/user-avatar-default.png");

            // Configure numeric properties
            entity.Property(e => e.YearsOfExperience)
                .HasDefaultValue(0);

            // Configure datetime properties
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Configure relationships
            entity.HasOne(e => e.Position)
                .WithMany()
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.DoctorPrices)
                .WithOne(e => e.Doctor)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PositionEntity
        modelBuilder.Entity<PositionEntity>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Configure string properties
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            // Configure datetime properties
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Configure PriceEntity
        modelBuilder.Entity<PriceEntity>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Configure decimal precision
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .IsRequired();
        });

        // Configure DoctorPriceEntity (Junction table)
        modelBuilder.Entity<DoctorPriceEntity>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.DoctorId, e.PriceId });

            // Configure relationships
            entity.HasOne(e => e.Doctor)
                .WithMany(e => e.DoctorPrices)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Price)
                .WithMany()
                .HasForeignKey(e => e.PriceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.IsOverride).HasDefaultValue(false);
        });

        // Configure PriceRuleEntity
        modelBuilder.Entity<PriceRuleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.BasePrice).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasDefaultValue(BookingCare.Shared.Common.Enums.Status.ACTIVE);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
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
        // Update DoctorEntity timestamps
        var doctorEntries = ChangeTracker.Entries<DoctorEntity>();
        foreach (var entry in doctorEntries)
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

        // Update PositionEntity timestamps
        var positionEntries = ChangeTracker.Entries<PositionEntity>();
        foreach (var entry in positionEntries)
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
