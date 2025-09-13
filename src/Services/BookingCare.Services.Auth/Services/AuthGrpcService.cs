using BookingCare.Services.Auth.Protos;
using Grpc.Core;

namespace BookingCare.Services.Auth.Services;

public class AuthGrpcService : Protos.AuthService.AuthServiceBase
{
    private readonly ILogger<AuthGrpcService> _logger;

    public AuthGrpcService(ILogger<AuthGrpcService> logger)
    {
        _logger = logger;
    }

    public override async Task<CreateUserAccountResponse> CreateUserAccount(
        CreateUserAccountRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Creating user account for UserId: {UserId}, Email: {Email}", 
                request.UserId, request.Email);

            // Simulate account creation logic
            await Task.Delay(100); // Simulate DB operation

            var accountId = Guid.NewGuid().ToString();
            var verificationToken = Guid.NewGuid().ToString();

            _logger.LogInformation("User account created successfully. AccountId: {AccountId}, UserId: {UserId}", 
                accountId, request.UserId);

            return new CreateUserAccountResponse
            {
                Success = true,
                Message = "User account created successfully",
                AccountId = accountId,
                UserId = request.UserId,
                VerificationToken = verificationToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user account for UserId: {UserId}, Email: {Email}", 
                request.UserId, request.Email);
            return new CreateUserAccountResponse
            {
                Success = false,
                Message = $"Failed to create user account: {ex.Message}",
                UserId = request.UserId
            };
        }
    }

    public override async Task<DeleteUserAccountResponse> DeleteUserAccount(
        DeleteUserAccountRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Deleting user account. AccountId: {AccountId}, UserId: {UserId}", 
                request.AccountId, request.UserId);

            // Simulate account deletion logic
            await Task.Delay(50); // Simulate DB operation

            _logger.LogInformation("User account deleted successfully. AccountId: {AccountId}, UserId: {UserId}", 
                request.AccountId, request.UserId);

            return new DeleteUserAccountResponse
            {
                Success = true,
                Message = "User account deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user account. AccountId: {AccountId}, UserId: {UserId}", 
                request.AccountId, request.UserId);
            return new DeleteUserAccountResponse
            {
                Success = false,
                Message = $"Failed to delete user account: {ex.Message}"
            };
        }
    }

    public override async Task<ValidateCredentialsResponse> ValidateCredentials(
        ValidateCredentialsRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Validating credentials for Email: {Email}", request.Email);

            // Simulate credential validation logic
            await Task.Delay(50); // Simulate DB lookup

            // For demo purposes, assume validation passes
            var userId = Guid.NewGuid().ToString();

            return new ValidateCredentialsResponse
            {
                Success = true,
                Message = "Credentials validated successfully",
                UserId = userId,
                Role = "patient",
                IsVerified = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials for Email: {Email}", request.Email);
            return new ValidateCredentialsResponse
            {
                Success = false,
                Message = $"Failed to validate credentials: {ex.Message}"
            };
        }
    }

    public override async Task<GenerateVerificationTokenResponse> GenerateVerificationToken(
        GenerateVerificationTokenRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Generating verification token for UserId: {UserId}, Email: {Email}", 
                request.UserId, request.Email);

            // Simulate token generation logic
            await Task.Delay(30); // Simulate processing

            var verificationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var expiresAt = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ");

            return new GenerateVerificationTokenResponse
            {
                Success = true,
                Message = "Verification token generated successfully",
                VerificationToken = verificationToken,
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating verification token for UserId: {UserId}, Email: {Email}", 
                request.UserId, request.Email);
            return new GenerateVerificationTokenResponse
            {
                Success = false,
                Message = $"Failed to generate verification token: {ex.Message}"
            };
        }
    }

    public override async Task<VerifyAccountResponse> VerifyAccount(
        VerifyAccountRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Verifying account for UserId: {UserId}", request.UserId);

            // Simulate account verification logic
            await Task.Delay(50); // Simulate DB update

            _logger.LogInformation("Account verified successfully for UserId: {UserId}", request.UserId);

            return new VerifyAccountResponse
            {
                Success = true,
                Message = "Account verified successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying account for UserId: {UserId}", request.UserId);
            return new VerifyAccountResponse
            {
                Success = false,
                Message = $"Failed to verify account: {ex.Message}"
            };
        }
    }

    public override async Task<ChangePasswordResponse> ChangePassword(
        ChangePasswordRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Changing password for UserId: {UserId}", request.UserId);

            // Simulate password change logic
            await Task.Delay(100); // Simulate password hashing and DB update

            _logger.LogInformation("Password changed successfully for UserId: {UserId}", request.UserId);

            return new ChangePasswordResponse
            {
                Success = true,
                Message = "Password changed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for UserId: {UserId}", request.UserId);
            return new ChangePasswordResponse
            {
                Success = false,
                Message = $"Failed to change password: {ex.Message}"
            };
        }
    }
}
