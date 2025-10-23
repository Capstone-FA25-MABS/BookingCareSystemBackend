namespace BookingCare.Services.Appointment.Models.Internal;

/// <summary>
/// Internal model for doctor basic information (used in refund history)
/// </summary>
internal class DoctorBasicInfo
{
    public Guid DoctorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
}

