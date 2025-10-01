using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Services.Appointment.Models.Entities;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Appointment.Enums;

namespace BookingCare.Services.Appointment.Repositories;

/// <summary>
/// Repository interface for Appointment service operations
/// </summary>
public interface IAppointmentRepository
{
    // Appointment operations
    Task<AppointmentEntity?> GetAppointmentByIdAsync(Guid id);
    Task<AppointmentEntity> CreateAppointmentAsync(AppointmentEntity appointment);
    Task<(List<AppointmentEntity> Appointments, int TotalCount)> GetAppointmentsAsync(AppointmentQueryRequest query, Role role);
    Task<bool> HasConflictingAppointmentAsync(Guid patientId, DateTime appointmentDate, Guid appointmentTimeId, Guid? excludeAppointmentId = null);
    Task<bool> IsDoctorAvailableAsync(Guid doctorId, DateTime appointmentDate, Guid appointmentTimeId, Guid? excludeAppointmentId = null);

    // Status operations
    Task<bool> UpdateAppointmentStatusAsync(Guid appointmentId, AppointmentStatus status, string? result = null);
}
