using BookingCare.Services.Doctor.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Doctor.Data;

public interface IHasTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

public class DoctorDbContext : DbContext
{
    public DoctorDbContext(DbContextOptions<DoctorDbContext> options) : base(options)
    {
    }

    public DbSet<DoctorEntity> Doctors { get; set; }
    public DbSet<PositionEntity> Positions { get; set; }
    public DbSet<SpecialtyEntity> Specialties { get; set; }
    public DbSet<DoctorPriceEntity> DoctorPrices { get; set; }
    public DbSet<LanguageEntity> Languages { get; set; }
    public DbSet<DoctorLanguageEntity> DoctorLanguages { get; set; }
    public DbSet<ServiceTypeEntity> ServiceTypes { get; set; }

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

            entity.HasOne(e => e.Specialty)
                .WithMany()
                .HasForeignKey(e => e.SpecialtyId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.DoctorPrices)
                .WithOne(e => e.Doctor)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.DoctorLanguages)
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

            // Configure enum properties
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(10);

            // Configure datetime properties
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Configure SpecialtyEntity
        modelBuilder.Entity<SpecialtyEntity>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Configure string properties
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ImageUrl)
                .IsRequired();

            // Configure enum properties
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(10);

            // Configure datetime properties
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Configure DoctorPriceEntity
        modelBuilder.Entity<DoctorPriceEntity>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Configure decimal precision
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .IsRequired();

            // Configure datetime properties
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Configure relationships
            entity.HasOne(e => e.Doctor)
                .WithMany(e => e.DoctorPrices)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ServiceType)
                .WithMany(e => e.DoctorPrices)
                .HasForeignKey(e => e.ServiceTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure LanguageEntity
        modelBuilder.Entity<LanguageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            // Configure enum properties
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(10);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Configure DoctorLanguageEntity
        modelBuilder.Entity<DoctorLanguageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Doctor)
                .WithMany(e => e.DoctorLanguages)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Language)
                .WithMany(e => e.DoctorLanguages)
                .HasForeignKey(e => e.LanguageId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint for doctor-language combination
            entity.HasIndex(e => new { e.DoctorId, e.LanguageId }).IsUnique();
        });

        // Configure ServiceTypeEntity
        modelBuilder.Entity<ServiceTypeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(255);

            entity.Property(e => e.ImageUrl)
                .IsRequired();

            // Configure enum properties
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(10);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");
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
        UpdateEntityTimestamps<DoctorEntity>();

        UpdateEntityTimestamps<PositionEntity>();

        UpdateEntityTimestamps<SpecialtyEntity>();

        UpdateEntityTimestamps<DoctorPriceEntity>();

        UpdateEntityTimestamps<LanguageEntity>();

        UpdateEntityTimestamps<ServiceTypeEntity>();
    }

    private void UpdateEntityTimestamps<T>() where T : class
    {
        var currentTime = DateTime.UtcNow;
        var entries = ChangeTracker.Entries<T>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                SetCreatedAndUpdatedTimestamps(entry, currentTime);
            }
            else if (entry.State == EntityState.Modified)
            {
                SetUpdatedTimestamp(entry, currentTime);
            }
        }
    }

    private void SetCreatedAndUpdatedTimestamps<T>(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> entry, DateTime currentTime) where T : class
    {
        if (entry.Entity is IHasTimestamps entity)
        {
            entity.CreatedAt = currentTime;
            entity.UpdatedAt = currentTime;
        }
    }

    private void SetUpdatedTimestamp<T>(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> entry, DateTime currentTime) where T : class
    {
        if (entry.Entity is IHasTimestamps entity)
        {
            entity.UpdatedAt = currentTime;
            entry.Property("CreatedAt").IsModified = false;
        }
    }
}
