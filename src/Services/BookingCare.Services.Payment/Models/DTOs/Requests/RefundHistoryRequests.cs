using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request ?? t?o refund history m?i
/// </summary>
public class CreateRefundHistoryRequest
{
    /// <summary>
    /// ID c?a payment c?n refund
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// ID c?a user yêu c?u refund
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid UserId { get; set; }

    /// <summary>
    /// ID c?a bank account ?? refund (optional - có th? ch?a có)
    /// </summary>
    public Guid? BankAccountId { get; set; }

    /// <summary>
    /// S? ti?n refund
    /// </summary>
    [Required]
    [JsonRequired]
    [Range(0.01, double.MaxValue, ErrorMessage = "S? ti?n refund ph?i l?n h?n 0")]
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Lý do refund
    /// </summary>
    [MaxLength(500, ErrorMessage = "Lý do refund không ???c v??t quá 500 ký t?")]
    public string? RefundReason { get; set; }
}

/// <summary>
/// Request ?? c?p nh?t tr?ng thái refund history
/// </summary>
public class UpdateRefundHistoryStatusRequest
{
    /// <summary>
    /// ID c?a refund history
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid Id { get; set; }

    /// <summary>
    /// Tr?ng thái m?i
    /// </summary>
    [Required]
    [JsonRequired]
    public RefundStatus Status { get; set; }

    /// <summary>
    /// ID c?a bank account ?? refund (khi chuy?n t? WAITING sang PENDING)
    /// </summary>
    public Guid? BankAccountId { get; set; }

    /// <summary>
    /// Ngày chuy?n ti?n (khi status = COMPLETED)
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Ghi chú t? staff
    /// </summary>
    [MaxLength(500, ErrorMessage = "Ghi chú không ???c v??t quá 500 ký t?")]
    public string? StaffNotes { get; set; }

    /// <summary>
    /// ID c?a staff x? lý
    /// </summary>
    public Guid? ProcessedByStaffId { get; set; }
}

/// <summary>
/// Request ?? l?y danh sách refund histories v?i phân trang
/// </summary>
public class GetRefundHistoriesRequest
{
    /// <summary>
    /// ID c?a user (optional - ?? l?y t?t c? n?u null)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Tr?ng thái refund (optional - ?? l?y t?t c? n?u null)
    /// </summary>
    public RefundStatus? Status { get; set; }

    /// <summary>
    /// T? ngày (optional)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// ??n ngày (optional)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// S? trang (b?t ??u t? 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page ph?i l?n h?n 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// S? l??ng item trên m?i trang
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize ph?i t? 1 ??n 100")]
    public int PageSize { get; set; } = 20;
}