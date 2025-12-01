using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Appointment.Services;

/// <summary>
/// Service interface for Appointment service operations
/// </summary>
public interface IAppointmentService
{
    // Appointment operations
    Task<Guid> CreateAppointmentAsync(CreateAppointmentRequest request, bool skipPayment = false);

    /// <summary>
    /// Get appointment by ID with enriched data for patient view
    /// </summary>
    Task<AppointmentResponse?> GetAppointmentByIdForPatientAsync(Guid id);
    Task<AppointmentListResponse> GetAppointmentsByPatientAsync(AppointmentQueryRequest query);
    Task<AppointmentListResponse> GetAppointmentsForManagementAsync(AppointmentQueryRequest query);

    // Status operations
    Task<bool> UpdateAppointmentStatusAsync(UpdateAppointmentStatusRequest request);

    /// <summary>
    /// Update appointment result and automatically change status to COMPLETED
    /// If status is not COMPLETED, it will be changed to COMPLETED
    /// </summary>
    Task<bool> UpdateAppointmentResultAsync(UpdateAppointmentResultRequest request);

    /// <summary>
    /// Cancel an appointment with validation
    /// Publishes event to trigger refund/reschedule options and notification
    /// </summary>
    Task<RescheduleResponse?> CancelAppointmentAsync(CancelAppointmentRequest request);

    /// <summary>
    /// Generate reschedule token without cancelling appointment (lazy token generation)
    /// Used when patient clicks reschedule/choose new doctor button
    /// Appointment status remains unchanged until patient completes the reschedule flow
    /// </summary>
    Task<GenerateRescheduleTokenResponse> GenerateRescheduleTokenAsync(
        GenerateRescheduleTokenRequest request
    );

    /// <summary>
    /// Reschedule appointment with same doctor (Option 1)
    /// </summary>
    Task<bool> RescheduleSameDoctorAsync(RescheduleSameDoctorRequest request);

    /// <summary>
    /// Staff assigns new doctor (creates soft reservation) - Option 2 Step 1
    /// This creates a soft lock on the doctor's schedule until patient confirms or token expires
    /// </summary>
    Task<string> AssignNewDoctorAsync(AssignNewDoctorRequest request);

    /// <summary>
    /// Request refund for cancelled appointment (Option 4)
    /// Publishes event to Payment Service to create refund request
    /// </summary>
    Task<bool> RequestRefundAsync(RequestRefundRequest request);

    /// <summary>
    /// Choose new doctor (Option 3)
    /// Patient selects a different doctor from same hospital + specialty
    /// Handles 3 scenarios:
    /// - Same price: Update appointment directly
    /// - Higher price: Return payment URL for price difference
    /// - Lower price: Update appointment and create refund request
    /// </summary>
    Task<ChooseNewDoctorResponse> ChooseNewDoctorAsync(ChooseNewDoctorRequest request);

    /// <summary>
    /// Get available doctors for staff to assign (Option 2)
    /// Returns doctors from same hospital + specialty
    /// If checkAvailability = true, only returns doctors available at specified date/time
    /// If checkAvailability = false, returns all doctors (ignores date/time)
    /// </summary>
    Task<AvailableDoctorsResponse> GetAvailableDoctorsAsync(
        Guid hospitalId,
        Guid specialtyId,
        DateTime? appointmentDate,
        AppointmentTime? appointmentTimeId,
        bool checkAvailability = true
    );

    Task<StaffHospitalStatisticsResponse> GetHospitalStaffStatisticsAsync(StaffHospitalStatisticsRequest request);

    /// <summary>
    /// Get doctors for assignment flow (hospital staff assigns doctor to pending specialty appointment)
    /// Returns recommended doctors (sorted by experience, rating, booking count) and previous doctors
    /// </summary>
    Task<DoctorsForAssignmentResponse> GetDoctorsForAssignmentAsync(GetDoctorsForAssignmentRequest request);

    /// <summary>
    /// Assign doctor to a pending specialty appointment (NEW flow for "Hospital assigns doctor")
    /// This directly assigns the doctor and confirms the appointment
    /// Different from AssignNewDoctorAsync which is for cancel/reschedule flow
    /// </summary>
    Task<AssignDoctorToAppointmentResponse> AssignDoctorToAppointmentAsync(AssignDoctorToAppointmentRequest request);

    // Validation operations
    Task<bool> ValidateAppointmentAsync(CreateAppointmentRequest request);

    // Email notification operations
    /// <summary>
    /// Send appointment booking success email notification to patient
    /// This method is called by event handler when payment is successful
    /// </summary>
    Task<bool> SendAppointmentBookingSuccessEmailAsync(
        Guid appointmentId,
        Guid patientId,
        string accountId,
        decimal amount = 0
    );
}
