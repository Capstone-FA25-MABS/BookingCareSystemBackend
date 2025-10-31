using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Service for enriching participant data from Auth Service with Redis caching
/// </summary>
public interface IParticipantEnrichmentService
{
    /// <summary>
    /// 🎯 OPTIMIZED: Enrich participant details only for OTHER participants (exclude current user)
    /// </summary>
    /// <param name="conversations">List of conversations to enrich</param>
    /// <param name="currentUserId">ID of the current user to exclude</param>
    Task EnrichOtherParticipantDetailsAsync(IEnumerable<ConversationResponse> conversations, string currentUserId);

    /// <summary>
    /// 📝 LEGACY: Enrich participant details for ALL participants (backward compatibility)
    /// </summary>
    /// <param name="conversations">List of conversations to enrich</param>
    Task EnrichParticipantDetailsAsync(IEnumerable<ConversationResponse> conversations);

    /// <summary>
    /// Get account details for a list of account IDs
    /// </summary>
    /// <param name="accountIds">List of account IDs</param>
    /// <returns>Dictionary mapping account ID to account details</returns>
    Task<Dictionary<string, ConversationParticipant>> GetAccountDetailsAsync(IEnumerable<string> accountIds);

    /// <summary>
    /// Get account detail for a single account ID (with caching)
    /// </summary>
    /// <param name="accountId">Account ID</param>
    /// <returns>Account detail or null if not found</returns>
    Task<ConversationParticipant?> GetAccountDetailAsync(string accountId);

    /// <summary>
    /// Clear cache for a single account ID
    /// </summary>
    /// <param name="accountId">Account ID to clear cache for</param>
    Task ClearAccountCacheAsync(string accountId);

    /// <summary>
    /// Clear cache for multiple account IDs
    /// </summary>
    /// <param name="accountIds">List of Account IDs to clear cache for</param>
    Task ClearAccountCacheAsync(IEnumerable<string> accountIds);
}