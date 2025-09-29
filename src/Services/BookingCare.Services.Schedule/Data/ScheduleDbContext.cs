using BookingCare.Services.Schedule.Models.Entities;
using BookingCare.Services.Schedule.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Schedule.Data;

/// <summary>
/// Database context for Schedule Service
/// </summary>
public class ScheduleDbContext : DbContext
{
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

            // Configure SchedulePatterns collection as JSON
            entity.Property(e => e.SchedulePatterns)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<SchedulePatterns>>(v, new System.Text.Json.JsonSerializerOptions()) ?? new List<SchedulePatterns>())
                .HasColumnType("nvarchar(max)")
                .IsRequired();

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

            // Configure enum as integer in database
            entity.Property(e => e.AppointmentTime)
                .HasConversion<int>();

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

            // Configure SchedulePatterns collection as JSON
            entity.Property(e => e.SchedulePatterns)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<SchedulePatterns>>(v, new System.Text.Json.JsonSerializerOptions()) ?? new List<SchedulePatterns>())
                .HasColumnType("nvarchar(max)")
                .IsRequired();

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
            if (entityEntry.Entity is DoctorDailyScheduleEntity doctorSchedule)
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