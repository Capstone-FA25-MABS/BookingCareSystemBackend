using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO để tạo payment mới
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// ID của appointment (tùy chọn)
    /// </summary>
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// ID của clinic (tùy chọn)
    /// </summary>
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// ID của patient (tùy chọn)
    /// </summary>
    public Guid? PatientId { get; set; }

    /// <summary>
    /// ID của subscription (tùy chọn - chỉ dùng cho clinic khi đăng ký gói)
    /// </summary>
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// Số tiền thanh toán
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Loại giao dịch
    /// </summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// ID phương thức thanh toán
    /// </summary>
    public Guid PaymentMethodId { get; set; }
}