using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity cho bảng bank_accounts - Lưu trữ thông tin tài khoản ngân hàng của user
/// </summary>
[Table("bank_accounts")]
public class BankAccountEntity
{
    /// <summary>
    /// ID của bank account
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID của user sở hữu tài khoản ngân hàng
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Mã ngân hàng (ví dụ: VCB, TCB, VTB)
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("bank_code")]
    public string BankCode { get; set; } = string.Empty;

    /// <summary>
    /// Tên ngân hàng đầy đủ
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("bank_name")]
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// Số tài khoản ngân hàng
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("account_number")]
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Tên chủ tài khoản
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("account_name")]
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Có phải là tài khoản mặc định hay không
    /// </summary>
    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Trạng thái tài khoản có đang hoạt động hay không
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian cập nhật cuối cùng
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}