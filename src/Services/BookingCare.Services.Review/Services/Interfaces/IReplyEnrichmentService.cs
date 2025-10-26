using BookingCare.Services.Review.Models.DTOs;

namespace BookingCare.Services.Review.Services.Interfaces;

/// <summary>
/// Interface for enriching reply author information from Auth service
/// </summary>
public interface IReplyEnrichmentService
{
    /// <summary>
    /// Gets account information for multiple account IDs (for reply authors)
    /// </summary>
    /// <param name="accountIds">List of account IDs to fetch</param>
    /// <returns>Dictionary mapping account ID to AccountInfo</returns>
    Task<Dictionary<string, AccountInfo>> GetReplyAuthorsInfoAsync(List<string> accountIds);
}