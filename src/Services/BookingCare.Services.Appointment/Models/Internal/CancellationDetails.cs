namespace BookingCare.Services.Appointment.Models.Internal;

/// <summary>
/// Internal model for cancellation details
/// </summary>
internal class CancellationDetails
{
    public bool IsStaffCancellation { get; set; }
    public string CancelledBy { get; set; } = string.Empty;
    public decimal RefundPercentage { get; set; }
}
