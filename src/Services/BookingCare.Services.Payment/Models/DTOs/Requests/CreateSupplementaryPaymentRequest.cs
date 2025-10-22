using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO to create a supplementary payment for appointment price difference
/// Used when patient chooses new doctor with higher price (Option 3)
/// </summary>
public class CreateSupplementaryPaymentRequest
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
    /// Additional amount to pay (price difference)
    /// </summary>
    [Required]
    [JsonRequired]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal AdditionalAmount { get; set; }

    /// <summary>
    /// Payment method ID (VNPay, PayOS, etc.)
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Reason for supplementary payment
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; } = "Price difference payment for doctor change";

    /// <summary>
    /// Reschedule token for validation
    /// </summary>
    [Required]
    [JsonRequired]
    [MaxLength(100)]
    public string RescheduleToken { get; set; } = string.Empty;

    /// <summary>
    /// Flag to indicate if this is a staff-assigned doctor (Option 2) or patient-chosen (Option 3)
    /// True: Staff-assigned doctor, False: Patient chose doctor
    /// Used for proper callback handling
    /// </summary>
    public bool IsStaffAssigned { get; set; } = false;
}
