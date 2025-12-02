using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using Microsoft.AspNetCore.Http;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service for analyzing medical lab test results using OCR and AI
/// </summary>
public interface ILabResultAnalysisService
{
    /// <summary>
    /// Analyze lab result image and provide medical insights
    /// </summary>
    /// <param name="file">Lab result image file</param>
    /// <param name="location">User location for recommendations</param>
    /// <param name="userId">User ID (optional)</param>
    /// <param name="sessionId">Session ID for persistence (optional, will create new if not provided)</param>
    /// <returns>Analysis response with normal/abnormal indicators and recommendations</returns>
    Task<LabResultAnalysisResponse> AnalyzeLabResultAsync(
        IFormFile file,
        LocationContext? location,
        Guid? userId,
        Guid? sessionId);
}
