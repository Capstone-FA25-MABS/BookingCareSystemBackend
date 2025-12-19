using BookingCare.Services.Content.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Content.Data;

/// <summary>
/// Unified DbContext for the Content service, hosting Blog and Hospital FAQ data.
/// This replaces the separate BlogDbContext and HospitalFaqDbContext from the old services.
/// </summary>
public class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<BlogCategoryEntity> BlogCategories => Set<BlogCategoryEntity>();
    public DbSet<BlogEntity> Blogs => Set<BlogEntity>();
    public DbSet<HospitalFaqEntity> HospitalFaqs => Set<HospitalFaqEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BlogCategory configuration (from old BlogDbContext)
        modelBuilder.Entity<BlogCategoryEntity>(entity =>
        {
            entity.ToTable("blog_categories");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.CategoryName)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(e => e.ImageUrl)
                  .HasMaxLength(1024);

            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(10)
                  .HasDefaultValue(CategoryStatus.Active);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Parent)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Blog configuration (from old BlogDbContext)
        modelBuilder.Entity<BlogEntity>(entity =>
        {
            entity.ToTable("blogs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.TitleVi)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(e => e.TitleEn)
                  .HasMaxLength(255);

            entity.Property(e => e.Tag)
                  .HasMaxLength(100);

            entity.Property(e => e.Source)
                  .HasMaxLength(100);

            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .HasDefaultValue(BlogStatus.Pending);

            entity.Property(e => e.CreatedBy)
                  .HasColumnName("CreatedBy");

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Category)
                  .WithMany(e => e.Blogs)
                  .HasForeignKey(e => e.BlogCategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Hospital FAQ configuration (from old HospitalFaqDbContext)
        modelBuilder.Entity<HospitalFaqEntity>(entity =>
        {
            entity.ToTable("hospital_faqs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.HospitalId)
                  .IsRequired();

            entity.Property(e => e.Question)
                  .IsRequired();

            entity.Property(e => e.Answer)
                  .IsRequired();

            entity.Property(e => e.CreatedBy)
                  .IsRequired();

            entity.Property(e => e.DisplayOrder)
                  .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("GETDATE()");

            // Index for faster queries by hospital
            entity.HasIndex(e => e.HospitalId);

            // Index for sorting by display order
            entity.HasIndex(e => new { e.HospitalId, e.DisplayOrder });
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ITimestampedEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(ITimestampedEntity.CreatedAt)).IsModified = false;
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}


