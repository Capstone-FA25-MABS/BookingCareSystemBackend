using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO tạo payment cho subscription (clinic đăng ký gói)
/// </summary>
public class CreateSubscriptionPaymentRequest
{
    /// <summary>
    /// ID của subscription (bắt buộc)
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// ID của clinic (bắt buộc)
    /// </summary>
    public Guid ClinicId { get; set; }

    /// <summary>
    /// Số tiền thanh toán
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ID phương thức thanh toán
    /// </summary>
    public Guid PaymentMethodId { get; set; }
}