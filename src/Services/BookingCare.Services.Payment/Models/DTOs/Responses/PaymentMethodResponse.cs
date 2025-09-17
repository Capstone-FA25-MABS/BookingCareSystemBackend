using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO cho payment method
/// </summary>
public class PaymentMethodResponse
{
    /// <summary>
    /// ID của phương thức thanh toán
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tên phương thức thanh toán
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả phương thức thanh toán
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Trạng thái phương thức thanh toán

    /// </summary>
    public PaymentMethodStatus Status { get; set; }
}