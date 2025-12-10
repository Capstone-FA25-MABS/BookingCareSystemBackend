using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingCare.Services.AI.Data;

/// <summary>
/// Extension methods for EntityTypeBuilder to reduce code duplication
/// </summary>
internal static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Configure common cache entity properties: UsageCount, SuccessRate, CreatedAt, LastUsedAt, CreatedBy
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureCacheEntityProperties<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        System.Linq.Expressions.Expression<Func<TEntity, int>> usageCountSelector,
        System.Linq.Expressions.Expression<Func<TEntity, double>> successRateSelector,
        System.Linq.Expressions.Expression<Func<TEntity, DateTime>> createdAtSelector,
        System.Linq.Expressions.Expression<Func<TEntity, DateTime>> lastUsedAtSelector,
        System.Linq.Expressions.Expression<Func<TEntity, string?>> createdBySelector)
        where TEntity : class
    {
        builder.Property(usageCountSelector)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(successRateSelector)
            .IsRequired()
            .HasDefaultValue(0.0);

        builder.Property(createdAtSelector)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(lastUsedAtSelector)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(createdBySelector)
            .HasMaxLength(100)
            .IsRequired(false);

        return builder;
    }

    /// <summary>
    /// Configure common cache entity properties: UsageCount and SuccessRate only
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureCacheUsageProperties<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        System.Linq.Expressions.Expression<Func<TEntity, int>> usageCountSelector,
        System.Linq.Expressions.Expression<Func<TEntity, double>> successRateSelector)
        where TEntity : class
    {
        builder.Property(usageCountSelector)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(successRateSelector)
            .IsRequired()
            .HasDefaultValue(0.0);

        return builder;
    }
}

