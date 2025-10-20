using AutoMapper;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Service để enrichment participant data từ Auth Service với Redis caching
/// </summary>
public class ParticipantEnrichmentService : BaseService, IParticipantEnrichmentService
{
    private readonly AuthService.AuthServiceClient _authClient;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;

    // Cache configuration
    private const string ACCOUNT_CACHE_PREFIX = "account_details";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(30);

    public ParticipantEnrichmentService(
        AuthService.AuthServiceClient authClient,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<ParticipantEnrichmentService> logger) : base(logger)
    {
        _authClient = authClient;
        _cacheService = cacheService;
        _mapper = mapper;
    }

    /// <summary>
    /// 🎯 OPTIMIZED: Enrichment participant details chỉ cho OTHER participants (exclude current user)
    /// </summary>
    public async Task EnrichOtherParticipantDetailsAsync(IEnumerable<ConversationResponse> conversations, string currentUserId)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            var conversationList = conversations.ToList();
            LogInfo("Bắt đầu enrichment OTHER participant details cho {Count} conversations (exclude user: {CurrentUserId})",
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
                LogDebug("Không có other participants để enrichment (all conversations chỉ có current user)", null);
                return;
            }

            LogDebug("Found {Count} unique OTHER participants to enrich: {ParticipantIds}",
                null, otherParticipantIds.Count, string.Join(", ", otherParticipantIds));

            // Lấy account details cho OTHER participants only
            var accountDetails = await GetAccountDetailsAsync(otherParticipantIds);

            // Apply enrichment cho từng conversation - chỉ OTHER participants
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
                        LogWarning("Không tìm thấy account details cho OTHER participant: {ParticipantId} (normalized: {NormalizedId})",
                            null, participantId, normalizedId);
                    }
                }

                conversation.ParticipantDetails = enrichedParticipants;

                LogDebug("Enriched {Count} OTHER participants for conversation {ConversationId} (original participants: {OriginalCount})",
                    null, enrichedParticipants.Count, conversation.Id, conversation.Participants.Count);
            }

            LogInfo("Hoàn thành enrichment OTHER participant details cho {Count} conversations với {TotalEnriched} OTHER participants",
                null, conversationList.Count, accountDetails.Count);
        }, "EnrichOtherParticipantDetails");
    }

    /// <summary>
    /// 📝 LEGACY: Enrichment participant details cho TẤT CẢ participants (backward compatibility)
    /// </summary>
    public async Task EnrichParticipantDetailsAsync(IEnumerable<ConversationResponse> conversations)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            var conversationList = conversations.ToList();
            LogInfo("Bắt đầu enrichment ALL participant details cho {Count} conversations", null, conversationList.Count);

            // Collect tất cả unique participant IDs - NORMALIZE TO LOWERCASE
            var allParticipantIds = conversationList
                .SelectMany(c => c.Participants)
                .Select(id => id.ToLowerInvariant()) // 🔧 FIX: Normalize to lowercase
                .Distinct()
                .ToList();

            if (!allParticipantIds.Any())
            {
                LogDebug("Không có participants để enrichment", null);
                return;
            }

            LogDebug("Normalized ALL participant IDs to lowercase: {ParticipantIds}", null, string.Join(", ", allParticipantIds));

            // Lấy account details cho tất cả participants
            var accountDetails = await GetAccountDetailsAsync(allParticipantIds);

            // Apply enrichment cho từng conversation
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
                        LogWarning("Không tìm thấy account details cho participant: {ParticipantId} (normalized: {NormalizedId})",
                            null, participantId, normalizedId);
                    }
                }

                conversation.ParticipantDetails = enrichedParticipants;

                LogDebug("Enriched {Count}/{Total} ALL participants for conversation {ConversationId}",
                    null, enrichedParticipants.Count, conversation.Participants.Count, conversation.Id);
            }

            LogInfo("Hoàn thành enrichment ALL participant details cho {Count} conversations với {TotalEnriched} participants",
                null, conversationList.Count, accountDetails.Count);
        }, "EnrichParticipantDetails");
    }

    /// <summary>
    /// Lấy thông tin account details cho multiple account IDs với batch processing và caching
    /// </summary>
    public async Task<Dictionary<string, ConversationParticipant>> GetAccountDetailsAsync(IEnumerable<string> accountIds)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // 🔧 FIX: Normalize all account IDs to lowercase for consistent caching
            var normalizedAccountIds = accountIds.Select(id => id.ToLowerInvariant()).Distinct().ToList();
            var result = new Dictionary<string, ConversationParticipant>();

            LogInfo("Lấy account details cho {Count} accounts (normalized)", null, normalizedAccountIds.Count);

            // Step 1: Kiểm tra cache trước
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

            // Step 2: Fetch uncached accounts từ Auth Service
            if (uncachedAccountIds.Any())
            {
                try
                {
                    // 🔧 FIX: Pass original IDs to gRPC (Auth Service có thể case-sensitive)
                    var originalAccountIds = accountIds.Where(id =>
                        uncachedAccountIds.Contains(id.ToLowerInvariant())).ToList();

                    var grpcRequest = new GetAccountDetailsRequest();
                    grpcRequest.AccountIds.AddRange(originalAccountIds);

                    LogDebug("Calling Auth Service gRPC cho {Count} accounts: {AccountIds}",
                        null, originalAccountIds.Count, string.Join(", ", originalAccountIds));

                    var grpcResponse = await _authClient.GetAccountDetailsAsync(grpcRequest);

                    if (grpcResponse.Success && grpcResponse.AccountDetails.Any())
                    {
                        // Process và cache results
                        foreach (var accountDetail in grpcResponse.AccountDetails)
                        {
                            if (accountDetail.Found)
                            {
                                var participantDetail = MapToConversationParticipant(accountDetail);

                                // 🔧 FIX: Store with normalized lowercase key
                                var normalizedKey = accountDetail.AccountId.ToLowerInvariant();
                                result[normalizedKey] = participantDetail;

                                // Cache result với normalized key
                                await SetToCacheAsync(normalizedKey, participantDetail);

                                LogDebug("Cached account details for {AccountId} (key: {NormalizedKey}): {FullName}",
                                    null, accountDetail.AccountId, normalizedKey, accountDetail.FullName);
                            }
                            else
                            {
                                LogWarning("Account không tìm thấy trong Auth Service: {AccountId}", null, accountDetail.AccountId);
                            }
                        }

                        LogInfo("Processed {FoundCount}/{RequestedCount} accounts từ Auth Service",
                            null, grpcResponse.AccountDetails.Count(a => a.Found), originalAccountIds.Count);
                    }
                    else
                    {
                        LogWarning("Auth Service trả về lỗi hoặc empty results: {Message}", null, grpcResponse.Message);
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "Lỗi khi gọi Auth Service gRPC cho accounts: {AccountIds}", null, string.Join(", ", uncachedAccountIds));
                    // Continue with cached results only
                }
            }

            LogInfo("Completed account details retrieval: {FoundCount}/{TotalCount} accounts",
                null, result.Count, normalizedAccountIds.Count);

            return result;
        }, "GetAccountDetailsAsync");
    }

    /// <summary>
    /// Lấy thông tin account detail cho một account ID duy nhất
    /// </summary>
    public async Task<ConversationParticipant?> GetAccountDetailAsync(string accountId)
    {
        // 🔧 FIX: Normalize input account ID
        var normalizedAccountId = accountId.ToLowerInvariant();
        var results = await GetAccountDetailsAsync(new[] { accountId });
        return results.TryGetValue(normalizedAccountId, out var detail) ? detail : null;
    }

    /// <summary>
    /// Clear cache cho một account ID
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
    /// Clear cache cho multiple account IDs
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
    /// Lấy account detail từ cache
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
            LogWarning("Lỗi khi lấy từ cache cho account {AccountId}: {Error}", null, normalizedAccountId, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Lưu account detail vào cache
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
            LogWarning("Lỗi khi lưu cache cho account {AccountId}: {Error}", null, normalizedAccountId, ex.Message);
        }
    }

    /// <summary>
    /// Generate cache key cho account - ALWAYS USE NORMALIZED KEY
    /// </summary>
    private static string GetCacheKey(string normalizedAccountId) => $"{ACCOUNT_CACHE_PREFIX}:{normalizedAccountId}";

    /// <summary>
    /// Map gRPC AccountDetail sang ConversationParticipant
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