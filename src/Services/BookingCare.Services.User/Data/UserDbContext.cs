using BookingCare.Services.User.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.User.Data;

public class UserDbContext : DbContext
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<PatientRelativeEntity> PatientRelatives { get; set; }

    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AccountId)
                .IsRequired();

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
                .HasMaxLength(20)
                .HasConversion<string>();

            entity.Property(e => e.DateOfBirth)
                .IsRequired(false);

            entity.Property(e => e.Address)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Phone)
                .HasMaxLength(10);

            entity.Property(e => e.AvatarUrl)
                .HasDefaultValue("https://bookingcaree.com/user-avatar-default.png");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // One User can have many PatientRelatives
            entity.HasMany<PatientRelativeEntity>()
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PatientRelative
        modelBuilder.Entity<PatientRelativeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Gender)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.DateOfBirth)
                .IsRequired();

            entity.Property(e => e.Phone)
                .HasMaxLength(15);

            entity.Property(e => e.Relationship)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.HealthInsuranceNumber)
                .HasMaxLength(20);

            entity.Property(e => e.IdentityNumber)
                .HasMaxLength(20);

            entity.Property(e => e.Notes)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Index for faster lookup by UserId
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_PatientRelatives_UserId");
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
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is UserEntity || e.Entity is PatientRelativeEntity);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is UserEntity user)
                {
                    user.CreatedAt = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is PatientRelativeEntity relative)
                {
                    relative.CreatedAt = DateTime.UtcNow;
                    relative.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is UserEntity user)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                    entry.Property(nameof(user.CreatedAt)).IsModified = false;
                }
                else if (entry.Entity is PatientRelativeEntity relative)
                {
                    relative.UpdatedAt = DateTime.UtcNow;
                    entry.Property(nameof(relative.CreatedAt)).IsModified = false;
                }
            }
        }
    }
}
