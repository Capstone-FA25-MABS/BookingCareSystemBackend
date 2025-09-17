using Grpc.Core;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Auth.Repositories;

namespace BookingCare.Services.Auth.Services;

/// <summary>
/// gRPC service implementation for Auth service
/// </summary>
public class AuthGrpcService : Protos.AuthService.AuthServiceBase
{
    private readonly IAuthRepository _authRepository;
    private readonly ILogger<AuthGrpcService> _logger;

    public AuthGrpcService(IAuthRepository authRepository, ILogger<AuthGrpcService> logger)
    {
        _authRepository = authRepository;
        _logger = logger;
    }

    public override async Task<CheckAccountExistsResponse> CheckAccountExists(
        CheckAccountExistsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC CheckAccountExists called for email: {Email}, phone: {Phone}",
                request.Email, request.PhoneNumber);

            bool exists = false;
            string message = "Account not found";

            // Check by email if provided
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var account = await _authRepository.EmailExistsAsync(request.Email);
                if (account)
                {
                    exists = true;
                    message = "Account with this email already exists";
                }
            }
            // Check by phone if provided and email not found
            else if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                var account = await _authRepository.PhoneNumberExistsAsync(request.PhoneNumber);
                if (account)
                {
                    exists = true;
                    message = "Account with this phone number already exists";
                }
            }

            return new CheckAccountExistsResponse
            {
                Exists = exists,
                Message = message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CheckAccountExists");
            throw new RpcException(new Status(StatusCode.Internal, "Internal error occurred"));
        }
    }

    public override async Task<GetAccountStatusByIdsResponse> GetAccountStatusByIds(
        GetAccountStatusByIdsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetAccountStatusByIds called for {Count} account IDs",
                request.AccountIds.Count);

            var response = new GetAccountStatusByIdsResponse
            {
                Success = true,
                Message = "Account statuses retrieved successfully"
            };

            if (request.AccountIds == null || request.AccountIds.Count == 0)
            {
                response.Message = "No account IDs provided";
                return response;
            }

            // Parse string IDs to Guid and build response for all requested IDs
            var validAccountIds = new List<Guid>();

            foreach (var accountIdString in request.AccountIds)
            {
                if (Guid.TryParse(accountIdString, out var accountId))
                {
                    validAccountIds.Add(accountId);
                }
                else
                {
                    // Add invalid ID directly to response
                    _logger.LogWarning("Invalid account ID provided: {InvalidId}", accountIdString);
                    response.AccountStatuses.Add(new AccountStatus
                    {
                        AccountId = accountIdString,
                        Found = false,
                        Status = -1
                    });
                }
            }

            // Get accounts from repository (empty list if no valid IDs)
            var accounts = await _authRepository.GetAccountsByIdsAsync(validAccountIds);
            var accountDict = accounts.ToDictionary(a => a.Id, a => a);

            // Build response for valid IDs
            foreach (var requestedId in validAccountIds)
            {
                var accountStatus = new AccountStatus
                {
                    AccountId = requestedId.ToString()
                };

                if (accountDict.TryGetValue(requestedId, out var account))
                {
                    accountStatus.Found = true;
                    accountStatus.Status = (int)account.Status;
                }
                else
                {
                    accountStatus.Found = false;
                    accountStatus.Status = -1; // Not found
                }

                response.AccountStatuses.Add(accountStatus);
            }

            _logger.LogInformation("Successfully processed {TotalRequested} account IDs: {ValidCount} valid, {FoundCount} found",
                request.AccountIds.Count, validAccountIds.Count, accounts.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetAccountStatusByIds");
            throw new RpcException(new Status(StatusCode.Internal, "Internal error occurred"));
        }
    }
}
