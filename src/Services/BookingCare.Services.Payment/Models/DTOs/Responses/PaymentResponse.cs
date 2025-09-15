using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO cho payment
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// ID c?a payment
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID c?a appointment
    /// </summary>
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// ID c?a clinic
    /// </summary>
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// ID c?a patient
    /// </summary>
    public Guid? PatientId { get; set; }

    /// <summary>
    /// ID c?a subscription
    /// </summary>
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// S? ti?n thanh to?n
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Lo?i giao d?ch
    /// </summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// ID ph??ng th?c thanh to?n
    /// </summary>
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// T?n ph??ng th?c thanh to?n
    /// </summary>
    public string PaymentMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Tr?ng th?i thanh to?n
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Th?i gian t?o
    /// </summary>
    public DateTime CreatedAt { get; set; }
}