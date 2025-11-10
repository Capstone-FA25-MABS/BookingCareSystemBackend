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

            entity.Property(e => e.UserId).IsRequired(true);

            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsRequired(false);

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
            // Always use UTC time to ensure consistency
            var utcNow = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                // Only set CreatedAt if it hasn't been set (default DateTime is 0001-01-01)
                if (entry.Entity.CreatedAt == default || entry.Entity.CreatedAt == DateTime.MinValue)
                {
                    entry.Entity.CreatedAt = utcNow;
                }
                else
                {
                    // Ensure CreatedAt is in UTC (convert if needed)
                    if (entry.Entity.CreatedAt.Kind != DateTimeKind.Utc)
                    {
                        entry.Entity.CreatedAt = entry.Entity.CreatedAt.ToUniversalTime();
                    }
                }
            }

            // Always update UpdatedAt to current UTC time
            entry.Entity.UpdatedAt = utcNow;

            // Ensure UpdatedAt is in UTC (convert if needed)
            if (entry.Entity.UpdatedAt.Kind != DateTimeKind.Utc)
            {
                entry.Entity.UpdatedAt = entry.Entity.UpdatedAt.ToUniversalTime();
            }
        }
    }
}
