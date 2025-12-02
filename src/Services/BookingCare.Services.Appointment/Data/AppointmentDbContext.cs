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

            // ============================================
            // UNIQUE INDEXES (for conflict checking)
            // Filtered: exclude CANCELLED và COMPLETED để cho phép đặt lại slot
            // ============================================

            // 1. Check trùng lịch bệnh nhân (bản thân) - mỗi patient chỉ có 1 appointment/slot khi đặt cho bản thân
            // RelativeId IS NULL = đặt cho bản thân
            entity.HasIndex(e => new { e.PatientId, e.AppointmentDate, e.AppointmentTimeId })
                .IsUnique()
                .HasDatabaseName("IX_Patient_Date_Time_Unique")
                .HasFilter("[RelativeId] IS NULL AND [Status] IN ('PENDING', 'CONFIRMED')");

            // 1b. Check trùng lịch người thân - mỗi relative chỉ có 1 appointment/slot
            // RelativeId IS NOT NULL = đặt cho người thân
            entity.HasIndex(e => new { e.RelativeId, e.AppointmentDate, e.AppointmentTimeId })
                .IsUnique()
                .HasDatabaseName("IX_Relative_Date_Time_Unique")
                .HasFilter("[RelativeId] IS NOT NULL AND [Status] IN ('PENDING', 'CONFIRMED')");

            // 2. Check trùng lịch bác sĩ - mỗi doctor chỉ có 1 appointment/slot
            entity.HasIndex(e => new { e.DoctorId, e.AppointmentDate, e.AppointmentTimeId })
                .IsUnique()
                .HasDatabaseName("IX_Doctor_Date_Time_Unique")
                .HasFilter("[DoctorId] IS NOT NULL AND [Status] IN ('PENDING', 'CONFIRMED')");

            // 3. Check trùng lịch dịch vụ - mỗi service chỉ có 1 appointment/slot
            // (Nếu service có giới hạn slot, cần logic khác - đây chỉ là unique per slot)
            entity.HasIndex(e => new { e.ServiceId, e.AppointmentDate, e.AppointmentTimeId })
                .HasDatabaseName("IX_Service_Date_Time")
                .HasFilter("[ServiceId] IS NOT NULL AND [Status] IN ('PENDING', 'CONFIRMED')");

            // ============================================
            // QUERY INDEXES (for listing/filtering)
            // Non-unique, optimized for common query patterns
            // ============================================

            // 4. Lấy lịch theo bệnh nhân (lọc theo ngày & trạng thái)
            entity.HasIndex(e => new { e.PatientId, e.AppointmentDate, e.Status })
                .HasDatabaseName("IX_Patient_Date_Status");

            // 5. Lấy lịch theo bác sĩ (lọc theo ngày & trạng thái)
            entity.HasIndex(e => new { e.DoctorId, e.AppointmentDate, e.Status })
                .HasDatabaseName("IX_Doctor_Date_Status")
                .HasFilter("[DoctorId] IS NOT NULL");

            // 6. Lấy lịch theo dịch vụ (lọc theo ngày & trạng thái)
            entity.HasIndex(e => new { e.ServiceId, e.AppointmentDate, e.Status })
                .HasDatabaseName("IX_Service_Date_Status")
                .HasFilter("[ServiceId] IS NOT NULL");
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
