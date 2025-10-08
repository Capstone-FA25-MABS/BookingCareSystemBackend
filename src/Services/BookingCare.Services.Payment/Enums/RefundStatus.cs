namespace BookingCare.Services.Payment.Enums;

/// <summary>
/// Status of a refund request
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// Waiting - user has no bank account or no active bank account
    /// </summary>
    WAITING,

    /// <summary>
    /// Pending - waiting for staff to process the refund
    /// </summary>
    PENDING,

    /// <summary>
    /// Completed - refund succeeded
    /// </summary>
    COMPLETED
}