using BookingCare.Services.AI.Models.DTOs;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service interface for AI-related operations
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Generate medical summary from conversation transcript using Gemini AI
    /// </summary>
    /// <param name="request">Request containing transcript and appointment details</param>
    /// <returns>AI-generated medical summary</returns>
    Task<MedicalSummaryResponse> GenerateMedicalSummaryAsync(GenerateMedicalSummaryRequest request);
}
