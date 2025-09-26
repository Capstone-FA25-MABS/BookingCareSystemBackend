using BookingCare.Shared.Saga.Core;
using BookingCare.Shared.Saga.Models;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.User.Protos;
using BookingCare.Services.Doctor.Protos;

namespace BookingCare.Shared.Saga.Steps;

/// <summary>
/// Step 1: Create Account in Auth Service
/// </summary>
public class CreateAccountGrpcStep : CompensatableSagaStepBase
{
    private readonly ILogger<CreateAccountGrpcStep> _logger;
    private readonly IConfiguration _configuration;

    public override string StepName => "CreateAccount";
    public override int Order => 1;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(2);

    public CreateAccountGrpcStep(ILogger<CreateAccountGrpcStep> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[CreateAccountGrpcStep] Creating account for saga {SagaId}", context.SagaId);

            // Get registration data from context
            var email = context.GetData<string>("Email");
            var password = context.GetData<string>("Password");
            var phoneNumber = context.GetData<string>("PhoneNumber");
            var role = context.GetData<string>("Role");

            // OTP verification data (for Patient registration)
            var purpose = context.GetData<string>("Purpose");
            var channel = context.GetData<string>("Channel");
            var proof = context.GetData<string>("Proof");
            var issuedAt = context.GetData<string>("IssuedAt");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            {
                return Failure("Missing required account data: Email, Password, or Role");
            }

            // Create gRPC client for Auth Service
            var authGrpcUrl = _configuration["Services:Auth:GrpcUrl"];
            if (string.IsNullOrEmpty(authGrpcUrl))
            {
                return Failure("Auth service gRPC URL not configured");
            }

            using var grpcChannel = GrpcChannel.ForAddress(authGrpcUrl);
            var client = new AuthService.AuthServiceClient(grpcChannel);

            // Call CreateAccount gRPC method
            var request = new CreateAccountRequest
            {
                Email = email,
                Password = password,
                PhoneNumber = phoneNumber ?? "",
                Role = role,
                Purpose = purpose ?? "",
                Channel = channel ?? "",
                Proof = proof ?? "",
                IssuedAt = issuedAt ?? ""
            };

            var response = await client.CreateAccountAsync(request, cancellationToken: cancellationToken);

            if (response.Success)
            {
                // Store AccountId for next steps
                context.SetData("AccountId", response.AccountId);

                _logger.LogInformation("[CreateAccountGrpcStep] Account created successfully: {AccountId}", response.AccountId);

                return Success(new Dictionary<string, object>
                {
                    { "AccountId", response.AccountId },
                    { "Message", response.Message }
                });
            }
            else
            {
                return Failure($"Failed to create account: {response.Message}");
            }
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "[CreateAccountGrpcStep] gRPC error creating account");
            return Failure($"gRPC error: {ex.Status.Detail}", ex, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateAccountGrpcStep] Unexpected error creating account");
            return Failure($"Unexpected error: {ex.Message}", ex);
        }
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[CreateAccountGrpcStep] Compensating - deleting account for saga {SagaId}", context.SagaId);

            var accountId = context.GetData<string>("AccountId");
            if (string.IsNullOrEmpty(accountId))
            {
                _logger.LogWarning("[CreateAccountGrpcStep] No AccountId found for compensation");
                return Success(); // Nothing to compensate
            }

            // Create gRPC client for Auth Service
            var authGrpcUrl = _configuration["Services:Auth:GrpcUrl"];
            if (string.IsNullOrEmpty(authGrpcUrl))
            {
                return Failure("Auth service gRPC URL not configured for compensation");
            }

            using var grpcChannel = GrpcChannel.ForAddress(authGrpcUrl);
            var client = new AuthService.AuthServiceClient(grpcChannel);

            // Call DeleteAccount gRPC method
            var request = new DeleteAccountRequest
            {
                AccountId = accountId
            };

            var response = await client.DeleteAccountAsync(request, cancellationToken: cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation("[CreateAccountGrpcStep] Account compensated successfully: {AccountId}", accountId);
                return Success();
            }
            else
            {
                return Failure($"Failed to compensate account: {response.Message}");
            }
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "[CreateAccountGrpcStep] gRPC error compensating account");
            return Failure($"gRPC compensation error: {ex.Status.Detail}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateAccountGrpcStep] Unexpected error compensating account");
            return Failure($"Unexpected compensation error: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Step 2: Create User Profile in User Service
/// </summary>
public class CreateUserProfileGrpcStep : CompensatableSagaStepBase
{
    private readonly ILogger<CreateUserProfileGrpcStep> _logger;
    private readonly IConfiguration _configuration;

    public override string StepName => "CreateUserProfile";
    public override int Order => 2;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(2);

    public CreateUserProfileGrpcStep(ILogger<CreateUserProfileGrpcStep> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[CreateUserProfileGrpcStep] Creating user profile for saga {SagaId}", context.SagaId);

            // Get data from context
            var accountId = context.GetData<string>("AccountId");
            var email = context.GetData<string>("Email");
            var fullName = context.GetData<string>("FullName");
            var phoneNumber = context.GetData<string>("PhoneNumber");
            var gender = context.GetData<string>("Gender");
            var birthday = context.GetData<string>("Birthday");
            var address = context.GetData<string>("Address");
            var avatarUrl = context.GetData<string>("AvatarUrl");

            // Parse full name into first and last name
            var nameParts = fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

            if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(email))
            {
                return Failure("Missing required user data: AccountId or Email");
            }

            // Create gRPC client for User Service
            var userGrpcUrl = _configuration["Services:User:GrpcUrl"];
            if (string.IsNullOrEmpty(userGrpcUrl))
            {
                return Failure("User service gRPC URL not configured");
            }

            using var grpcChannel = GrpcChannel.ForAddress(userGrpcUrl);
            var client = new UserService.UserServiceClient(grpcChannel);

            // Call CreateUser gRPC method
            var request = new CreateUserRequest
            {
                AccountId = accountId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Phone = phoneNumber ?? "",
                Gender = gender ?? "",
                DateOfBirth = birthday ?? "",
                Address = address ?? "",
                AvatarUrl = avatarUrl ?? ""
            };

            var response = await client.CreateUserAsync(request, cancellationToken: cancellationToken);

            // Store UserId for compensation
            context.SetData("UserId", response.Id);

            _logger.LogInformation("[CreateUserProfileGrpcStep] User profile created successfully: {UserId}", response.Id);

            return Success(new Dictionary<string, object>
            {
                { "UserId", response.Id },
                { "UserEmail", response.Email }
            });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "[CreateUserProfileGrpcStep] gRPC error creating user profile");
            return Failure($"gRPC error: {ex.Status.Detail}", ex, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateUserProfileGrpcStep] Unexpected error creating user profile");
            return Failure($"Unexpected error: {ex.Message}", ex);
        }
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[CreateUserProfileGrpcStep] Compensating - deleting user profile for saga {SagaId}", context.SagaId);

            var userId = context.GetData<string>("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("[CreateUserProfileGrpcStep] No UserId found for compensation");
                return Success(); // Nothing to compensate
            }

            // Create gRPC client for User Service
            var userGrpcUrl = _configuration["Services:User:GrpcUrl"];
            if (string.IsNullOrEmpty(userGrpcUrl))
            {
                return Failure("User service gRPC URL not configured for compensation");
            }

            using var grpcChannel = GrpcChannel.ForAddress(userGrpcUrl);
            var client = new UserService.UserServiceClient(grpcChannel);

            // Call DeleteUser gRPC method
            var request = new DeleteUserRequest
            {
                Id = userId
            };

            var response = await client.DeleteUserAsync(request, cancellationToken: cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation("[CreateUserProfileGrpcStep] User profile compensated successfully: {UserId}", userId);
                return Success();
            }
            else
            {
                return Failure($"Failed to compensate user profile: {response.Message}");
            }
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "[CreateUserProfileGrpcStep] gRPC error compensating user profile");
            return Failure($"gRPC compensation error: {ex.Status.Detail}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateUserProfileGrpcStep] Unexpected error compensating user profile");
            return Failure($"Unexpected compensation error: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Step 2: Create Doctor Profile in Doctor Service
/// </summary>
public class CreateDoctorProfileGrpcStep : CompensatableSagaStepBase
{
    private readonly ILogger<CreateDoctorProfileGrpcStep> _logger;
    private readonly IConfiguration _configuration;

    public override string StepName => "CreateDoctorProfile";
    public override int Order => 2;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(2);

    public CreateDoctorProfileGrpcStep(ILogger<CreateDoctorProfileGrpcStep> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[CreateDoctorProfileGrpcStep] Creating doctor profile for saga {SagaId}", context.SagaId);

            // Get data from context
            var accountId = context.GetData<string>("AccountId");
            var email = context.GetData<string>("Email");
            var fullName = context.GetData<string>("FullName");
            var gender = context.GetData<string>("Gender");
            var address = context.GetData<string>("Address");
            var bio = context.GetData<string>("Bio");
            var yearsOfExperience = context.GetData<int>("YearsOfExperience");
            var avatarUrl = context.GetData<string>("AvatarUrl");
            var specialtyId = context.GetData<string>("SpecialtyId");
            var positionId = context.GetData<string>("PositionId");
            var hospitalId = context.GetData<string>("ClinicId"); // ClinicId from context maps to HospitalId in proto

            // Parse full name into first and last name
            var nameParts = fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

            if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(email))
            {
                return Failure("Missing required doctor data: AccountId or Email");
            }

            // Create gRPC client for Doctor Service
            var doctorGrpcUrl = _configuration["Services:Doctor:GrpcUrl"];
            if (string.IsNullOrEmpty(doctorGrpcUrl))
            {
                return Failure("Doctor service gRPC URL not configured");
            }

            using var grpcChannel = GrpcChannel.ForAddress(doctorGrpcUrl);
            var client = new DoctorService.DoctorServiceClient(grpcChannel);

            // Call CreateDoctor gRPC method
            var request = new CreateDoctorRequest
            {
                AccountId = accountId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Gender = gender ?? "",
                Address = address ?? "",
                Bio = bio ?? "",
                YearsOfExperience = yearsOfExperience,
                AvatarUrl = avatarUrl ?? "",
                SpecialtyId = specialtyId ?? "",
                PositionId = positionId ?? "",
                HospitalId = hospitalId ?? ""
            };

            var response = await client.CreateDoctorAsync(request, cancellationToken: cancellationToken);

            // Store DoctorId for compensation
            context.SetData("DoctorId", response.Id);

            _logger.LogInformation("[CreateDoctorProfileGrpcStep] Doctor profile created successfully: {DoctorId}", response.Id);

            return Success(new Dictionary<string, object>
            {
                { "DoctorId", response.Id },
                { "DoctorEmail", response.Email }
            });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "[CreateDoctorProfileGrpcStep] gRPC error creating doctor profile");
            return Failure($"gRPC error: {ex.Status.Detail}", ex, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateDoctorProfileGrpcStep] Unexpected error creating doctor profile");
            return Failure($"Unexpected error: {ex.Message}", ex);
        }
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[CreateDoctorProfileGrpcStep] Compensating - deleting doctor profile for saga {SagaId}", context.SagaId);

            var doctorId = context.GetData<string>("DoctorId");
            if (string.IsNullOrEmpty(doctorId))
            {
                _logger.LogWarning("[CreateDoctorProfileGrpcStep] No DoctorId found for compensation");
                return Success(); // Nothing to compensate
            }

            // Create gRPC client for Doctor Service
            var doctorGrpcUrl = _configuration["Services:Doctor:GrpcUrl"];
            if (string.IsNullOrEmpty(doctorGrpcUrl))
            {
                return Failure("Doctor service gRPC URL not configured for compensation");
            }

            using var grpcChannel = GrpcChannel.ForAddress(doctorGrpcUrl);
            var client = new DoctorService.DoctorServiceClient(grpcChannel);

            // Call DeleteDoctor gRPC method
            var request = new DeleteDoctorRequest
            {
                Id = doctorId
            };

            var response = await client.DeleteDoctorAsync(request, cancellationToken: cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation("[CreateDoctorProfileGrpcStep] Doctor profile compensated successfully: {DoctorId}", doctorId);
                return Success();
            }
            else
            {
                return Failure($"Failed to compensate doctor profile: {response.Message}");
            }
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "[CreateDoctorProfileGrpcStep] gRPC error compensating doctor profile");
            return Failure($"gRPC compensation error: {ex.Status.Detail}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateDoctorProfileGrpcStep] Unexpected error compensating doctor profile");
            return Failure($"Unexpected compensation error: {ex.Message}", ex);
        }
    }
}