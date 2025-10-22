namespace BookingCare.Services.Notification.Models.DTOs;

/// <summary>
/// Data transfer object for building cancellation email with reschedule options
/// </summary>
public class CancellationWithOptionsEmailData
{
    /// <summary>
    /// Patient name
    /// </summary>
    public required string PatientName { get; init; }

    /// <summary>
    /// Original appointment date
    /// </summary>
    public required DateTime AppointmentDate { get; init; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    public required string CancellationReason { get; init; }

    /// <summary>
    /// Doctor name (optional)
    /// </summary>
    public string? DoctorName { get; init; }

    /// <summary>
    /// Hospital name (optional)
    /// </summary>
    public string? HospitalName { get; init; }

    /// <summary>
    /// Potential refund amount (if Option 4 is chosen)
    /// </summary>
    public decimal? PotentialRefundAmount { get; init; }

    /// <summary>
    /// Potential refund percentage (if Option 4 is chosen)
    /// </summary>
    public decimal? PotentialRefundPercentage { get; init; }

    /// <summary>
    /// URL for Option 1: Reschedule with same doctor
    /// </summary>
    public string? SameDoctorRescheduleUrl { get; init; }

    /// <summary>
    /// URL for Option 2: Confirm new doctor assigned by staff
    /// </summary>
    public string? ConfirmNewDoctorUrl { get; init; }

    /// <summary>
    /// URL for Option 3: Choose new doctor
    /// </summary>
    public string? ChooseNewDoctorUrl { get; init; }

    /// <summary>
    /// URL for Option 4: Request refund
    /// </summary>
    public string? RefundRequestUrl { get; init; }

    /// <summary>
    /// Token expiry date/time for reschedule options
    /// </summary>
    public DateTime? TokenExpiry { get; init; }
}

