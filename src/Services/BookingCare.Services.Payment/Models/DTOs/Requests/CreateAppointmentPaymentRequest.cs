using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO ?? t?o payment cho appointment (patient ??t l?ch)
/// </summary>
public class CreateAppointmentPaymentRequest
{
    /// <summary>
    /// ID c?a appointment (b?t bu?c)
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// ID c?a patient (b?t bu?c)
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// S? ti?n thanh toán
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ID ph??ng th?c thanh toán
    /// </summary>
    public Guid PaymentMethodId { get; set; }
}