namespace BookingCare.Services.Payment.Enums;

/// <summary>
/// Status for hospital payout records
/// </summary>
public enum PayoutStatus
{
    /// <summary>
    /// Payout is pending - waiting for admin to process
    /// </summary>
    PENDING,

    /// <summary>
    /// Admin has marked the payout as paid/completed
    /// </summary>
    COMPLETED,
}
