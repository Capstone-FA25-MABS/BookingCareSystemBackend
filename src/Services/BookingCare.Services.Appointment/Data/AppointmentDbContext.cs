using BookingCare.Services.Appointment.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Appointment.Data;

public class AppointmentDbContext : DbContext
{
    public DbSet<AppointmentEntity> Appointments { get; set; }

    public AppointmentDbContext(DbContextOptions<AppointmentDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Appointment
        modelBuilder.Entity<AppointmentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PatientId)
                .IsRequired();

            entity.Property(e => e.AppointmentDate)
                .IsRequired();

            entity.Property(e => e.AppointmentTimeId)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.AppointmentType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // 1. Check trùng lịch bệnh nhân
            entity.HasIndex(e => new { e.PatientId, e.AppointmentDate, e.AppointmentTimeId })
                .IsUnique()
                .HasDatabaseName("IX_Patient_Date_Time_Unique");

            // 2. Check trùng lịch bác sĩ (nếu cần rule không cho 2 lịch cùng lúc)
            entity.HasIndex(e => new { e.DoctorId, e.AppointmentDate, e.AppointmentTimeId })
                .IsUnique()
                .HasDatabaseName("IX_Doctor_Date_Time_Unique");

            // 3. Lấy lịch theo bệnh nhân (lọc theo ngày & trạng thái)
            entity.HasIndex(e => new { e.PatientId, e.AppointmentDate, e.Status })
                .HasDatabaseName("IX_Patient_Date_Status");

            // 4. Lấy lịch theo bác sĩ (lọc theo ngày & trạng thái)
            entity.HasIndex(e => new { e.DoctorId, e.AppointmentDate, e.Status })
                .HasDatabaseName("IX_Doctor_Date_Status");
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
            .Where(e => e.Entity is AppointmentEntity);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added && entry.Entity is AppointmentEntity addedAppointment)
            {
                addedAppointment.CreatedAt = DateTime.UtcNow;
                addedAppointment.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified && entry.Entity is AppointmentEntity modifiedAppointment)
            {
                modifiedAppointment.UpdatedAt = DateTime.UtcNow;
                entry.Property(nameof(modifiedAppointment.CreatedAt)).IsModified = false;
            }
        }
    }
}
