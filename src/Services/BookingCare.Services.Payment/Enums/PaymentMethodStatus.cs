namespace BookingCare.Services.Payment.Enums;

/// <summary>
/// Represents the status of a payment method.
/// </summary>
public enum PaymentMethodStatus
{
    /// <summary>
    /// Payment method is inactive and not available for use.
    /// </summary>
    INACTIVE,


    /// <summary>
    /// Payment method is active and available for use.
    /// </summary>
    ACTIVE,
}
