using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO để cập nhật trạng thái payment
/// </summary>
public class UpdatePaymentStatusRequest
{
    /// <summary>
    /// ID của payment
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Trạng thái mới
    /// </summary>
    public PaymentStatus Status { get; set; }
}