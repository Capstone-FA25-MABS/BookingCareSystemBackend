namespace BookingCare.Services.Appointment.Models.Internal;

/// <summary>
/// Internal model for hospital notification information
/// </summary>
internal class HospitalNotificationInfo
{
    public Guid HospitalId { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
}
