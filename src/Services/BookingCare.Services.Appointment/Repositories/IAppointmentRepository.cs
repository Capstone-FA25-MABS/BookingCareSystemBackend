using BookingCare.Services.Appointment.Enums;
using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Services.Appointment.Models.Entities;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Appointment.Repositories;

/// <summary>
/// Repository interface for Appointment service operations
/// </summary>
public interface IAppointmentRepository
{
    // Appointment operations
    Task<AppointmentEntity?> GetAppointmentByIdAsync(Guid id);
    Task<AppointmentEntity> CreateAppointmentAsync(AppointmentEntity appointment);
    Task<(List<AppointmentEntity> Appointments, int TotalCount)> GetAppointmentsAsync(
        AppointmentQueryRequest query,
        Role role
    );
    Task<bool> HasConflictingAppointmentAsync(
        Guid patientId,
        DateTime appointmentDate,
        AppointmentTime appointmentTimeId,
        Guid? relativeId = null,
        Guid? excludeAppointmentId = null
    );
    Task<bool> IsDoctorAvailableAsync(
        Guid doctorId,
        DateTime appointmentDate,
        AppointmentTime appointmentTimeId,
        Guid? excludeAppointmentId = null
    );
    Task<bool> IsServiceMedicalAvailableAsync(
        Guid serviceId,
        DateTime appointmentDate,
        AppointmentTime appointmentTimeId,
        Guid? excludeAppointmentId = null
    );

    // Status operations
    Task<bool> UpdateAppointmentStatusAsync(
        Guid appointmentId,
        AppointmentStatus status,
        string? result = null
    );

    /// <summary>
    /// Update an existing appointment (for reschedule operations)
    /// </summary>
    Task<bool> UpdateAppointmentAsync(AppointmentEntity appointment);

    /// <summary>
    /// Get appointments with expired reschedule tokens for cleanup
    /// </summary>
    Task<List<AppointmentEntity>> GetAppointmentsWithExpiredTokensAsync(DateTime now);

    /// <summary>
    /// Cancel an appointment with cancellation reason
    /// Optimized method specifically for cancellation that takes the full entity
    /// </summary>
    Task<bool> CancelAppointmentAsync(
        AppointmentEntity appointment,
        string cancellationReason,
        string cancelledBy
    );

    /// <summary>
    /// Delete an appointment completely from the database
    /// Used when payment fails to free up the time slot completely
    /// </summary>
    Task<bool> DeleteAppointmentAsync(AppointmentEntity appointment);

    // Statistics operations
    /// <summary>
    /// Get counts for all appointment statuses for a specific user or organization
    /// using a single optimized query.
    /// All filters are encapsulated inside <see cref="AppointmentStatusFilter"/>.
    /// </summary>
    Task<Dictionary<AppointmentStatus, int>> GetStatusCountsByUserAsync(
        AppointmentStatusFilter filter
    );
    Task<List<AppointmentEntity>> GetAppointmentsForHospitalAsync(
        Guid hospitalId,
        DateTime fromDate,
        DateTime toDate
    );
    Task<Dictionary<Guid, DateTime>> GetPatientFirstAppointmentsAsync(Guid hospitalId);

    // Background service operations
    /// <summary>
    /// Get overdue appointments by status
    /// Returns appointments where AppointmentDate is before the reference date
    /// </summary>
    Task<List<AppointmentEntity>> GetOverdueAppointmentsByStatusAsync(
        AppointmentStatus status,
        DateTime referenceDate
    );

    /// <summary>
    /// Get all booked appointment time IDs for a doctor on a specific date
    /// Returns appointments with status PENDING, CONFIRMED, or COMPLETED
    /// </summary>
    Task<List<AppointmentTime>> GetBookedAppointmentTimesAsync(
        Guid doctorId,
        DateOnly appointmentDate
    );

    /// <summary>
    /// Get all booked appointment time IDs for a service medical on a specific date
    /// Returns appointments with status PENDING, CONFIRMED, or COMPLETED
    /// </summary>
    Task<List<AppointmentTime>> GetBookedAppointmentTimesByServiceAsync(
        Guid serviceId,
        DateOnly appointmentDate
    );

    /// <summary>
    /// Get booked slot counts for a specialty (hospital assigns doctor mode)
    /// Returns count of PENDING/CONFIRMED appointments per time slot
    /// </summary>
    Task<Dictionary<AppointmentTime, int>> GetSpecialtyBookedSlotCountsAsync(
        Guid hospitalId,
        Guid specialtyId,
        DateOnly appointmentDate,
        AppointmentType appointmentType
    );

    /// <summary>
    /// NEW: Get completed appointments by patient with optional doctor or service filter (for Review service validation)
    /// Returns appointments with COMPLETED status for appointment history validation
    /// </summary>
    Task<List<AppointmentEntity>> GetCompletedAppointmentsByPatientAsync(
        Guid patientId,
        Guid? doctorId = null,
        Guid? serviceId = null
    );

    /// <summary>
    /// Get appointments by their IDs (for Payment service payout validation)
    /// </summary>
    Task<List<AppointmentEntity>> GetAppointmentsByIdsAsync(List<Guid> appointmentIds);

    #region Assign Doctor To Appointment (NEW flow)

    /// <summary>
    /// Get completed appointments for a patient at a specific hospital and specialty
    /// Used to find previous doctors who treated this patient
    /// </summary>
    Task<List<AppointmentEntity>> GetCompletedAppointmentsForPatientAsync(
        Guid patientId,
        Guid hospitalId,
        Guid specialtyId
    );

    /// <summary>
    /// Get booking counts (completed appointments) for multiple doctors
    /// </summary>
    Task<Dictionary<Guid, int>> GetDoctorBookingCountsAsync(List<Guid> doctorIds);

    /// <summary>
    /// Get doctor IDs that have booked slots at a specific date/time
    /// Used to check availability
    /// </summary>
    Task<List<Guid>> GetDoctorsWithBookedSlotAsync(
        List<Guid> doctorIds,
        DateOnly date,
        AppointmentTime appointmentTimeId
    );

    #endregion
}
