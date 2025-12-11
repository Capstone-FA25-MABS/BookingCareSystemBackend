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

    /// <summary>
    /// DbSet for AILabTools API keys
    /// </summary>
    public DbSet<AILabToolsApiKeyEntity> AILabToolsApiKeys { get; set; }

    /// <summary>
    /// DbSet for nutrition profiles
    /// </summary>
    public DbSet<NutritionProfileEntity> NutritionProfiles { get; set; }

    /// <summary>
    /// DbSet for meal plans
    /// </summary>
    public DbSet<MealPlanEntity> MealPlans { get; set; }

    /// <summary>
    /// DbSet for workout plans
    /// </summary>
    public DbSet<WorkoutPlanEntity> WorkoutPlans { get; set; }

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

            entity.ConfigureCacheEntityProperties(
                e => e.UsageCount,
                e => e.SuccessRate,
                e => e.CreatedAt,
                e => e.LastUsedAt,
                e => e.CreatedBy);

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

            entity.ConfigureCacheEntityProperties(
                e => e.UsageCount,
                e => e.SuccessRate,
                e => e.CreatedAt,
                e => e.LastUsedAt,
                e => e.CreatedBy);

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

            entity.ConfigureCacheEntityProperties(
                e => e.UsageCount,
                e => e.SuccessRate,
                e => e.CreatedAt,
                e => e.LastUsedAt,
                e => e.CreatedBy);

            // Indexes for fast lookup
            entity.HasIndex(e => e.EnglishName)
                .IsUnique()
                .HasDatabaseName("IX_Dermatology_EnglishName");
            entity.HasIndex(e => e.UsageCount);
            entity.HasIndex(e => e.LastUsedAt);
        });

        // Configure AILabToolsApiKeyEntity
        modelBuilder.Entity<AILabToolsApiKeyEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.ApiKey)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.MaxUsageCount)
                .IsRequired()
                .HasDefaultValue(10);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUsedAt)
                .IsRequired(false);

            entity.Property(e => e.Notes)
                .HasMaxLength(500)
                .IsRequired(false);

            // Indexes for performance
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.UsageCount);
            entity.HasIndex(e => e.LastUsedAt);
        });

        // Configure NutritionProfileEntity
        modelBuilder.Entity<NutritionProfileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.HeightCm).IsRequired();
            entity.Property(e => e.WeightKg).IsRequired();
            entity.Property(e => e.BMI).IsRequired();
            entity.Property(e => e.ActivityLevel).HasMaxLength(50).IsRequired();
            entity.Property(e => e.HealthGoal).HasMaxLength(50).IsRequired();

            entity.Property(e => e.HealthConditionsJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            entity.Property(e => e.DietaryPreferencesJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

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

        // Configure MealPlanEntity
        modelBuilder.Entity<MealPlanEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.NutritionProfileId).IsRequired();
            entity.Property(e => e.Date).IsRequired();

            entity.Property(e => e.MealsJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired()
                .HasDefaultValue("[]");

            entity.Property(e => e.GeneratedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IsNotificationSent)
                .IsRequired()
                .HasDefaultValue(false);

            // Foreign key relationship
            entity.HasOne(e => e.NutritionProfile)
                .WithMany(p => p.MealPlans)
                .HasForeignKey(e => e.NutritionProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(e => new { e.UserId, e.Date })
                .HasDatabaseName("IX_MealPlans_UserId_Date");
            entity.HasIndex(e => e.NutritionProfileId);
            entity.HasIndex(e => e.IsNotificationSent);
        });

        // Configure WorkoutPlanEntity
        modelBuilder.Entity<WorkoutPlanEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.NutritionProfileId).IsRequired();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.WorkoutType).HasMaxLength(100).IsRequired();

            entity.Property(e => e.ExercisesJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired()
                .HasDefaultValue("[]");

            entity.Property(e => e.GeneratedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IsNotificationSent)
                .IsRequired()
                .HasDefaultValue(false);

            // Foreign key relationship
            entity.HasOne(e => e.NutritionProfile)
                .WithMany(p => p.WorkoutPlans)
                .HasForeignKey(e => e.NutritionProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(e => new { e.UserId, e.Date })
                .HasDatabaseName("IX_WorkoutPlans_UserId_Date");
            entity.HasIndex(e => e.NutritionProfileId);
            entity.HasIndex(e => e.IsNotificationSent);
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
        UpdateConversationSessionTimestamps();
        UpdateCacheEntityTimestamps<SymptomQuestionCacheEntity>();
        UpdateKeywordEntityTimestamps();
        UpdateCacheEntityTimestamps<LabResultAbnormalIndicatorCacheEntity>();
        UpdateCacheEntityTimestamps<DermatologyDiseaseCacheEntity>();
        UpdateAILabToolsApiKeyTimestamps();
    }

    private void UpdateConversationSessionTimestamps()
    {
        var entries = ChangeTracker.Entries<ConversationSessionEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                SetCreatedAtIfNeeded(entry.Entity, utcNow);
            }

            SetUpdatedAt(entry.Entity, utcNow);
        }
    }

    private void UpdateCacheEntityTimestamps<T>() where T : class
    {
        var entries = ChangeTracker.Entries<T>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                SetCacheEntityTimestamps(entry.Entity, utcNow);
            }
        }
    }

    private void UpdateKeywordEntityTimestamps()
    {
        var entries = ChangeTracker.Entries<ConversationContextKeywordEntity>()
            .Where(e => e.State == EntityState.Added);

        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            SetCreatedAtIfNeeded(entry.Entity, utcNow);
        }
    }

    private static void SetCreatedAtIfNeeded(ConversationSessionEntity entity, DateTime utcNow)
    {
        if (IsDefaultDateTime(entity.CreatedAt))
        {
            entity.CreatedAt = utcNow;
        }
        else if (entity.CreatedAt.Kind != DateTimeKind.Utc)
        {
            entity.CreatedAt = entity.CreatedAt.ToUniversalTime();
        }
    }

    private static void SetCreatedAtIfNeeded(ConversationContextKeywordEntity entity, DateTime utcNow)
    {
        if (IsDefaultDateTime(entity.CreatedAt))
        {
            entity.CreatedAt = utcNow;
        }
    }

    private static void SetUpdatedAt(ConversationSessionEntity entity, DateTime utcNow)
    {
        entity.UpdatedAt = utcNow;
        if (entity.UpdatedAt.Kind != DateTimeKind.Utc)
        {
            entity.UpdatedAt = entity.UpdatedAt.ToUniversalTime();
        }
    }

    private static void SetCacheEntityTimestamps<T>(T entity, DateTime utcNow)
    {
        switch (entity)
        {
            case SymptomQuestionCacheEntity cacheEntity:
                SetCacheEntityTimestampsInternal(cacheEntity, utcNow);
                break;
            case LabResultAbnormalIndicatorCacheEntity cacheEntity:
                SetCacheEntityTimestampsInternal(cacheEntity, utcNow);
                break;
            case DermatologyDiseaseCacheEntity cacheEntity:
                SetCacheEntityTimestampsInternal(cacheEntity, utcNow);
                break;
        }
    }

    private static void SetCacheEntityTimestampsInternal(SymptomQuestionCacheEntity entity, DateTime utcNow)
    {
        SetCacheTimestamps(entity, utcNow);
    }

    private static void SetCacheEntityTimestampsInternal(LabResultAbnormalIndicatorCacheEntity entity, DateTime utcNow)
    {
        SetCacheTimestamps(entity, utcNow);
    }

    private static void SetCacheEntityTimestampsInternal(DermatologyDiseaseCacheEntity entity, DateTime utcNow)
    {
        SetCacheTimestamps(entity, utcNow);
    }

    private static void SetCacheTimestamps(SymptomQuestionCacheEntity entity, DateTime utcNow)
    {
        SetCreatedAtIfNeeded(entity, utcNow);
        SetLastUsedAtIfNeeded(entity, utcNow);
    }

    private static void SetCacheTimestamps(LabResultAbnormalIndicatorCacheEntity entity, DateTime utcNow)
    {
        SetCreatedAtIfNeeded(entity, utcNow);
        SetLastUsedAtIfNeeded(entity, utcNow);
    }

    private static void SetCacheTimestamps(DermatologyDiseaseCacheEntity entity, DateTime utcNow)
    {
        SetCreatedAtIfNeeded(entity, utcNow);
        SetLastUsedAtIfNeeded(entity, utcNow);
    }

    private static void SetCreatedAtIfNeeded(SymptomQuestionCacheEntity entity, DateTime utcNow)
    {
        if (IsDefaultDateTime(entity.CreatedAt))
        {
            entity.CreatedAt = utcNow;
        }
    }

    private static void SetCreatedAtIfNeeded(LabResultAbnormalIndicatorCacheEntity entity, DateTime utcNow)
    {
        if (IsDefaultDateTime(entity.CreatedAt))
        {
            entity.CreatedAt = utcNow;
        }
    }

    private static void SetCreatedAtIfNeeded(DermatologyDiseaseCacheEntity entity, DateTime utcNow)
    {
        if (IsDefaultDateTime(entity.CreatedAt))
        {
            entity.CreatedAt = utcNow;
        }
    }

    private static void SetLastUsedAtIfNeeded(SymptomQuestionCacheEntity entity, DateTime utcNow)
    {
        if (IsDefaultDateTime(entity.LastUsedAt))
        {
            entity.LastUsedAt = utcNow;
        }
    }

    private static void SetLastUsedAtIfNeeded(LabResultAbnormalIndicatorCacheEntity entity, DateTime utcNow)
    {
        if (IsDefaultDateTime(entity.LastUsedAt))
        {
            entity.LastUsedAt = utcNow;
        }
    }

    private static void SetLastUsedAtIfNeeded(DermatologyDiseaseCacheEntity entity, DateTime utcNow)
    {
        if (IsDefaultDateTime(entity.LastUsedAt))
        {
            entity.LastUsedAt = utcNow;
        }
    }

    private void UpdateAILabToolsApiKeyTimestamps()
    {
        var entries = ChangeTracker.Entries<AILabToolsApiKeyEntity>()
            .Where(e => e.State == EntityState.Added);

        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (IsDefaultDateTime(entry.Entity.CreatedAt))
            {
                entry.Entity.CreatedAt = utcNow;
            }
        }
    }

    private static bool IsDefaultDateTime(DateTime dateTime)
    {
        return dateTime == default || dateTime == DateTime.MinValue;
    }
}
