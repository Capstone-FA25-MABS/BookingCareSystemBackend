using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO cho payment
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// ID của payment
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID của appointment
    /// </summary>
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// ID của clinic
    /// </summary>
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// ID của patient
    /// </summary>
    public Guid? PatientId { get; set; }

    /// <summary>
    /// ID của subscription
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

    /// <summary>
    /// Tên phương thức thanh toán
    /// </summary>
    public string PaymentMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái thanh toán
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }
}