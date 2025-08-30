namespace BookingCare.Shared.Common.Enums;

/// <summary>
/// Represents the type of transaction in the BookingCare system.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Transaction related to an appointment booking.
    /// </summary>
    APPOINTMENT,

    /// <summary>
    /// Transaction related to a subscription service.
    /// </summary>
    SUBSCRIPTION
}