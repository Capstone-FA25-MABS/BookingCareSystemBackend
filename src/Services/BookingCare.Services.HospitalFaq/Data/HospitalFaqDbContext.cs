using BookingCare.Services.HospitalFaq.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.HospitalFaq.Data;

public class HospitalFaqDbContext : DbContext
{
    public HospitalFaqDbContext(DbContextOptions<HospitalFaqDbContext> options) : base(options)
    {
    }

    public DbSet<HospitalFaqEntity> HospitalFaqs => Set<HospitalFaqEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

