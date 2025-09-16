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

            // Parse string IDs to Guid
            var accountIds = new List<Guid>();
            var invalidIds = new List<string>();

            foreach (var accountIdString in request.AccountIds)
            {
                if (Guid.TryParse(accountIdString, out var accountId))
                {
                    accountIds.Add(accountId);
                }
                else
                {
                    invalidIds.Add(accountIdString);
                }
            }

            if (invalidIds.Any())
            {
                _logger.LogWarning("Invalid account IDs provided: {InvalidIds}",
                    string.Join(", ", invalidIds));
            }

            // Get accounts from repository
            var accounts = await _authRepository.GetAccountsByIdsAsync(accountIds);
            var accountDict = accounts.ToDictionary(a => a.Id, a => a);

            // Build response for all requested IDs
            foreach (var requestedId in accountIds)
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

            // Add entries for invalid IDs
            if (invalidIds.Any())
            {
                foreach (var invalidId in invalidIds)
                {
                    var accountStatus = new AccountStatus
                    {
                        AccountId = invalidId,
                        Found = false,
                        Status = -1
                    };
                    response.AccountStatuses.Add(accountStatus);
                }
            }

            _logger.LogInformation("Successfully retrieved status for {Count} accounts, {NotFound} not found",
                accounts.Count, accountIds.Count - accounts.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetAccountStatusByIds");
            throw new RpcException(new Status(StatusCode.Internal, "Internal error occurred"));
        }
    }
}
