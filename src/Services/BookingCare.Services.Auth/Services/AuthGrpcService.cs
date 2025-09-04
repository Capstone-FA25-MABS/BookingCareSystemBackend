using Grpc.Core;
using Microsoft.Extensions.Logging;
using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Services;
using BookingCare.Services.Auth.Protos;

namespace BookingCare.Services.Auth.Services;

/// <summary>
/// gRPC service implementation for Auth service
/// </summary>
public class AuthGrpcService : Protos.AuthService.AuthServiceBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthGrpcService> _logger;

    public AuthGrpcService(IAuthService authService, ILogger<AuthGrpcService> logger)
    {
        _authService = authService;
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
                var account = await _authService.GetAccountByEmailAsync(request.Email);
                if (account != null)
                {
                    exists = true;
                    message = "Account with this email already exists";
                }
            }
            // Check by phone if provided and email not found
            else if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                var account = await _authService.GetAccountByPhoneNumberAsync(request.PhoneNumber);
                if (account != null)
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
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }
}
