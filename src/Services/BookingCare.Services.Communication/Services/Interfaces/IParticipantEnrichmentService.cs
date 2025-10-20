using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Service ?? enrichment participant data t? Auth Service v?i Redis caching
/// </summary>
public interface IParticipantEnrichmentService
{
    /// <summary>
    /// 🎯 OPTIMIZED: Enrichment participant details chỉ cho OTHER participants (exclude current user)
    /// </summary>
    /// <param name="conversations">Danh sách conversations cần enrichment</param>
    /// <param name="currentUserId">ID của current user cần exclude</param>
    Task EnrichOtherParticipantDetailsAsync(IEnumerable<ConversationResponse> conversations, string currentUserId);

    /// <summary>
    /// 📝 LEGACY: Enrichment participant details cho TẤT CẢ participants (backward compatibility)
    /// </summary>
    /// <param name="conversations">Danh sách conversations cần enrichment</param>
    Task EnrichParticipantDetailsAsync(IEnumerable<ConversationResponse> conversations);

    /// <summary>
    /// L?y thông tin account details cho m?t list account IDs
    /// </summary>
    /// <param name="accountIds">Danh sách account IDs</param>
    /// <returns>Dictionary mapping account ID to account details</returns>
    Task<Dictionary<string, ConversationParticipant>> GetAccountDetailsAsync(IEnumerable<string> accountIds);

    /// <summary>
    /// L?y thông tin account detail cho m?t account ID duy nh?t (v?i caching)
    /// </summary>
    /// <param name="accountId">Account ID</param>
    /// <returns>Account detail ho?c null n?u không tìm th?y</returns>
    Task<ConversationParticipant?> GetAccountDetailAsync(string accountId);

    /// <summary>
    /// Clear cache cho m?t account ID c? th?
    /// </summary>
    /// <param name="accountId">Account ID c?n clear cache</param>
    Task ClearAccountCacheAsync(string accountId);

    /// <summary>
    /// Clear cache cho multiple account IDs
    /// </summary>
    /// <param name="accountIds">Danh sách Account IDs c?n clear cache</param>
    Task ClearAccountCacheAsync(IEnumerable<string> accountIds);
}