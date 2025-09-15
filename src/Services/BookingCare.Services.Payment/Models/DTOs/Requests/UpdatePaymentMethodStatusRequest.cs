using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO ?? c?p nh?t tr?ng thái payment method
/// </summary>
public class UpdatePaymentMethodStatusRequest
{
    /// <summary>
    /// ID c?a payment method
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tr?ng thái m?i
    /// </summary>
    public PaymentMethodStatus Status { get; set; }
}