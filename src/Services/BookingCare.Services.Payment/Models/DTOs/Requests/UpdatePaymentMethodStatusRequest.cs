using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO cập nhật trạng thái payment method
/// </summary>
public class UpdatePaymentMethodStatusRequest
{
    /// <summary>
    /// ID của payment method
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Trạng thái mới
    /// </summary>
    public PaymentMethodStatus Status { get; set; }
}