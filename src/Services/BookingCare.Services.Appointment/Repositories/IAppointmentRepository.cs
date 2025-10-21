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
    Task<bool> HasConflictingAppointmentAsync(Guid patientId, DateTime appointmentDate, AppointmentTime appointmentTimeId, Guid? excludeAppointmentId = null);
    Task<bool> IsDoctorAvailableAsync(Guid doctorId, DateTime appointmentDate, AppointmentTime appointmentTimeId, Guid? excludeAppointmentId = null);

    // Status operations
    Task<bool> UpdateAppointmentStatusAsync(Guid appointmentId, AppointmentStatus status, string? result = null);

    /// <summary>
    /// Cancel an appointment with cancellation reason
    /// Optimized method specifically for cancellation that takes the full entity
    /// </summary>
    Task<bool> CancelAppointmentAsync(AppointmentEntity appointment, string cancellationReason, string cancelledBy);

    /// <summary>
    /// Delete an appointment completely from the database
    /// Used when payment fails to free up the time slot completely
    /// </summary>
    Task<bool> DeleteAppointmentAsync(AppointmentEntity appointment);

    // Statistics operations
    /// <summary>
    /// Get counts for all appointment statuses for a specific user or organization
    /// Supports filtering by PatientId, DoctorId, HospitalId, or all (for ADMIN)
    /// </summary>
    Task<Dictionary<AppointmentStatus, int>> GetStatusCountsByUserAsync(
        Guid? patientId = null,
        Guid? doctorId = null,
        Guid? hospitalId = null,
        bool countAll = false);

    // Background service operations
    /// <summary>
    /// Get overdue appointments by status
    /// Returns appointments where AppointmentDate is before the reference date
    /// </summary>
    Task<List<AppointmentEntity>> GetOverdueAppointmentsByStatusAsync(
        AppointmentStatus status,
        DateTime referenceDate);

    /// <summary>
    /// Get all booked appointment time IDs for a doctor on a specific date
    /// Returns appointments with status PENDING, CONFIRMED, or COMPLETED
    /// </summary>
    Task<List<AppointmentTime>> GetBookedAppointmentTimesAsync(Guid doctorId, DateOnly appointmentDate);
}
