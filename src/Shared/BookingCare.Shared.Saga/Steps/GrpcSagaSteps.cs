using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Core;
using BookingCare.Shared.Saga.Models;
using BookingCare.Shared.Saga.Protos.Auth;
using BookingCare.Shared.Saga.Protos.User;
using BookingCare.Shared.Saga.Protos.Notification;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookingCare.Shared.Saga.Steps.Grpc;

/// <summary>
/// Saga step that creates user account via gRPC call to Auth Service
/// </summary>
public class CreateUserAccountGrpcStep : CompensatableSagaStepBase
{
    private readonly ILogger<CreateUserAccountGrpcStep> _logger;
    private readonly IConfiguration _configuration;

    public CreateUserAccountGrpcStep(ILogger<CreateUserAccountGrpcStep> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override string StepName => "CreateUserAccount";
    public override int Order => 1;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing CreateUserAccount step for SagaId: {SagaId}", context.SagaId);

            // Get user data from context
            var email = context.GetData<string>("Email") ?? throw new InvalidOperationException("Email is required");
            var password = context.GetData<string>("Password") ?? throw new InvalidOperationException("Password is required");
            var userId = context.GetData<string>("UserId") ?? Guid.NewGuid().ToString();
            var role = context.GetData<string>("Role") ?? "patient";

            // Create gRPC channel
            var authServiceUrl = _configuration["Services:Auth:GrpcUrl"] ?? "https://localhost:6013";
            using var channel = GrpcChannel.ForAddress(authServiceUrl);
            var client = new AuthService.AuthServiceClient(channel);

            // Make gRPC call
            var request = new CreateUserAccountRequest
            {
                UserId = userId,
                Email = email,
                Password = password, // In real implementation, this should be hashed
                Role = role,
                IsVerified = false,
                Provider = "local"
            };

            var response = await client.CreateUserAccountAsync(request, cancellationToken: cancellationToken);

            if (!response.Success)
            {
                _logger.LogError("Failed to create user account: {Message}", response.Message);
                return Failure(response.Message, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(5));
            }

            // Store account data in context for next steps and compensation
            context.SetData("AccountId", response.AccountId);
            context.SetData("UserId", response.UserId);
            context.SetData("VerificationToken", response.VerificationToken);

            _logger.LogInformation("User account created successfully. AccountId: {AccountId}, UserId: {UserId}", 
                response.AccountId, response.UserId);

            return Success(new Dictionary<string, object>
            {
                ["AccountId"] = response.AccountId,
                ["UserId"] = response.UserId,
                ["VerificationToken"] = response.VerificationToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateUserAccount step for SagaId: {SagaId}", context.SagaId);
            return Failure($"Failed to create user account: {ex.Message}", ex, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(10));
        }
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Compensating CreateUserAccount step for SagaId: {SagaId}", context.SagaId);

            var accountId = context.GetData<string>("AccountId");
            var userId = context.GetData<string>("UserId");

            if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No account data found for compensation. SagaId: {SagaId}", context.SagaId);
                return Success();
            }

            // Create gRPC channel
            var authServiceUrl = _configuration["Services:Auth:GrpcUrl"] ?? "https://localhost:6013";
            using var channel = GrpcChannel.ForAddress(authServiceUrl);
            var client = new AuthService.AuthServiceClient(channel);

            // Make compensation gRPC call
            var request = new DeleteUserAccountRequest
            {
                UserId = userId,
                AccountId = accountId
            };

            var response = await client.DeleteUserAccountAsync(request, cancellationToken: cancellationToken);

            if (!response.Success)
            {
                _logger.LogError("Failed to delete user account during compensation: {Message}", response.Message);
                return Failure(response.Message, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(5));
            }

            _logger.LogInformation("User account deleted successfully during compensation. AccountId: {AccountId}, UserId: {UserId}", 
                accountId, userId);

            return Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateUserAccount compensation for SagaId: {SagaId}", context.SagaId);
            return Failure($"Failed to compensate user account creation: {ex.Message}", ex, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(10));
        }
    }
}

/// <summary>
/// Saga step that creates user profile via gRPC call to User Service
/// </summary>
public class CreateUserProfileGrpcStep : CompensatableSagaStepBase
{
    private readonly ILogger<CreateUserProfileGrpcStep> _logger;
    private readonly IConfiguration _configuration;

    public CreateUserProfileGrpcStep(ILogger<CreateUserProfileGrpcStep> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override string StepName => "CreateUserProfile";
    public override int Order => 2;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing CreateUserProfile step for SagaId: {SagaId}", context.SagaId);

            // Get user data from context
            var userId = context.GetData<string>("UserId") ?? throw new InvalidOperationException("UserId is required");
            var email = context.GetData<string>("Email") ?? throw new InvalidOperationException("Email is required");
            var firstName = context.GetData<string>("FirstName") ?? "";
            var lastName = context.GetData<string>("LastName") ?? "";
            var phoneNumber = context.GetData<string>("PhoneNumber") ?? "";
            var dateOfBirth = context.GetData<string>("DateOfBirth") ?? "";
            var gender = context.GetData<string>("Gender") ?? "";
            var address = context.GetData<string>("Address") ?? "";

            // Create gRPC channel
            var userServiceUrl = _configuration["Services:User:GrpcUrl"] ?? "https://localhost:6012";
            using var channel = GrpcChannel.ForAddress(userServiceUrl);
            var client = new UserService.UserServiceClient(channel);

            // Make gRPC call
            var request = new CreateUserProfileRequest
            {
                UserId = userId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                DateOfBirth = dateOfBirth,
                Gender = gender,
                Address = address
            };

            var response = await client.CreateUserProfileAsync(request, cancellationToken: cancellationToken);

            if (!response.Success)
            {
                _logger.LogError("Failed to create user profile: {Message}", response.Message);
                return Failure(response.Message, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(5));
            }

            // Store profile data in context for compensation
            context.SetData("ProfileId", response.ProfileId);

            _logger.LogInformation("User profile created successfully. ProfileId: {ProfileId}, UserId: {UserId}", 
                response.ProfileId, response.UserId);

            return Success(new Dictionary<string, object>
            {
                ["ProfileId"] = response.ProfileId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateUserProfile step for SagaId: {SagaId}", context.SagaId);
            return Failure($"Failed to create user profile: {ex.Message}", ex, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(10));
        }
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Compensating CreateUserProfile step for SagaId: {SagaId}", context.SagaId);

            var profileId = context.GetData<string>("ProfileId");
            var userId = context.GetData<string>("UserId");

            if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No profile data found for compensation. SagaId: {SagaId}", context.SagaId);
                return Success();
            }

            // Create gRPC channel
            var userServiceUrl = _configuration["Services:User:GrpcUrl"] ?? "https://localhost:6012";
            using var channel = GrpcChannel.ForAddress(userServiceUrl);
            var client = new UserService.UserServiceClient(channel);

            // Make compensation gRPC call
            var request = new DeleteUserProfileRequest
            {
                UserId = userId,
                ProfileId = profileId
            };

            var response = await client.DeleteUserProfileAsync(request, cancellationToken: cancellationToken);

            if (!response.Success)
            {
                _logger.LogError("Failed to delete user profile during compensation: {Message}", response.Message);
                return Failure(response.Message, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(5));
            }

            _logger.LogInformation("User profile deleted successfully during compensation. ProfileId: {ProfileId}, UserId: {UserId}", 
                profileId, userId);

            return Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateUserProfile compensation for SagaId: {SagaId}", context.SagaId);
            return Failure($"Failed to compensate user profile creation: {ex.Message}", ex, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(10));
        }
    }
}

/// <summary>
/// Saga step that sends verification email via gRPC call to Notification Service
/// </summary>
public class SendVerificationEmailGrpcStep : CompensatableSagaStepBase
{
    private readonly ILogger<SendVerificationEmailGrpcStep> _logger;
    private readonly IConfiguration _configuration;

    public SendVerificationEmailGrpcStep(ILogger<SendVerificationEmailGrpcStep> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override string StepName => "SendVerificationEmail";
    public override int Order => 3;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing SendVerificationEmail step for SagaId: {SagaId}", context.SagaId);

            var email = context.GetData<string>("Email") ?? throw new InvalidOperationException("Email is required");
            var userId = context.GetData<string>("UserId") ?? throw new InvalidOperationException("UserId is required");
            var verificationToken = context.GetData<string>("VerificationToken") ?? throw new InvalidOperationException("VerificationToken is required");
            var firstName = context.GetData<string>("FirstName") ?? "User";
            var lastName = context.GetData<string>("LastName") ?? "User";

            // Create gRPC channel to Notification Service
            var notificationServiceUrl = _configuration["Services:Notification:GrpcUrl"] ?? "https://localhost:6020";
            using var channel = GrpcChannel.ForAddress(notificationServiceUrl);
            var client = new NotificationService.NotificationServiceClient(channel);

            // Make gRPC call
            var request = new SendVerificationEmailRequest
            {
                UserId = userId,
                Email = email,
                VerificationToken = verificationToken,
                FirstName = firstName,
                LastName = lastName
            };

            var response = await client.SendVerificationEmailAsync(request, cancellationToken: cancellationToken);

            if (!response.Success)
                return Failure(response.Message, shouldRetry: true);

            context.SetData("EmailId", response.EmailId);
            context.SetData("EmailSent", true);
            context.SetData("EmailSentAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            return Success(new Dictionary<string, object>
            {
                ["EmailId"] = response.EmailId,
                ["EmailSent"] = true,
                ["EmailSentAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendVerificationEmail step for SagaId: {SagaId}", context.SagaId);
            return Failure($"Failed to send verification email: {ex.Message}", ex, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(30));
        }
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Compensating SendVerificationEmail step for SagaId: {SagaId}", context.SagaId);

            // For email sending, compensation might be sending a cancellation email
            // or marking the verification token as invalid
            var email = context.GetData<string>("Email");
            var userId = context.GetData<string>("UserId");

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("Compensating email send - would send cancellation email to {Email} for UserId: {UserId}", email, userId);
                await Task.Delay(50, cancellationToken); // Simulate compensation
            }

            return Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendVerificationEmail compensation for SagaId: {SagaId}", context.SagaId);
            return Failure($"Failed to compensate verification email: {ex.Message}", ex, shouldRetry: false);
        }
    }
}
