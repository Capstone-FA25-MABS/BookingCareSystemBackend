namespace BookingCare.Services.Payment.Enums;

/// <summary>
/// Represents the status of a payment transaction.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending and has not been completed yet.
    /// </summary>
    PENDING,

    /// <summary>
    /// Payment has been successfully completed.
    /// </summary>
    COMPLETED,

    /// <summary>
    /// Payment has failed.
    /// </summary>
    FAILED,

    /// <summary>
    /// Payment has been refunded.
    /// </summary>
    REFUNDED
}