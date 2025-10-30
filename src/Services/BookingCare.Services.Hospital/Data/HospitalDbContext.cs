using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Enums;
using BookingCare.Shared.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Hospital.Data;

public class HospitalDbContext : DbContext
{
    public HospitalDbContext(DbContextOptions<HospitalDbContext> options) : base(options)
    {
    }

    public DbSet<HospitalEntity> Hospitals { get; set; }
    public DbSet<SubscriptionPlanEntity> SubscriptionPlans { get; set; }
    public DbSet<HospitalSubscriptionEntity> HospitalSubscriptions { get; set; }
    public DbSet<HospitalSpecialtyEntity> HospitalSpecialties { get; set; }
    public DbSet<HospitalImageEntity> HospitalImages { get; set; }
    public DbSet<HospitalRegistrationEntity> HospitalRegistrations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Hospital entity
        modelBuilder.Entity<HospitalEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Address).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

            // Note: Status is now managed by Auth service, not stored in hospital table
        });

        // Configure SubscriptionPlan entity
        modelBuilder.Entity<SubscriptionPlanEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(10,2)");
            entity.Property(e => e.BillingCycle).HasMaxLength(20).HasDefaultValue("MONTHLY");
            entity.Property(e => e.MaxDoctors).HasDefaultValue(0);
            entity.Property(e => e.MaxSpecialties).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

            // Check constraints
            entity.ToTable(t => t.HasCheckConstraint("CK_subscription_plans_billing_cycle", "billing_cycle IN ('MONTHLY', 'QUARTERLY', 'YEARLY')"));
            // Note: Status is now managed by Auth service, not stored in subscription plan table
        });

        // Configure HospitalSubscription entity
        modelBuilder.Entity<HospitalSubscriptionEntity>(entity =>
        {
            entity.HasKey(e => e.HospitalSubscriptionId);
            entity.Property(e => e.HospitalSubscriptionId).ValueGeneratedOnAdd();
            entity.Property(e => e.StartDate).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.EndDate).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

            // Foreign key relationships
            entity.HasOne(e => e.Hospital)
                  .WithMany(h => h.HospitalSubscriptions)
                  .HasForeignKey(e => e.HospitalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SubscriptionPlan)
                  .WithMany(s => s.HospitalSubscriptions)
                  .HasForeignKey(e => e.SubscriptionId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Check constraints for new subscription status values
            entity.ToTable(t => t.HasCheckConstraint("CK_hospital_subscriptions_status",
                "status IN ('ACTIVE', 'EXPIRED', 'CANCELLED', 'PENDING', 'TRIAL')"));
        });

        // Configure HospitalSpecialty entity (Many-to-Many)
        modelBuilder.Entity<HospitalSpecialtyEntity>(entity =>
        {
            entity.HasKey(e => new { e.HospitalId, e.SpecialtyId });

            entity.HasOne(e => e.Hospital)
                  .WithMany(h => h.HospitalSpecialties)
                  .HasForeignKey(e => e.HospitalId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Note: Specialty relationship would be configured when SpecialtyEntity is available
        });

        // Configure HospitalImage entity
        modelBuilder.Entity<HospitalImageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.S3Key).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ImageUrl).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

            // Foreign key relationship
            entity.HasOne(e => e.Hospital)
                  .WithMany(h => h.HospitalImages)
                  .HasForeignKey(e => e.HospitalId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure HospitalRegistration entity
        modelBuilder.Entity<HospitalRegistrationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.HospitalName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Address).IsRequired();
            entity.Property(e => e.LicenseFile).IsRequired();
            entity.Property(e => e.BusinessCertificateFile).IsRequired();
            entity.Property(e => e.IdentityCardFile).IsRequired();
            entity.Property(e => e.TaxCode).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.TaxCode);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

            // Foreign key relationship (optional)
            entity.HasOne(e => e.Hospital)
                  .WithMany()
                  .HasForeignKey(e => e.HospitalId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
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
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is HospitalEntity hospital)
            {
                if (entry.State == EntityState.Added)
                {
                    hospital.CreatedAt = DateTime.Now;
                }
                hospital.UpdatedAt = DateTime.Now;
            }
            else if (entry.Entity is SubscriptionPlanEntity subscription)
            {
                if (entry.State == EntityState.Added)
                {
                    subscription.CreatedAt = DateTime.Now;
                }
                subscription.UpdatedAt = DateTime.Now;
            }
            else if (entry.Entity is HospitalSubscriptionEntity hospitalSubscription)
            {
                if (entry.State == EntityState.Added)
                {
                    hospitalSubscription.CreatedAt = DateTime.Now;
                }
                hospitalSubscription.UpdatedAt = DateTime.Now;
            }
            else if (entry.Entity is HospitalRegistrationEntity registration)
            {
                if (entry.State == EntityState.Added)
                {
                    registration.CreatedAt = DateTime.Now;
                }
                registration.UpdatedAt = DateTime.Now;
            }
        }
    }
}
