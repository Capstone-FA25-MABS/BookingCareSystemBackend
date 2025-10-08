using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO to update payment status
/// </summary>
public class UpdatePaymentStatusRequest
{
    /// <summary>
    /// ID of the payment
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// New status
    /// </summary>
    public PaymentStatus Status { get; set; }
}