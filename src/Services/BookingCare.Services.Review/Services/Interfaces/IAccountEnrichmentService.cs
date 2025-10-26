using BookingCare.Services.Review.Models.DTOs;

namespace BookingCare.Services.Review.Services.Interfaces;

/// <summary>
/// Interface for enriching account information from Auth service
/// </summary>
public interface IAccountEnrichmentService
{
    /// <summary>
    /// Gets account information for multiple account IDs
    /// </summary>
    /// <param name="accountIds">List of account IDs to fetch</param>
    /// <returns>Dictionary mapping account ID to AccountInfo</returns>
    Task<Dictionary<string, AccountInfo>> GetAccountDetailsAsync(List<string> accountIds);
}