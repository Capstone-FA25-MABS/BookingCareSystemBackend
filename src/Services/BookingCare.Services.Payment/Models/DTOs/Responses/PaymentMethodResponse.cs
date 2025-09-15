using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO cho payment method
/// </summary>
public class PaymentMethodResponse
{
    /// <summary>
    /// ID c?a ph??ng th?c thanh toán
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tên ph??ng th?c thanh toán
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô t? ph??ng th?c thanh toán
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tr?ng thái ph??ng th?c thanh toán
    /// </summary>
    public PaymentMethodStatus Status { get; set; }
}