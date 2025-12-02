using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BookingCare.Services.AI.Models.DTOs.Requests;

/// <summary>
/// Request for lab result analysis
/// </summary>
public class LabResultAnalysisRequest
{
    /// <summary>
    /// Session ID for conversation persistence (optional, will create new if not provided)
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Lab result image file (JPG, PNG, PDF)
    /// </summary>
    [Required(ErrorMessage = "File xét nghiệm là bắt buộc")]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// User's location context for doctor/hospital recommendations
    /// </summary>
    public LocationContext? Location { get; set; }
}
