using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO to update payment status
/// </summary>
public class UpdatePaymentStatusRequest
{
    /// <summary>
    /// ID of the payment
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid Id { get; set; }

    /// <summary>
    /// New status
    /// </summary>
    [Required]
    [JsonRequired]
    public PaymentStatus Status { get; set; }
}