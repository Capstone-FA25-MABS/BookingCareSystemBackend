namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Service interface for retrieving appointment details via gRPC
/// </summary>
public interface IAppointmentDetailsService
{
    /// <summary>
    /// Get appointment details by appointment ID
    /// </summary>
    /// <param name="appointmentId">The appointment ID</param>
    /// <returns>Appointment details or null if not found</returns>

    Task<AppointmentDetailsDto?> GetAppointmentDetailsAsync(Guid appointmentId);
}

/// <summary>
/// DTO for appointment details from gRPC call
/// </summary>
public class AppointmentDetailsDto
{
    public Guid AppointmentId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string AppointmentType { get; set; } = string.Empty;
    public Guid? DoctorId { get; set; }
    public Guid? ServiceId { get; set; }
    public Guid? HospitalId { get; set; }
}