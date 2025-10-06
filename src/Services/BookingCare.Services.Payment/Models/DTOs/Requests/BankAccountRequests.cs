using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request để tạo bank account mới
/// </summary>
public class CreateBankAccountRequest
{
    /// <summary>
    /// ID của user sở hữu tài khoản ngân hàng
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid UserId { get; set; }

    /// <summary>
    /// Mã ngân hàng (ví dụ: VCB, TCB, VTB)
    /// </summary>
    [Required]
    [MaxLength(10, ErrorMessage = "Mã ngân hàng không được vượt quá 10 ký tự")]
    public string BankCode { get; set; } = string.Empty;

    /// <summary>
    /// Tên ngân hàng đầy đủ
    /// </summary>
    [Required]
    [MaxLength(255, ErrorMessage = "Tên ngân hàng không được vượt quá 255 ký tự")]
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// Số tài khoản ngân hàng
    /// </summary>
    [Required]
    [MaxLength(50, ErrorMessage = "Số tài khoản không được vượt quá 50 ký tự")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Số tài khoản chỉ được chứa các chữ số")]
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Tên chủ tài khoản
    /// </summary>
    [Required]
    [MaxLength(255, ErrorMessage = "Tên chủ tài khoản không được vượt quá 255 ký tự")]
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Có phải là tài khoản mặc định hay không
    /// </summary>
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// Request để cập nhật bank account
/// </summary>
public class UpdateBankAccountRequest
{
    /// <summary>
    /// ID của bank account cần cập nhật
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid Id { get; set; }

    /// <summary>
    /// Mã ngân hàng (ví dụ: VCB, TCB, VTB)
    /// </summary>
    [MaxLength(10, ErrorMessage = "Mã ngân hàng không được vượt quá 10 ký tự")]
    public string? BankCode { get; set; }

    /// <summary>
    /// Tên ngân hàng đầy đủ
    /// </summary>
    [MaxLength(255, ErrorMessage = "Tên ngân hàng không được vượt quá 255 ký tự")]
    public string? BankName { get; set; }

    /// <summary>
    /// Số tài khoản ngân hàng
    /// </summary>
    [MaxLength(50, ErrorMessage = "Số tài khoản không được vượt quá 50 ký tự")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Số tài khoản chỉ được chứa các chữ số")]
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Tên chủ tài khoản
    /// </summary>
    [MaxLength(255, ErrorMessage = "Tên chủ tài khoản không được vượt quá 255 ký tự")]
    public string? AccountName { get; set; }

    /// <summary>
    /// Có phải là tài khoản mặc định hay không
    /// </summary>
    public bool? IsDefault { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request để lấy bank accounts của user với phân trang
/// </summary>
public class GetBankAccountsRequest
{
    /// <summary>
    /// ID của user
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid UserId { get; set; }

    /// <summary>
    /// Số trang (bắt đầu từ 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page phải lớn hơn 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Số lượng item trên mỗi trang
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize phải từ 1 đến 100")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Chỉ lấy tài khoản đang hoạt động
    /// </summary>
    public bool? ActiveOnly { get; set; }
}