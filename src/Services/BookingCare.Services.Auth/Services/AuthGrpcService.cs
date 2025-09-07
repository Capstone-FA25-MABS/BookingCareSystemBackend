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
}
