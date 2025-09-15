using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO ?? t?o payment cho subscription (clinic ??ng ký gói)
/// </summary>
public class CreateSubscriptionPaymentRequest
{
    /// <summary>
    /// ID c?a subscription (b?t bu?c)
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// ID c?a clinic (b?t bu?c)
    /// </summary>
    public Guid ClinicId { get; set; }

    /// <summary>
    /// S? ti?n thanh toán
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ID ph??ng th?c thanh toán
    /// </summary>
    public Guid PaymentMethodId { get; set; }
}