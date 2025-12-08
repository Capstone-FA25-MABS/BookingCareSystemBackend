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

    /// <summary>
    /// DbSet for symptom question cache
    /// </summary>
    public DbSet<SymptomQuestionCacheEntity> SymptomQuestionCache { get; set; }

    /// <summary>
    /// DbSet for conversation context keywords
    /// </summary>
    public DbSet<ConversationContextKeywordEntity> ConversationContextKeywords { get; set; }

    /// <summary>
    /// DbSet for lab result abnormal indicator cache
    /// </summary>
    public DbSet<LabResultAbnormalIndicatorCacheEntity> LabResultAbnormalIndicatorCache { get; set; }

    /// <summary>
    /// DbSet for dermatology disease cache
    /// </summary>
    public DbSet<DermatologyDiseaseCacheEntity> DermatologyDiseaseCache { get; set; }

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

        // Configure SymptomQuestionCacheEntity
        modelBuilder.Entity<SymptomQuestionCacheEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.InitialSymptom)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.ConversationContext)
                .HasMaxLength(1000)
                .IsRequired(false);

            entity.Property(e => e.NormalizedKeywords)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(e => e.NormalizedMessage)
                .HasMaxLength(500)
                .IsRequired(false);

            entity.Property(e => e.QuestionNumber)
                .IsRequired();

            entity.Property(e => e.Question)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(e => e.Purpose)
                .HasMaxLength(500)
                .IsRequired(false);

            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .IsRequired(false);

            entity.Property(e => e.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.SuccessRate)
                .IsRequired()
                .HasDefaultValue(0.0);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUsedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .IsRequired(false);

            // Indexes for fast lookup
            entity.HasIndex(e => new { e.NormalizedKeywords, e.QuestionNumber })
                .HasDatabaseName("IX_NormalizedKeywords_QuestionNumber");
            entity.HasIndex(e => new { e.NormalizedMessage, e.QuestionNumber })
                .HasDatabaseName("IX_NormalizedMessage_QuestionNumber");
            entity.HasIndex(e => e.InitialSymptom);
            entity.HasIndex(e => e.UsageCount);
            entity.HasIndex(e => e.LastUsedAt);
        });

        // Configure ConversationContextKeywordEntity
        modelBuilder.Entity<ConversationContextKeywordEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Keyword)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsRequired(false);

            entity.Property(e => e.Synonyms)
                .HasMaxLength(500)
                .IsRequired(false);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Unique constraint on Keyword
            entity.HasIndex(e => e.Keyword)
                .IsUnique();
            entity.HasIndex(e => e.Category);
        });

        // Configure LabResultAbnormalIndicatorCacheEntity
        modelBuilder.Entity<LabResultAbnormalIndicatorCacheEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.NormalizedKeywords)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(e => e.NormalizedText)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            entity.Property(e => e.AbnormalIndicatorsJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(e => e.NormalIndicatorsJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(e => e.SpecialtiesJson)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(e => e.Disclaimer)
                .HasMaxLength(1000)
                .IsRequired(false);

            entity.Property(e => e.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.SuccessRate)
                .IsRequired()
                .HasDefaultValue(0.0);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUsedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .IsRequired(false);

            // Indexes for fast lookup
            // Note: NormalizedText is nvarchar(max) so cannot be indexed directly
            // We'll use it for exact matching in code (acceptable for cache lookup)
            entity.HasIndex(e => e.NormalizedKeywords)
                .HasDatabaseName("IX_LabResult_NormalizedKeywords");
            entity.HasIndex(e => e.UsageCount);
            entity.HasIndex(e => e.LastUsedAt);
        });

        // Configure DermatologyDiseaseCacheEntity
        modelBuilder.Entity<DermatologyDiseaseCacheEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.EnglishName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.VietnameseName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.ReasonsJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            entity.Property(e => e.AdviceJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(e => e.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.SuccessRate)
                .IsRequired()
                .HasDefaultValue(0.0);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUsedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .IsRequired(false);

            // Indexes for fast lookup
            entity.HasIndex(e => e.EnglishName)
                .IsUnique()
                .HasDatabaseName("IX_Dermatology_EnglishName");
            entity.HasIndex(e => e.UsageCount);
            entity.HasIndex(e => e.LastUsedAt);
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
        // Update ConversationSessionEntity timestamps
        var sessionEntries = ChangeTracker.Entries<ConversationSessionEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in sessionEntries)
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

        // Update SymptomQuestionCacheEntity timestamps
        var cacheEntries = ChangeTracker.Entries<SymptomQuestionCacheEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in cacheEntries)
        {
            var utcNow = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default || entry.Entity.CreatedAt == DateTime.MinValue)
                {
                    entry.Entity.CreatedAt = utcNow;
                }

                if (entry.Entity.LastUsedAt == default || entry.Entity.LastUsedAt == DateTime.MinValue)
                {
                    entry.Entity.LastUsedAt = utcNow;
                }
            }
        }

        // Update ConversationContextKeywordEntity timestamps
        var keywordEntries = ChangeTracker.Entries<ConversationContextKeywordEntity>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in keywordEntries)
        {
            var utcNow = DateTime.UtcNow;

            if (entry.Entity.CreatedAt == default || entry.Entity.CreatedAt == DateTime.MinValue)
            {
                entry.Entity.CreatedAt = utcNow;
            }
        }

        // Update LabResultAbnormalIndicatorCacheEntity timestamps
        var labCacheEntries = ChangeTracker.Entries<LabResultAbnormalIndicatorCacheEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in labCacheEntries)
        {
            var utcNow = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default || entry.Entity.CreatedAt == DateTime.MinValue)
                {
                    entry.Entity.CreatedAt = utcNow;
                }

                if (entry.Entity.LastUsedAt == default || entry.Entity.LastUsedAt == DateTime.MinValue)
                {
                    entry.Entity.LastUsedAt = utcNow;
                }
            }
        }

        // Update DermatologyDiseaseCacheEntity timestamps
        var dermatologyCacheEntries = ChangeTracker.Entries<DermatologyDiseaseCacheEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in dermatologyCacheEntries)
        {
            var utcNow = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default || entry.Entity.CreatedAt == DateTime.MinValue)
                {
                    entry.Entity.CreatedAt = utcNow;
                }

                if (entry.Entity.LastUsedAt == default || entry.Entity.LastUsedAt == DateTime.MinValue)
                {
                    entry.Entity.LastUsedAt = utcNow;
                }
            }
        }
    }
}
