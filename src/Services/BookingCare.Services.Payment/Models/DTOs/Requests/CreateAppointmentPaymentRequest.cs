using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO to create a payment for an appointment (created by patient)
/// </summary>
public class CreateAppointmentPaymentRequest
{
    /// <summary>
    /// Appointment ID (required)
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// Patient ID (required)
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid PatientId { get; set; }

    /// <summary>
    /// Hospital ID
    /// </summary>
    public Guid? HospitalId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    [Required]
    [JsonRequired]
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method ID
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Discount code to apply (optional)
    /// </summary>
    public string? DiscountCode { get; set; }
}
