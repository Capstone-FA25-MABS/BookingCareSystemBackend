using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Services.Interfaces;
using BookingCare.Services.Auth.Protos;

namespace BookingCare.Services.Review.Services.Implementations;

/// <summary>
/// Service for enriching reply author information from Auth service
/// </summary>
public class ReplyEnrichmentService : BaseAuthEnrichmentService, IReplyEnrichmentService
{
    public ReplyEnrichmentService(AuthService.AuthServiceClient authClient, ILogger<ReplyEnrichmentService> logger)
        : base(authClient, logger)
    {
    }

    /// <summary>
    /// Gets account information for multiple account IDs (for reply authors)
    /// </summary>
    /// <param name="accountIds">List of account IDs to fetch</param>
    /// <returns>Dictionary mapping account ID to AccountInfo</returns>
    public async Task<Dictionary<string, AccountInfo>> GetReplyAuthorsInfoAsync(List<string> accountIds)
    {
        return await GetAccountDetailsFromAuthServiceAsync(accountIds, "reply author enrichment");
    }
}