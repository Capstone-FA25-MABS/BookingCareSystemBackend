using BookingCare.Services.Appointment.Models.DTOs;

namespace BookingCare.Services.Appointment.Services;

/// <summary>
/// Service interface for Appointment service operations
/// </summary>
public interface IAppointmentService
{
    // Appointment operations
    Task<Guid> CreateAppointmentAsync(CreateAppointmentRequest request);
    /// <summary>
    /// Get appointment by ID with enriched data for patient view
    /// </summary>
    Task<AppointmentResponse?> GetAppointmentByIdForPatientAsync(Guid id);
    Task<AppointmentListResponse> GetAppointmentsByPatientAsync(AppointmentQueryRequest query);
    Task<AppointmentListResponse> GetAppointmentsForManagementAsync(AppointmentQueryRequest query);

    // Status operations
    Task<bool> UpdateAppointmentStatusAsync(UpdateAppointmentStatusRequest request);

    /// <summary>
    /// Cancel an appointment with validation (must be 24 hours before appointment)
    /// Publishes event to trigger refund and notification
    /// </summary>
    Task<bool> CancelAppointmentAsync(CancelAppointmentRequest request);

    // Validation operations
    Task<bool> ValidateAppointmentAsync(CreateAppointmentRequest request);
}
