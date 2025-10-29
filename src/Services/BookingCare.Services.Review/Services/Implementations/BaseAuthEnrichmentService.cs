using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Auth.Protos;
using BookingCare.Shared.Common.Services;
using Grpc.Core;

namespace BookingCare.Services.Review.Services.Implementations;

/// <summary>
/// Base class for services that enrich account information from Auth service
/// Eliminates duplicate code between AccountEnrichmentService and ReplyEnrichmentService
/// </summary>
public abstract class BaseAuthEnrichmentService : BaseService
{
    private readonly AuthService.AuthServiceClient _authClient;

    protected BaseAuthEnrichmentService(AuthService.AuthServiceClient authClient, ILogger logger)
        : base(logger)
    {
        _authClient = authClient;
    }

    /// <summary>
    /// Gets account information for multiple account IDs from Auth service
    /// </summary>
    /// <param name="accountIds">List of account IDs to fetch</param>
    /// <param name="operationName">Operation name for logging</param>
    /// <returns>Dictionary mapping account ID to AccountInfo</returns>
    protected async Task<Dictionary<string, AccountInfo>> GetAccountDetailsFromAuthServiceAsync(
  List<string> accountIds,
      string operationName)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            if (!accountIds.Any())
            {
                LogInfo("No account IDs provided for {OperationName}", null, operationName);
                return new Dictionary<string, AccountInfo>();
            }

            var uniqueAccountIds = GetUniqueAccountIds(accountIds);
            LogInfo("Fetching {OperationName} details for {Count} accounts from AuthService",
null, operationName, uniqueAccountIds.Count);

            var request = new GetAccountDetailsRequest();
            request.AccountIds.AddRange(uniqueAccountIds);

            try
            {
                var response = await _authClient.GetAccountDetailsAsync(request);
                return ProcessAuthServiceResponse(response, uniqueAccountIds, operationName);
            }
            catch (RpcException ex)
            {
                return HandleGrpcException(ex, uniqueAccountIds, operationName);
            }

        }, operationName);
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
 List<string> uniqueAccountIds,
        string operationName)
    {
        if (!response.Success)
        {
            LogWarning("Auth service returned unsuccessful response for {OperationName}: {Message}",
          null, operationName, response.Message);
            return CreateEmptyAccountInfos(uniqueAccountIds);
        }

        var accountInfoDict = MapFoundAccounts(response.AccountDetails);
        AddMissingAccounts(accountInfoDict, uniqueAccountIds);

        LogInfo("Successfully enriched {Found}/{Total} {OperationName} details from AuthService",
                    null, accountInfoDict.Values.Count(a => a.Found), uniqueAccountIds.Count, operationName);

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
        foreach (var accountId in uniqueAccountIds.Where(accountId => !accountInfoDict.ContainsKey(accountId)))
        {
            accountInfoDict[accountId] = new AccountInfo
            {
                AccountId = accountId,
                Found = false
            };
        }
    }

    /// <summary>
    /// Handles gRPC exceptions and returns appropriate fallback response
    /// </summary>
    private Dictionary<string, AccountInfo> HandleGrpcException(
 RpcException ex,
        List<string> uniqueAccountIds,
        string operationName)
    {
        var errorMessage = ex.StatusCode switch
        {
            StatusCode.DeadlineExceeded => "Auth service call timed out for {OperationName}: {Status}",
            StatusCode.Unavailable => "Auth service is unavailable for {OperationName}: {Status}",
            _ => "gRPC call to Auth service failed for {OperationName}: {Status} - {Detail}"
        };

        if (ex.StatusCode == StatusCode.DeadlineExceeded || ex.StatusCode == StatusCode.Unavailable)
        {
            LogWarning(errorMessage, null, operationName, ex.StatusCode.ToString());
        }
        else
        {
            LogWarning(errorMessage, null, operationName, ex.StatusCode.ToString(), ex.Status.Detail);
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