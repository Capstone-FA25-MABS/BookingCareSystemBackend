using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO ?? c?p nh?t tr?ng thái payment
/// </summary>
public class UpdatePaymentStatusRequest
{
    /// <summary>
    /// ID c?a payment
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tr?ng thái m?i
    /// </summary>
    public PaymentStatus Status { get; set; }
}