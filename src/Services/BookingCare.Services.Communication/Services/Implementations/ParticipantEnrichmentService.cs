using AutoMapper;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Service to enrich participant data from the Auth Service with Redis caching
/// </summary>
public class ParticipantEnrichmentService : BaseService, IParticipantEnrichmentService
{
    private readonly AuthService.AuthServiceClient _authClient;
    private readonly ICacheService _cacheService;


    // Cache configuration
    private const string ACCOUNT_CACHE_PREFIX = "account_details";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(30);

    public ParticipantEnrichmentService(
        AuthService.AuthServiceClient authClient,
        ICacheService cacheService,
        ILogger<ParticipantEnrichmentService> logger) : base(logger)
    {
        _authClient = authClient;
        _cacheService = cacheService;

    }

    /// <summary>
    /// 🎯 OPTIMIZED: Enrich participant details only for OTHER participants (exclude current user)
    /// </summary>
    public async Task EnrichOtherParticipantDetailsAsync(IEnumerable<ConversationResponse> conversations, string currentUserId)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            var conversationList = conversations.ToList();
            LogInfo("Starting enrichment of OTHER participant details for {Count} conversations (exclude user: {CurrentUserId})",
                null, conversationList.Count, currentUserId);

            // 🎯 OPTIMIZATION: Collect only OTHER participants (exclude currentUserId) - NORMALIZED TO LOWERCASE
            var otherParticipantIds = conversationList
                .SelectMany(c => c.Participants)
                .Where(id => !string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase)) // Exclude current user
                .Select(id => id.ToLowerInvariant()) // Normalize to lowercase
                .Distinct()
                .ToList();

            if (!otherParticipantIds.Any())
            {
                LogDebug("No OTHER participants to enrich (all conversations only contain the current user)", null);
                return;
            }

            LogDebug("Found {Count} unique OTHER participants to enrich: {ParticipantIds}",
                null, otherParticipantIds.Count, string.Join(", ", otherParticipantIds));

            // Get account details for OTHER participants only
            var accountDetails = await GetAccountDetailsAsync(otherParticipantIds);

            // Apply enrichment per conversation - only OTHER participants
            foreach (var conversation in conversationList)
            {
                var enrichedParticipants = new List<ConversationParticipant>();

                foreach (var participantId in conversation.Participants)
                {
                    // Skip current user
                    if (string.Equals(participantId, currentUserId, StringComparison.OrdinalIgnoreCase))
                    {
                        LogDebug("Skipping current user {UserId} in conversation {ConversationId}",
                            null, participantId, conversation.Id);
                        continue;
                    }

                    // 🔧 Use lowercase key for lookup
                    var normalizedId = participantId.ToLowerInvariant();
                    if (accountDetails.TryGetValue(normalizedId, out var details))
                    {
                        enrichedParticipants.Add(details);
                        LogDebug("Added OTHER participant {ParticipantId} ({FullName}) to conversation {ConversationId}",
                            null, participantId, details.FullName, conversation.Id);
                    }
                    else
                    {
                        LogWarning("Account details not found for OTHER participant: {ParticipantId} (normalized: {NormalizedId})",
                            null, participantId, normalizedId);
                    }
                }

                conversation.ParticipantDetails = enrichedParticipants;

                LogDebug("Enriched {Count} OTHER participants for conversation {ConversationId} (original participants: {OriginalCount})",
                    null, enrichedParticipants.Count, conversation.Id, conversation.Participants.Count);
            }

            LogInfo("Completed enrichment of OTHER participant details for {Count} conversations with {TotalEnriched} OTHER participants",
                null, conversationList.Count, accountDetails.Count);
        }, "EnrichOtherParticipantDetails");
    }

    /// <summary>
    /// 📝 LEGACY: Enrich participant details for ALL participants (backward compatibility)
    /// </summary>
    public async Task EnrichParticipantDetailsAsync(IEnumerable<ConversationResponse> conversations)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            var conversationList = conversations.ToList();
            LogInfo("Starting enrichment of ALL participant details for {Count} conversations", null, conversationList.Count);

            // Collect all unique participant IDs - NORMALIZE TO LOWERCASE
            var allParticipantIds = conversationList
                .SelectMany(c => c.Participants)
                .Select(id => id.ToLowerInvariant()) // 🔧 FIX: Normalize to lowercase
                .Distinct()
                .ToList();

            if (!allParticipantIds.Any())
            {
                LogDebug("No participants to enrich", null);
                return;
            }

            LogDebug("Normalized ALL participant IDs to lowercase: {ParticipantIds}", null, string.Join(", ", allParticipantIds));

            // Get account details for all participants
            var accountDetails = await GetAccountDetailsAsync(allParticipantIds);

            // Apply enrichment per conversation
            foreach (var conversation in conversationList)
            {
                var enrichedParticipants = new List<ConversationParticipant>();

                foreach (var participantId in conversation.Participants)
                {
                    // 🔧 FIX: Use lowercase key for lookup
                    var normalizedId = participantId.ToLowerInvariant();
                    if (accountDetails.TryGetValue(normalizedId, out var details))
                    {
                        enrichedParticipants.Add(details);
                    }
                    else
                    {
                        LogWarning("Account details not found for participant: {ParticipantId} (normalized: {NormalizedId})",
                            null, participantId, normalizedId);
                    }
                }

                conversation.ParticipantDetails = enrichedParticipants;

                LogDebug("Enriched {Count}/{Total} ALL participants for conversation {ConversationId}",
                    null, enrichedParticipants.Count, conversation.Participants.Count, conversation.Id);
            }

            LogInfo("Completed enrichment of ALL participant details for {Count} conversations with {TotalEnriched} participants",
                null, conversationList.Count, accountDetails.Count);
        }, "EnrichParticipantDetails");
    }

    /// <summary>
    /// Get account details for multiple account IDs with batch processing and caching
    /// </summary>
    public async Task<Dictionary<string, ConversationParticipant>> GetAccountDetailsAsync(IEnumerable<string> accountIds)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // 🔧 FIX: Normalize all account IDs to lowercase for consistent caching
            var normalizedAccountIds = accountIds.Select(id => id.ToLowerInvariant()).Distinct().ToList();
            var result = new Dictionary<string, ConversationParticipant>();

            LogInfo("Retrieving account details for {Count} accounts (normalized)", null, normalizedAccountIds.Count);

            // Step 1: Check cache first
            var cachedAccounts = new List<string>();
            var uncachedAccountIds = new List<string>();

            foreach (var accountId in normalizedAccountIds)
            {
                var cachedDetail = await GetFromCacheAsync(accountId);
                if (cachedDetail != null)
                {
                    result[accountId] = cachedDetail;
                    cachedAccounts.Add(accountId);
                }
                else
                {
                    uncachedAccountIds.Add(accountId);
                }
            }

            LogDebug("Cache results: {CachedCount} cached, {UncachedCount} uncached",
                null, cachedAccounts.Count, uncachedAccountIds.Count);

            // Step 2: Fetch uncached accounts from Auth Service
            if (uncachedAccountIds.Any())
            {
                try
                {
                    // 🔧 FIX: Pass original IDs to gRPC (Auth Service may be case-sensitive)
                    var originalAccountIds = accountIds.Where(id =>
                        uncachedAccountIds.Contains(id.ToLowerInvariant())).ToList();

                    var grpcRequest = new GetAccountDetailsRequest();
                    grpcRequest.AccountIds.AddRange(originalAccountIds);

                    LogDebug("Calling Auth Service gRPC for {Count} accounts: {AccountIds}",
                        null, originalAccountIds.Count, string.Join(", ", originalAccountIds));

                    var grpcResponse = await _authClient.GetAccountDetailsAsync(grpcRequest);

                    if (grpcResponse.Success && grpcResponse.AccountDetails.Any())
                    {
                        // Process and cache results
                        foreach (var accountDetail in grpcResponse.AccountDetails)
                        {
                            if (accountDetail.Found)
                            {
                                var participantDetail = MapToConversationParticipant(accountDetail);

                                // 🔧 FIX: Store with normalized lowercase key
                                var normalizedKey = accountDetail.AccountId.ToLowerInvariant();
                                result[normalizedKey] = participantDetail;

                                // Cache result with normalized key
                                await SetToCacheAsync(normalizedKey, participantDetail);

                                LogDebug("Cached account details for {AccountId} (key: {NormalizedKey}): {FullName}",
                                    null, accountDetail.AccountId, normalizedKey, accountDetail.FullName);
                            }
                            else
                            {
                                LogWarning("Account not found in Auth Service: {AccountId}", null, accountDetail.AccountId);
                            }
                        }

                        LogInfo("Processed {FoundCount}/{RequestedCount} accounts from Auth Service",
                            null, grpcResponse.AccountDetails.Count(a => a.Found), originalAccountIds.Count);
                    }
                    else
                    {
                        LogWarning("Auth Service returned an error or empty results: {Message}", null, grpcResponse.Message);
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error calling Auth Service gRPC for accounts: {AccountIds}", null, string.Join(", ", uncachedAccountIds));
                    // Continue with cached results only
                }
            }

            LogInfo("Completed account details retrieval: {FoundCount}/{TotalCount} accounts",
                null, result.Count, normalizedAccountIds.Count);

            return result;
        }, "GetAccountDetailsAsync");
    }

    /// <summary>
    /// Get account detail for a single account ID
    /// </summary>
    public async Task<ConversationParticipant?> GetAccountDetailAsync(string accountId)
    {
        // 🔧 FIX: Normalize input account ID
        var normalizedAccountId = accountId.ToLowerInvariant();
        var results = await GetAccountDetailsAsync(new[] { accountId });
        return results.TryGetValue(normalizedAccountId, out var detail) ? detail : null;
    }

    /// <summary>
    /// Clear cache for a single account ID
    /// </summary>
    public async Task ClearAccountCacheAsync(string accountId)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            // 🔧 FIX: Use normalized key for cache operations
            var normalizedKey = accountId.ToLowerInvariant();
            var cacheKey = GetCacheKey(normalizedKey);
            await _cacheService.RemoveAsync(cacheKey);
            LogDebug("Cleared cache for account: {AccountId} (key: {NormalizedKey})", null, accountId, normalizedKey);
        }, "ClearAccountCache");
    }

    /// <summary>
    /// Clear cache for multiple account IDs
    /// </summary>
    public async Task ClearAccountCacheAsync(IEnumerable<string> accountIds)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            var accountIdList = accountIds.ToList();
            LogInfo("Clearing cache for {Count} accounts", null, accountIdList.Count);

            var tasks = accountIdList.Select(ClearAccountCacheAsync);
            await Task.WhenAll(tasks);

            LogInfo("Completed cache clearing for {Count} accounts", null, accountIdList.Count);
        }, "ClearAccountCacheMultiple");
    }

    #region Private Helper Methods

    /// <summary>
    /// Get account detail from cache
    /// </summary>
    private async Task<ConversationParticipant?> GetFromCacheAsync(string normalizedAccountId)
    {
        try
        {
            var cacheKey = GetCacheKey(normalizedAccountId);
            var result = await _cacheService.GetAsync<ConversationParticipant>(cacheKey);

            if (result != null)
            {
                LogDebug("Cache HIT for account: {AccountId}", null, normalizedAccountId);
            }
            else
            {
                LogDebug("Cache MISS for account: {AccountId}", null, normalizedAccountId);
            }

            return result;
        }
        catch (Exception ex)
        {
            LogWarning("Error getting from cache for account {AccountId}: {Error}", null, normalizedAccountId, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Save account detail to cache
    /// </summary>
    private async Task SetToCacheAsync(string normalizedAccountId, ConversationParticipant participantDetail)
    {
        try
        {
            var cacheKey = GetCacheKey(normalizedAccountId);
            await _cacheService.SetAsync(cacheKey, participantDetail, CACHE_DURATION);
            LogDebug("Cache SET for account: {AccountId} with TTL: {Duration}minutes",
                null, normalizedAccountId, CACHE_DURATION.TotalMinutes);
        }
        catch (Exception ex)
        {
            LogWarning("Error saving cache for account {AccountId}: {Error}", null, normalizedAccountId, ex.Message);
        }
    }

    /// <summary>
    /// Generate cache key for account - ALWAYS USE NORMALIZED KEY
    /// </summary>
    private static string GetCacheKey(string normalizedAccountId) => $"{ACCOUNT_CACHE_PREFIX}:{normalizedAccountId}";

    /// <summary>
    /// Map gRPC AccountDetail to ConversationParticipant
    /// </summary>
    private static ConversationParticipant MapToConversationParticipant(AccountDetail accountDetail)
    {
        return new ConversationParticipant
        {
            Id = accountDetail.AccountId, // Keep original casing for display
            FullName = accountDetail.FullName,
            Email = accountDetail.Email,
            AvatarUrl = accountDetail.AvatarUrl,
            Role = accountDetail.Role,
            IsOnline = false // Will be populated later if needed by presence service
        };
    }

    #endregion
}