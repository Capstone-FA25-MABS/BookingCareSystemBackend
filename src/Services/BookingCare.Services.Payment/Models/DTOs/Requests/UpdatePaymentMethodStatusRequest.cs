using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO to update payment method status
/// </summary>
public class UpdatePaymentMethodStatusRequest
{
    /// <summary>
    /// ID of the payment method
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// New status
    /// </summary>
    public PaymentMethodStatus Status { get; set; }
}