using Grpc.Core;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Auth.Repositories;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.User.Protos;
using BookingCare.Services.Doctor.Protos;

namespace BookingCare.Services.Auth.Services;

/// <summary>
/// gRPC service implementation for Auth service
/// </summary>
public class AuthGrpcService : Protos.AuthService.AuthServiceBase
{
    private readonly IAuthRepository _authRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthGrpcService> _logger;
    private readonly UserService.UserServiceClient _userGrpcClient;
    private readonly DoctorService.DoctorServiceClient _doctorGrpcClient;

    public AuthGrpcService(
        IAuthRepository authRepository,
        IAuthService authService,
        ILogger<AuthGrpcService> logger,
        UserService.UserServiceClient userGrpcClient,
        DoctorService.DoctorServiceClient doctorGrpcClient)
    {
        _authRepository = authRepository;
        _authService = authService;
        _logger = logger;
        _userGrpcClient = userGrpcClient;
        _doctorGrpcClient = doctorGrpcClient;
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
            throw new RpcException(new Grpc.Core.Status(StatusCode.Internal, "Internal error occurred"));
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
            throw new RpcException(new Grpc.Core.Status(StatusCode.Internal, "Internal error occurred"));
        }
    }

    public override async Task<CreateAccountResponse> CreateAccount(
        CreateAccountRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC CreateAccount called for email: {Email}", request.Email);

            // Parse role
            if (!Enum.TryParse<Role>(request.Role, true, out var role))
            {
                return new CreateAccountResponse
                {
                    Success = false,
                    Message = $"Invalid role: {request.Role}"
                };
            }

            // Map gRPC request to RegisterRequest
            var registerRequest = new RegisterRequest
            {
                Email = request.Email,
                Password = request.Password,
                PhoneNumber = request.PhoneNumber,
                Purpose = ParsePurpose(request.Purpose) ?? OtpPurpose.REGISTER,
                Channel = request.Channel,
                Proof = request.Proof,
                IssuedAt = request.IssuedAt
            };

            // Call existing AuthService logic for regular users
            var result = await _authService.CreateAccountForSagaAsync(registerRequest, role);
            return new CreateAccountResponse
            {
                Success = result.Success,
                AccountId = result.AccountId,
                Message = result.Message,
                Email = request.Email
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CreateAccount for email: {Email}", request.Email);
            return new CreateAccountResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public override async Task<CreateExternalAccountResponse> CreateExternalAccount(
        CreateExternalAccountRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC CreateExternalAccount called for email: {Email} via {Provider}",
                request.Email, request.ExternalProvider);

            // Call AuthService method for external account creation
            var result = await _authService.CreateExternalAccountForSagaAsync(
                request.Email,
                request.FullName,
                request.AvatarUrl,
                request.ExternalProvider,
                request.ExternalUserId);

            return new CreateExternalAccountResponse
            {
                Success = result.Success,
                AccountId = result.AccountId,
                Message = result.Message,
                Email = request.Email
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CreateExternalAccount for email: {Email}", request.Email);
            return new CreateExternalAccountResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }


    public override async Task<DeleteAccountResponse> DeleteAccount(
        DeleteAccountRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC DeleteAccount called for AccountId: {AccountId}", request.AccountId);

            // Call existing AuthService logic
            var result = await _authService.DeleteAccountForSagaAsync(request.AccountId);

            return new DeleteAccountResponse
            {
                Success = result.Success,
                Message = result.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC DeleteAccount for AccountId: {AccountId}", request.AccountId);
            return new DeleteAccountResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public override async Task<GetAccountDetailsResponse> GetAccountDetails(
        GetAccountDetailsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetAccountDetails called for {Count} account IDs", request.AccountIds.Count);

            var response = new GetAccountDetailsResponse
            {
                Success = true,
                Message = "Account details retrieved successfully"
            };

            if (request.AccountIds == null || request.AccountIds.Count == 0)
            {
                response.Message = "No account IDs provided";
                return response;
            }

            var (validAccountIds, invalidIds) = ValidateAccountIds(request.AccountIds, response);

            if (invalidIds.Any())
            {
                _logger.LogWarning("Invalid account IDs provided: {InvalidIds}", string.Join(", ", invalidIds));
            }

            if (!validAccountIds.Any())
            {
                response.Message = "No valid account IDs provided";
                return response;
            }

            var accounts = await _authRepository.GetAccountsWithRolesAsync(validAccountIds);
            var accountDict = accounts.ToDictionary(a => a.AccountId, a => a);

            var (userDetails, doctorDetails) = await FetchUserAndDoctorDetailsAsync(accounts);

            BuildAccountDetailsResponse(validAccountIds, accountDict, userDetails, doctorDetails, response);

            _logger.LogInformation("Successfully processed {TotalRequested} account IDs: {ValidCount} valid, {FoundCount} found",
                request.AccountIds.Count, validAccountIds.Count, accounts.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetAccountDetails");
            return new GetAccountDetailsResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    #region Private Helper Methods

    private (List<Guid> validIds, List<string> invalidIds) ValidateAccountIds(
        IEnumerable<string> accountIds,
        GetAccountDetailsResponse response)
    {
        var validAccountIds = new List<Guid>();
        var invalidIds = new List<string>();

        foreach (var accountIdString in accountIds)
        {
            if (Guid.TryParse(accountIdString, out var accountId))
            {
                validAccountIds.Add(accountId);
            }
            else
            {
                invalidIds.Add(accountIdString);
                response.AccountDetails.Add(new AccountDetail
                {
                    AccountId = accountIdString,
                    Found = false,
                    Role = "Unknown"
                });
            }
        }

        return (validAccountIds, invalidIds);
    }

    private async Task<(Dictionary<string, (string Email, string FullName, string AvatarUrl)> userDetails,
                       Dictionary<string, (string Email, string FullName, string AvatarUrl)> doctorDetails)>
        FetchUserAndDoctorDetailsAsync(List<(Guid AccountId, List<string> Roles)> accounts)
    {
        var patientAccountIds = accounts
            .Where(a => a.Roles.Any(r => r == Role.PATIENT.ToString()))
            .Select(a => a.AccountId.ToString())
            .ToList();

        var doctorAccountIds = accounts
            .Where(a => a.Roles.Any(r => r == Role.DOCTOR.ToString()))
            .Select(a => a.AccountId.ToString())
            .ToList();

        var userDetails = await FetchUserDetailsAsync(patientAccountIds);
        var doctorDetails = await FetchDoctorDetailsAsync(doctorAccountIds);

        return (userDetails, doctorDetails);
    }

    private async Task<Dictionary<string, (string Email, string FullName, string AvatarUrl)>>
        FetchUserDetailsAsync(List<string> patientAccountIds)
    {
        var userDetails = new Dictionary<string, (string Email, string FullName, string AvatarUrl)>();

        if (!patientAccountIds.Any())
            return userDetails;

        try
        {
            var userRequest = new GetUsersByAccountIdsRequest();
            userRequest.AccountIds.AddRange(patientAccountIds);

            var userResponse = await _userGrpcClient.GetUsersByAccountIdsAsync(userRequest);
            foreach (var user in userResponse.Users)
            {
                userDetails[user.AccountId] = (user.Email, user.FullName, user.AvatarUrl ?? string.Empty);
            }

            _logger.LogInformation("Retrieved {Count} user details from UserService", userResponse.Users.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling UserService for account IDs: {AccountIds}",
                string.Join(", ", patientAccountIds));
        }

        return userDetails;
    }

    private async Task<Dictionary<string, (string Email, string FullName, string AvatarUrl)>>
        FetchDoctorDetailsAsync(List<string> doctorAccountIds)
    {
        var doctorDetails = new Dictionary<string, (string Email, string FullName, string AvatarUrl)>();

        if (!doctorAccountIds.Any())
            return doctorDetails;

        try
        {
            var doctorRequest = new GetDoctorsByAccountIdsRequest();
            doctorRequest.AccountIds.AddRange(doctorAccountIds);

            var doctorResponse = await _doctorGrpcClient.GetDoctorsByAccountIdsAsync(doctorRequest);
            foreach (var doctor in doctorResponse.Doctors)
            {
                doctorDetails[doctor.AccountId] = (doctor.Email, doctor.FullName, doctor.AvatarUrl ?? string.Empty);
            }

            _logger.LogInformation("Retrieved {Count} doctor details from DoctorService", doctorResponse.Doctors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling DoctorService for account IDs: {AccountIds}",
                string.Join(", ", doctorAccountIds));
        }

        return doctorDetails;
    }

    private void BuildAccountDetailsResponse(
        List<Guid> validAccountIds,
        Dictionary<Guid, (Guid AccountId, List<string> Roles)> accountDict,
        Dictionary<string, (string Email, string FullName, string AvatarUrl)> userDetails,
        Dictionary<string, (string Email, string FullName, string AvatarUrl)> doctorDetails,
        GetAccountDetailsResponse response)
    {
        foreach (var accountId in validAccountIds)
        {
            var accountIdString = accountId.ToString();
            var accountDetail = CreateAccountDetail(accountIdString, accountDict, userDetails, doctorDetails, accountId);
            response.AccountDetails.Add(accountDetail);
        }
    }

    private AccountDetail CreateAccountDetail(
        string accountIdString,
        Dictionary<Guid, (Guid AccountId, List<string> Roles)> accountDict,
        Dictionary<string, (string Email, string FullName, string AvatarUrl)> userDetails,
        Dictionary<string, (string Email, string FullName, string AvatarUrl)> doctorDetails,
        Guid accountId)
    {
        var accountDetail = new AccountDetail
        {
            AccountId = accountIdString,
            Found = false
        };

        if (accountDict.TryGetValue(accountId, out var account))
        {
            accountDetail.Found = true;
            accountDetail.Role = string.Join(",", account.Roles);

            SetAccountDetailProfile(accountDetail, accountIdString, userDetails, doctorDetails);
        }
        else
        {
            accountDetail.Role = "NotFound";
        }

        return accountDetail;
    }

    private void SetAccountDetailProfile(
        AccountDetail accountDetail,
        string accountIdString,
        Dictionary<string, (string Email, string FullName, string AvatarUrl)> userDetails,
        Dictionary<string, (string Email, string FullName, string AvatarUrl)> doctorDetails)
    {
        if (userDetails.TryGetValue(accountIdString, out var userDetail))
        {
            accountDetail.Email = userDetail.Email;
            accountDetail.FullName = userDetail.FullName;
            accountDetail.AvatarUrl = userDetail.AvatarUrl;
        }
        else if (doctorDetails.TryGetValue(accountIdString, out var doctorDetail))
        {
            accountDetail.Email = doctorDetail.Email;
            accountDetail.FullName = doctorDetail.FullName;
            accountDetail.AvatarUrl = doctorDetail.AvatarUrl;
        }
        else
        {
            accountDetail.Email = string.Empty;
            accountDetail.FullName = string.Empty;
            accountDetail.AvatarUrl = string.Empty;
            _logger.LogWarning("Account {AccountId} with role {Role} found but no profile details available",
                accountIdString, accountDetail.Role);
        }
    }

    private static OtpPurpose? ParsePurpose(string? purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose))
            return null;

        if (Enum.TryParse<OtpPurpose>(purpose, true, out var parsedPurpose))
            return parsedPurpose;

        return null;
    }

    #endregion
}
