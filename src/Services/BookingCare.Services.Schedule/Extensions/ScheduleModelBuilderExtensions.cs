using Microsoft.EntityFrameworkCore;
using BookingCare.Services.Schedule.Models.Entities;
using BookingCare.Services.Schedule.Enums;
using System.Collections.Generic;

namespace BookingCare.Services.Schedule.Extensions;

internal static class ScheduleModelBuilderExtensions
{
    public static void ConfigureScheduleEntities(this ModelBuilder modelBuilder)
    {
        // DoctorDailyScheduleEntity
        modelBuilder.Entity<DoctorDailyScheduleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DoctorId).IsRequired();
            entity.Property(e => e.ScheduleDate).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.SchedulePatterns)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<SchedulePatterns>>(v, new System.Text.Json.JsonSerializerOptions()) ?? new List<SchedulePatterns>())
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.HasIndex(e => new { e.DoctorId, e.ScheduleDate }).IsUnique();
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.ScheduleDate);
        });

        // ServiceScheduleEntity
        modelBuilder.Entity<ServiceScheduleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ServiceId).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.SchedulePatterns)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<SchedulePatterns>>(v, new System.Text.Json.JsonSerializerOptions()) ?? new List<SchedulePatterns>())
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.HasIndex(e => e.ServiceId);
            entity.HasIndex(e => e.ClinicId);
        });

        // DoctorScheduleExceptionEntity
        modelBuilder.Entity<DoctorScheduleExceptionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DoctorId).IsRequired();
            entity.Property(e => e.ExceptionDate).IsRequired();
            entity.Property(e => e.ExceptionType).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.IsAvailable).HasDefaultValue(false);
            entity.Property(e => e.Reason).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.AppointmentTime).HasConversion<int>();
            entity.HasIndex(e => new { e.DoctorId, e.ExceptionDate });
            entity.HasIndex(e => e.ExceptionType);
        });

        // ClinicExceptionEntity
        modelBuilder.Entity<ClinicExceptionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClinicId).IsRequired();
            entity.Property(e => e.ExceptionDate).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(255);
            entity.HasIndex(e => new { e.ClinicId, e.ExceptionDate });
        });
    }
}
