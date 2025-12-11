using BookingCare.Services.AI.Models.DTOs;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service for managing nutrition conversation flow
/// </summary>
public interface INutritionConversationService
{
    /// <summary>
    /// Start a new nutrition conversation
    /// </summary>
    Task<NutritionConversationResponse> StartConversationAsync(
        Guid userId, 
        string sessionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process user's answer and return next question or completion
    /// </summary>
    Task<NutritionConversationResponse> ProcessAnswerAsync(
        Guid userId, 
        string sessionId, 
        string answer, 
        CancellationToken cancellationToken = default);
}
