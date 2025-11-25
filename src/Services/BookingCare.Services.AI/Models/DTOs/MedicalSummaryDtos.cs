using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.AI.Models.DTOs;

/// <summary>
/// Request to generate medical summary from conversation transcript
/// </summary>
public class GenerateMedicalSummaryRequest
{
    [Required(ErrorMessage = "Appointment ID is required")]
    public required Guid AppointmentId { get; set; }

    [Required(ErrorMessage = "Conversation transcript is required")]
    [MinLength(10, ErrorMessage = "Transcript must be at least 10 characters")]
    public required string Transcript { get; set; }

    /// <summary>
    /// Optional: Patient name for context
    /// </summary>
    public string? PatientName { get; set; }

    /// <summary>
    /// Optional: Doctor name for context
    /// </summary>
    public string? DoctorName { get; set; }

    /// <summary>
    /// Optional: Appointment date for context
    /// </summary>
    public DateTime? AppointmentDate { get; set; }
}

/// <summary>
/// Response containing AI-generated medical summary
/// </summary>
public class MedicalSummaryResponse
{
    /// <summary>
    /// AI-generated medical summary in structured format
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Appointment ID this summary belongs to
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// Timestamp when summary was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request to save edited medical summary to appointment
/// </summary>
public class SaveMedicalSummaryRequest
{
    [Required(ErrorMessage = "Appointment ID is required")]
    public required Guid AppointmentId { get; set; }

    [Required(ErrorMessage = "Summary is required")]
    [MaxLength(4000, ErrorMessage = "Summary cannot exceed 4000 characters")]
    public required string Summary { get; set; }
}
