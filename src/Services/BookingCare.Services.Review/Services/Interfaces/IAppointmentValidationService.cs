using BookingCare.Services.Review.Models.DTOs;

namespace BookingCare.Services.Review.Services.Interfaces;

/// <summary>
/// Interface for validating patient appointment history with Appointment service
/// </summary>
public interface IAppointmentValidationService
{
    /// <summary>
    /// Checks if patient has completed appointment with specified doctor
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>Appointment history validation result</returns>
    Task<AppointmentHistoryValidationResult> HasCompletedAppointmentWithDoctorAsync(Guid patientId, Guid doctorId);

    /// <summary>
    /// Checks if patient has completed appointment with specified service
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="serviceId">Service ID</param>
    /// <returns>Appointment history validation result</returns>
    Task<AppointmentHistoryValidationResult> HasCompletedAppointmentWithServiceAsync(Guid patientId, Guid serviceId);
}