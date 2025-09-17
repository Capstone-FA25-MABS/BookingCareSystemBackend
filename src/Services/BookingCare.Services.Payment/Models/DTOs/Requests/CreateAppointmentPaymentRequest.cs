using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO để tạo payment cho appointment (patient tạo lịch)
/// </summary>
public class CreateAppointmentPaymentRequest
{
    /// <summary>
    /// ID của appointment (bắt buộc)
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// ID của patient (bắt buộc)
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Số tiền thanh toán
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ID phương thức thanh toán
    /// </summary>
    public Guid PaymentMethodId { get; set; }
}