using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service for analyzing dermatology images using Legit.Health API
/// </summary>
public interface IDermatologyAnalysisService
{
    /// <summary>
    /// Analyze skin image and provide dermatological diagnosis with malignancy assessment
    /// </summary>
    /// <param name="file">Skin image file</param>
    /// <param name="location">User location for recommendations</param>
    /// <param name="userId">User ID (optional)</param>
    /// <param name="sessionId">Session ID for persistence (optional, will create new if not provided)</param>
    /// <returns>Analysis response with diagnosis, malignancy risk, biopsy recommendation, and doctor/hospital suggestions</returns>
    Task<DermatologyAnalysisResponse> AnalyzeSkinImageAsync(
        IFormFile file,
        LocationContext? location,
        Guid? userId,
        Guid? sessionId);
}
