namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response cho bank account
/// </summary>
public class BankAccountResponse
{
    /// <summary>
    /// ID c?a bank account
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID c?a user s? h?u tài kho?n ngân hàng
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Mã ngân hàng
    /// </summary>
    public string BankCode { get; set; } = string.Empty;

    /// <summary>
    /// Tên ngân hàng ??y ??
    /// </summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// S? tài kho?n ngân hàng (???c mask ?? b?o m?t)
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// S? tài kho?n ??y ?? (ch? hi?n th? cho ch? tài kho?n)
    /// </summary>
    public string? FullAccountNumber { get; set; }

    /// <summary>
    /// Tên ch? tài kho?n
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Có ph?i là tài kho?n m?c ??nh hay không
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Tr?ng thái ho?t ??ng
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Th?i gian t?o
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Th?i gian c?p nh?t cu?i cùng
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}