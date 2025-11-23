using BookingCare.Services.Schedule.Models.Entities;
using BookingCare.Services.Schedule.Enums;
using Microsoft.EntityFrameworkCore;

using BookingCare.Services.Schedule.Extensions;

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
    public DbSet<ServiceMedicalDailyScheduleEntity> ServiceMedicalDailySchedules { get; set; }
    public DbSet<ServiceMedicalScheduleExceptionEntity> ServiceMedicalScheduleExceptions { get; set; }

    public ScheduleDbContext(DbContextOptions<ScheduleDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use extension to configure schedule entities to avoid duplication
        modelBuilder.ConfigureScheduleEntities();
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
            else if (entityEntry.Entity is ServiceMedicalDailyScheduleEntity serviceMedicalSchedule)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    serviceMedicalSchedule.CreatedAt = DateTime.UtcNow;
                }
                serviceMedicalSchedule.UpdatedAt = DateTime.UtcNow;
            }
            else if (entityEntry.Entity is DoctorScheduleExceptionEntity exception && entityEntry.State == EntityState.Added)
            {
                exception.CreatedAt = DateTime.UtcNow;
            }
            else if (entityEntry.Entity is ServiceMedicalScheduleExceptionEntity serviceMedicalException && entityEntry.State == EntityState.Added)
            {
                serviceMedicalException.CreatedAt = DateTime.UtcNow;
            }
        }
    }
}