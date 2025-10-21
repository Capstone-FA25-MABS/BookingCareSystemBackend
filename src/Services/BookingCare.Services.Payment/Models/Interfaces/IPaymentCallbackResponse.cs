namespace BookingCare.Services.Payment.Models.Interfaces;

/// <summary>
/// Interface for standardizing payment gateway callback responses
/// </summary>
public interface IPaymentCallbackResponse
{
    /// <summary>
    /// Response code from the payment gateway
    /// </summary>
    string ResponseCode { get; }

    /// <summary>
    /// Transaction ID or reference from the payment gateway
    /// </summary>
    string? TransactionId { get; }

    /// <summary>
    /// Whether the payment was successful
    /// </summary>
    bool IsSuccess { get; }
}