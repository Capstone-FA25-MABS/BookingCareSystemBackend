using Microsoft.EntityFrameworkCore;
using BookingCare.Services.AI.Models.Entities;

namespace BookingCare.Services.AI.Data;

/// <summary>
/// Database context for the AI Service
/// </summary>
public class AiDbContext : DbContext
{
    public AiDbContext(DbContextOptions<AiDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// DbSet for conversation sessions
    /// </summary>
    public DbSet<ConversationSessionEntity> ConversationSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ConversationSessionEntity
        modelBuilder.Entity<ConversationSessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.UserId).IsRequired(false);
            entity.Property(e => e.ProvinceId).HasMaxLength(50).IsRequired(false);
            entity.Property(e => e.DistrictId).HasMaxLength(50).IsRequired(false);

            entity.Property(e => e.ConversationHistory)
                .HasColumnType("nvarchar(max)")
                .IsRequired()
                .HasDefaultValue("[]");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
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
        var entries = ChangeTracker.Entries<ConversationSessionEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
