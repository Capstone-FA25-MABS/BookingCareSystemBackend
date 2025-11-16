using BookingCare.Services.Hospital.Models.Entities;
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
    public DbSet<HospitalServiceTypeEntity> HospitalServiceTypes { get; set; }
    public DbSet<HospitalServiceMedicalEntity> HospitalServiceMedicals { get; set; }

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
            // Unique constraint on (Name, BillingCycle) to allow same name with different billing cycles
            entity.HasIndex(e => new { e.Name, e.BillingCycle })
                .IsUnique()
                .HasDatabaseName("IX_subscription_plans_name_billing_cycle_unique");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(10,2)");
            entity.Property(e => e.BillingCycle).HasMaxLength(20).HasDefaultValue("MONTHLY");
            entity.Property(e => e.MaxDoctors).HasDefaultValue(0);
            entity.Property(e => e.MaxSpecialties).HasDefaultValue(0);
            entity.Property(e => e.MaxAppointments).HasDefaultValue(0);
            entity.Property(e => e.MaxServices).HasDefaultValue(0);

            // Allow null for unlimited (use -1 to represent null in database)
            entity.Property(e => e.MaxDoctors).IsRequired(false);
            entity.Property(e => e.MaxSpecialties).IsRequired(false);
            entity.Property(e => e.MaxAppointments).IsRequired(false);
            entity.Property(e => e.MaxServices).IsRequired(false);
            entity.Property(e => e.Features).HasColumnType("NVARCHAR(MAX)");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

            // Convert Status enum to string in database
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(Status.ACTIVE);

            // Enhanced check constraints
            entity.ToTable(t => t.HasCheckConstraint("CK_subscription_plans_billing_cycle", "billing_cycle IN ('MONTHLY', 'QUARTERLY', 'YEARLY')"));
            entity.ToTable(t => t.HasCheckConstraint("CK_subscription_plans_price", "price >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_subscription_plans_max_doctors", "(max_doctors IS NULL OR max_doctors >= 0)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_subscription_plans_max_specialties", "(max_specialties IS NULL OR max_specialties >= 0)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_subscription_plans_max_appointments", "(max_appointments IS NULL OR max_appointments >= 0)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_subscription_plans_max_services", "(max_services IS NULL OR max_services >= 0)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_subscription_plans_status", "status IN ('ACTIVE', 'INACTIVE')"));
        });

        // Configure HospitalSubscription entity
        modelBuilder.Entity<HospitalSubscriptionEntity>(entity =>
        {
            entity.HasKey(e => e.HospitalSubscriptionId);
            entity.Property(e => e.HospitalSubscriptionId).ValueGeneratedOnAdd();
            entity.Property(e => e.StartDate).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.EndDate).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>(); // Convert enum to string in database
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

            // Usage counts - Track actual usage against subscription limits
            entity.Property(e => e.DoctorCount).HasDefaultValue(0);
            entity.Property(e => e.SpecialtyCount).HasDefaultValue(0);
            entity.Property(e => e.AppointmentCount).HasDefaultValue(0);
            entity.Property(e => e.ServiceCount).HasDefaultValue(0);

            // Foreign key relationships
            entity.HasOne(e => e.Hospital)
                  .WithMany(h => h.HospitalSubscriptions)
                  .HasForeignKey(e => e.HospitalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SubscriptionPlan)
                  .WithMany(s => s.HospitalSubscriptions)
                  .HasForeignKey(e => e.SubscriptionId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete of subscription plans

            // Enhanced check constraints
            entity.ToTable(t => t.HasCheckConstraint("CK_hospital_subscriptions_status",
                "status IN ('ACTIVE', 'EXPIRED', 'CANCELLED', 'PENDING', 'TRIAL')"));
            entity.ToTable(t => t.HasCheckConstraint("CK_hospital_subscriptions_end_date",
                "end_date > start_date"));

            // Unique constraint: Only one active subscription per hospital
            entity.HasIndex(e => new { e.HospitalId, e.Status })
                .HasFilter("status IN ('ACTIVE', 'TRIAL')") // Only one active or trial subscription per hospital
                .HasDatabaseName("IX_hospital_subscriptions_hospital_active_unique");
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

        // Configure HospitalServiceType entity (Many-to-Many)
        modelBuilder.Entity<HospitalServiceTypeEntity>(entity =>
        {
            entity.HasKey(e => new { e.HospitalId, e.ServiceTypeId });

            entity.HasOne(e => e.Hospital)
                  .WithMany(h => h.HospitalServiceTypes)
                  .HasForeignKey(e => e.HospitalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

            // Note: ServiceType relationship is defined in Doctor service
        });

        // Configure HospitalServiceMedical entity (Many-to-Many)
        modelBuilder.Entity<HospitalServiceMedicalEntity>(entity =>
        {
            entity.HasKey(e => new { e.HospitalId, e.ServiceMedicalId });

            entity.HasOne(e => e.Hospital)
                  .WithMany(h => h.HospitalServiceMedicals)
                  .HasForeignKey(e => e.HospitalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

            // Note: ServiceMedical relationship is defined in ServiceMedical service
        });

        // Configure HospitalImage entity
        modelBuilder.Entity<HospitalImageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ImageUrl).IsRequired();
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

            // Representative Information
            entity.Property(e => e.RepresentativeName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RepresentativeEmail).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.RepresentativeEmail);
            entity.Property(e => e.RepresentativePhone).IsRequired().HasMaxLength(20);

            // Hospital Information
            entity.Property(e => e.HospitalName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.HospitalEmail).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.HospitalEmail);
            entity.Property(e => e.HospitalPhone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Address).IsRequired();
            entity.Property(e => e.TaxCode).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.TaxCode);

            // Files
            entity.Property(e => e.LicenseFile).IsRequired();
            entity.Property(e => e.BusinessCertificateFile).IsRequired();
            entity.Property(e => e.IdentityCardFile).IsRequired();

            // Status and metadata
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
            var isAdded = entry.State == EntityState.Added;
            var currentTime = DateTime.Now;

            switch (entry.Entity)
            {
                case HospitalEntity hospital:
                    UpdateEntityTimestamp(isAdded,
                        () => hospital.CreatedAt = currentTime,
                        () => hospital.UpdatedAt = currentTime);
                    break;

                case SubscriptionPlanEntity subscription:
                    UpdateEntityTimestamp(isAdded,
                        () => subscription.CreatedAt = currentTime,
                        () => subscription.UpdatedAt = currentTime);
                    break;

                case HospitalSubscriptionEntity hospitalSubscription:
                    UpdateEntityTimestamp(isAdded,
                        () => hospitalSubscription.CreatedAt = currentTime,
                        () => hospitalSubscription.UpdatedAt = currentTime);
                    break;

                case HospitalRegistrationEntity registration:
                    UpdateEntityTimestamp(isAdded,
                        () => registration.CreatedAt = currentTime,
                        () => registration.UpdatedAt = currentTime);
                    break;
            }
        }
    }

    private static void UpdateEntityTimestamp(
        bool isAdded,
        Action setCreatedAt,
        Action setUpdatedAt)
    {
        if (isAdded)
        {
            setCreatedAt();
        }
        setUpdatedAt();
    }
}
