using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Services.Interfaces;
using BookingCare.Services.Auth.Protos;
using BookingCare.Shared.Common.Services;
using Grpc.Core;

namespace BookingCare.Services.Review.Services.Implementations;

/// <summary>
/// Service for enriching reply author information from Auth service
/// </summary>
public class ReplyEnrichmentService : BaseService, IReplyEnrichmentService
{
    private readonly AuthService.AuthServiceClient _authClient;

    public ReplyEnrichmentService(AuthService.AuthServiceClient authClient, ILogger<ReplyEnrichmentService> logger)
        : base(logger)
    {
        _authClient = authClient;
    }

    /// <summary>
    /// Gets account information for multiple account IDs (for reply authors)
    /// </summary>
    /// <param name="accountIds">List of account IDs to fetch</param>
    /// <returns>Dictionary mapping account ID to AccountInfo</returns>
    public async Task<Dictionary<string, AccountInfo>> GetReplyAuthorsInfoAsync(List<string> accountIds)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            if (!accountIds.Any())
            {
                LogInfo("No account IDs provided for reply author enrichment", null);
                return new Dictionary<string, AccountInfo>();
            }

            var uniqueAccountIds = GetUniqueAccountIds(accountIds);
            LogInfo("Fetching reply author details for {Count} accounts from AuthService", null, uniqueAccountIds.Count);

            var request = new GetAccountDetailsRequest();
            request.AccountIds.AddRange(uniqueAccountIds);

            try
            {
                var response = await _authClient.GetAccountDetailsAsync(request);
                return ProcessAuthServiceResponse(response, uniqueAccountIds);
            }
            catch (RpcException ex)
            {
                return HandleGrpcException(ex, uniqueAccountIds);
            }

        }, "GetReplyAuthorsInfo");
    }

    /// <summary>
    /// Filters and removes duplicates from account IDs
    /// </summary>
    private static List<string> GetUniqueAccountIds(List<string> accountIds)
    {
        return accountIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
    }

    /// <summary>
    /// Processes the response from Auth service and creates account info dictionary
    /// </summary>
    private Dictionary<string, AccountInfo> ProcessAuthServiceResponse(
        GetAccountDetailsResponse response,
        List<string> uniqueAccountIds)
    {
        if (!response.Success)
        {
            LogWarning("Auth service returned unsuccessful response: {Message}", null, response.Message);
            return CreateEmptyAccountInfos(uniqueAccountIds);
        }

        var accountInfoDict = MapFoundAccounts(response.AccountDetails);
        AddMissingAccounts(accountInfoDict, uniqueAccountIds);

        LogInfo("Successfully enriched {Found}/{Total} reply author details from AuthService",
            null, accountInfoDict.Values.Count(a => a.Found), uniqueAccountIds.Count);

        return accountInfoDict;
    }

    /// <summary>
    /// Maps found account details to AccountInfo objects
    /// </summary>
    private static Dictionary<string, AccountInfo> MapFoundAccounts(
        IEnumerable<AccountDetail> accountDetails)
    {
        var accountInfoDict = new Dictionary<string, AccountInfo>();

        foreach (var accountDetail in accountDetails)
        {
            accountInfoDict[accountDetail.AccountId] = new AccountInfo
            {
                AccountId = accountDetail.AccountId,
                Email = accountDetail.Email ?? string.Empty,
                FullName = accountDetail.FullName ?? string.Empty,
                AvatarUrl = accountDetail.AvatarUrl ?? string.Empty,
                Role = accountDetail.Role ?? string.Empty,
                Found = accountDetail.Found
            };
        }

        return accountInfoDict;
    }

    /// <summary>
    /// Adds missing account IDs as not found entries
    /// </summary>
    private static void AddMissingAccounts(
        Dictionary<string, AccountInfo> accountInfoDict,
        List<string> uniqueAccountIds)
    {
        foreach (var accountId in uniqueAccountIds)
        {
            if (!accountInfoDict.ContainsKey(accountId))
            {
                accountInfoDict[accountId] = new AccountInfo
                {
                    AccountId = accountId,
                    Found = false
                };
            }
        }
    }

    /// <summary>
    /// Handles gRPC exceptions and returns appropriate fallback response
    /// </summary>
    private Dictionary<string, AccountInfo> HandleGrpcException(RpcException ex, List<string> uniqueAccountIds)
    {
        var errorMessage = ex.StatusCode switch
        {
            StatusCode.DeadlineExceeded => "Auth service call timed out: {Status}",
            StatusCode.Unavailable => "Auth service is unavailable: {Status}",
            _ => "gRPC call to Auth service failed: {Status} - {Detail}"
        };

        if (ex.StatusCode == StatusCode.DeadlineExceeded || ex.StatusCode == StatusCode.Unavailable)
        {
            LogWarning(errorMessage, null, ex.StatusCode.ToString());
        }
        else
        {
            LogWarning(errorMessage, null, ex.StatusCode.ToString(), ex.Status.Detail);
        }

        return CreateEmptyAccountInfos(uniqueAccountIds);
    }

    /// <summary>
    /// Creates empty account info objects for when Auth service is unavailable
    /// </summary>
    private static Dictionary<string, AccountInfo> CreateEmptyAccountInfos(List<string> accountIds)
    {
        return accountIds.ToDictionary(
            accountId => accountId,
            accountId => new AccountInfo
            {
                AccountId = accountId,
                Found = false
            });
    }
}