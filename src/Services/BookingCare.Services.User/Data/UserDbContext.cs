using BookingCare.Services.User.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.User.Data;

public class UserDbContext : DbContext
{
    public DbSet<UserEntity> Users { get; set; }

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
            .Where(e => e.Entity is UserEntity);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is UserEntity user)
                {
                    user.CreatedAt = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is UserEntity user)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                    entry.Property(nameof(user.CreatedAt)).IsModified = false;
                }
            }
        }
    }
}
