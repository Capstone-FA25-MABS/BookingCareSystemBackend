using BookingCare.Services.Schedule.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Schedule.Data;

/// <summary>
/// Database context for Schedule Service
/// </summary>
public class ScheduleDbContext : DbContext
{
    public DbSet<AppointmentTimeEntity> AppointmentTimes { get; set; }
    public DbSet<SchedulePatternEntity> SchedulePatterns { get; set; }
    public DbSet<SchedulePatternSlotEntity> SchedulePatternSlots { get; set; }
    public DbSet<DoctorDailyScheduleEntity> DoctorDailySchedules { get; set; }
    public DbSet<DoctorScheduleExceptionEntity> DoctorScheduleExceptions { get; set; }
    public DbSet<ClinicExceptionEntity> ClinicExceptions { get; set; }
    public DbSet<ServiceScheduleEntity> ServiceSchedules { get; set; }

    public ScheduleDbContext(DbContextOptions<ScheduleDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure AppointmentTimeEntity
        modelBuilder.Entity<AppointmentTimeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.StartTime)
                .HasMaxLength(5)
                .IsRequired();

            entity.Property(e => e.EndTime)
                .HasMaxLength(5)
                .IsRequired();

            // Indexes for performance
            entity.HasIndex(e => new { e.StartTime, e.EndTime });
        });

        // Configure SchedulePatternEntity
        modelBuilder.Entity<SchedulePatternEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired()
                .HasDefaultValue("FULL_DAY");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Check constraint for valid pattern names
            entity.ToTable(t => t.HasCheckConstraint("CK_SchedulePattern_Name", 
                "[name] IN ('FULL_DAY', 'MORNING_ONLY', 'AFTERNOON_ONLY', 'EVENING_ONLY')"));

            // Index for performance
            entity.HasIndex(e => e.Name);
        });

        // Configure SchedulePatternSlotEntity
        modelBuilder.Entity<SchedulePatternSlotEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Pattern)
                .WithMany(e => e.SchedulePatternSlots)
                .HasForeignKey(e => e.PatternId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AppointmentTime)
                .WithMany(e => e.SchedulePatternSlots)
                .HasForeignKey(e => e.AppointmentTimeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite index for performance
            entity.HasIndex(e => new { e.PatternId, e.AppointmentTimeId });
        });

        // Configure DoctorDailyScheduleEntity
        modelBuilder.Entity<DoctorDailyScheduleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DoctorId)
                .IsRequired();

            entity.Property(e => e.ScheduleDate)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Pattern)
                .WithMany(e => e.DoctorDailySchedules)
                .HasForeignKey(e => e.PatternId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: one schedule per doctor per day
            entity.HasIndex(e => new { e.DoctorId, e.ScheduleDate })
                .IsUnique();

            // Index for performance
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.ScheduleDate);
        });

        // Configure DoctorScheduleExceptionEntity
        modelBuilder.Entity<DoctorScheduleExceptionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DoctorId)
                .IsRequired();

            entity.Property(e => e.ExceptionDate)
                .IsRequired();

            entity.Property(e => e.ExceptionType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(false);

            entity.Property(e => e.Reason)
                .HasMaxLength(255);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.AppointmentTime)
                .WithMany(e => e.DoctorScheduleExceptions)
                .HasForeignKey(e => e.AppointmentTimeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            entity.HasIndex(e => new { e.DoctorId, e.ExceptionDate });
            entity.HasIndex(e => e.ExceptionType);
        });

        // Configure ClinicExceptionEntity
        modelBuilder.Entity<ClinicExceptionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ClinicId)
                .IsRequired();

            entity.Property(e => e.ExceptionDate)
                .IsRequired();

            entity.Property(e => e.Reason)
                .HasMaxLength(255);

            // Index for performance
            entity.HasIndex(e => new { e.ClinicId, e.ExceptionDate });
        });

        // Configure ServiceScheduleEntity
        modelBuilder.Entity<ServiceScheduleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ServiceId)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Pattern)
                .WithMany(e => e.ServiceSchedules)
                .HasForeignKey(e => e.PatternId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            entity.HasIndex(e => e.ServiceId);
            entity.HasIndex(e => e.ClinicId);
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
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entityEntry in entries)
        {
            if (entityEntry.Entity is SchedulePatternEntity schedulePattern)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    schedulePattern.CreatedAt = DateTime.UtcNow;
                }
                schedulePattern.UpdatedAt = DateTime.UtcNow;
            }
            else if (entityEntry.Entity is DoctorDailyScheduleEntity doctorSchedule)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    doctorSchedule.CreatedAt = DateTime.UtcNow;
                }
                doctorSchedule.UpdatedAt = DateTime.UtcNow;
            }
            else if (entityEntry.Entity is ServiceScheduleEntity serviceSchedule)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    serviceSchedule.CreatedAt = DateTime.UtcNow;
                }
                serviceSchedule.UpdatedAt = DateTime.UtcNow;
            }
            else if (entityEntry.Entity is DoctorScheduleExceptionEntity exception)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    exception.CreatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}