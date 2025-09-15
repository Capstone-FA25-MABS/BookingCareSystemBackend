using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO ?? t?o payment m?i
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// ID c?a appointment (tùy ch?n)
    /// </summary>
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// ID c?a clinic (tùy ch?n)
    /// </summary>
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// ID c?a patient (tùy ch?n)
    /// </summary>
    public Guid? PatientId { get; set; }

    /// <summary>
    /// ID c?a subscription (tùy ch?n - ch? d?ng cho clinic khi ??ng ký gói)
    /// </summary>
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// S? ti?n thanh toán
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Lo?i giao d?ch
    /// </summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// ID ph??ng th?c thanh toán
    /// </summary>
    public Guid PaymentMethodId { get; set; }
}