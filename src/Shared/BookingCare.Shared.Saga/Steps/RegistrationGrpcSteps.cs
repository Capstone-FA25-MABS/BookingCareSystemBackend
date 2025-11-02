using BookingCare.Shared.Saga.Models;
using BookingCare.Shared.Saga.Constants;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.User.Protos;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;

namespace BookingCare.Shared.Saga.Steps;

/// <summary>
/// Step 1: Create Account in Auth Service
/// </summary>
public class CreateAccountGrpcStep : BaseGrpcStep
{
    private readonly IConfiguration _configuration;

    public override string StepName => "CreateAccount";
    public override int Order => 1;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(2);

    public CreateAccountGrpcStep(ILogger<CreateAccountGrpcStep> logger, IConfiguration configuration)
        : base(logger)
    {
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
                context.SetData(SagaConstants.ACCOUNT_ID_KEY, response.AccountId);

                _logger.LogInformation("[CreateAccountGrpcStep] Account created successfully: {AccountId}", response.AccountId);

                return Success(new Dictionary<string, object>
                {
                    { SagaConstants.ACCOUNT_ID_KEY, response.AccountId },
                    { "Message", response.Message }
                });
            }
            else
            {
                return Failure($"Failed to create account: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            return HandleGrpcException(ex, "creating account", StepName);
        }
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        return await CompensateAccountDeletionAsync(context, _configuration, StepName, "deleting account", SagaConstants.ACCOUNT_ID_KEY, cancellationToken);
    }
}

/// <summary>
/// Step 2: Create User Profile in User Service
/// </summary>
public class CreateUserProfileGrpcStep : BaseGrpcStep
{
    private readonly IConfiguration _configuration;

    public override string StepName => "CreateUserProfile";
    public override int Order => 2;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(2);

    public CreateUserProfileGrpcStep(ILogger<CreateUserProfileGrpcStep> logger, IConfiguration configuration)
        : base(logger)
    {
        _configuration = configuration;
    }

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[CreateUserProfileGrpcStep] Creating user profile for saga {SagaId}", context.SagaId);

            // Get data from context
            var accountId = context.GetData<string>(SagaConstants.ACCOUNT_ID_KEY);
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

            return HandleSuccessfulResponse(response, "Id", "Email", StepName, context, "UserId");
        }
        catch (Exception ex)
        {
            return HandleGrpcException(ex, "creating user profile", StepName);
        }
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            LogCompensationStart(StepName, context.SagaId, "deleting user profile");

            var userId = context.GetData<string>("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return LogCompensationWarning(StepName, "UserId");
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
        catch (Exception ex)
        {
            return HandleGrpcException(ex, "compensating user profile", StepName);
        }
    }
}

/// <summary>
/// Step 2: Create Doctor Profile in Doctor Service
/// </summary>
public class CreateDoctorProfileGrpcStep : BaseGrpcStep
{
    private readonly IConfiguration _configuration;

    public override string StepName => "CreateDoctorProfile";
    public override int Order => 2;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(2);

    public CreateDoctorProfileGrpcStep(ILogger<CreateDoctorProfileGrpcStep> logger, IConfiguration configuration)
        : base(logger)
    {
        _configuration = configuration;
    }

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[CreateDoctorProfileGrpcStep] Creating doctor profile for saga {SagaId}", context.SagaId);

            // Get data from context
            var accountId = context.GetData<string>(SagaConstants.ACCOUNT_ID_KEY);
            var email = context.GetData<string>("Email");
            var fullName = context.GetData<string>("FullName");
            var gender = context.GetData<string>("Gender");
            var address = context.GetData<string>("Address");
            var bio = context.GetData<string>("Bio");
            var yearsOfExperience = context.GetData<int>("YearsOfExperience");
            var avatarUrl = context.GetData<string>("AvatarUrl");
            var specialtyId = context.GetData<string>("SpecialtyId");
            var positionId = context.GetData<string>("PositionId");
            var hospitalId = context.GetData<string>("HospitalId");

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

            // Get languageIds and servicePrices from context
            var languageIdsStr = context.GetData<string>("LanguageIds");
            var servicePricesJson = context.GetData<string>("ServicePrices");

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

            // Add language IDs if available
            if (!string.IsNullOrWhiteSpace(languageIdsStr))
            {
                var languageIds = languageIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                request.LanguageIds.AddRange(languageIds);
            }

            // Add service prices if available
            if (!string.IsNullOrWhiteSpace(servicePricesJson))
            {
                try
                {
                    var prices = System.Text.Json.JsonSerializer.Deserialize<List<PriceData>>(servicePricesJson);
                    if (prices != null)
                    {
                        foreach (var price in prices)
                        {
                            request.Prices.Add(new DoctorPriceInput
                            {
                                ServiceTypeId = price.ServiceTypeId,
                                Amount = (double)price.Amount
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[CreateDoctorProfileGrpcStep] Failed to parse ServicePrices JSON");
                }
            }

            var response = await client.CreateDoctorAsync(request, cancellationToken: cancellationToken);

            return HandleSuccessfulResponse(response, "Id", "Email", StepName, context, "DoctorId");
        }
        catch (Exception ex)
        {
            return HandleGrpcException(ex, "creating doctor profile", StepName);
        }
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            LogCompensationStart(StepName, context.SagaId, "deleting doctor profile");

            var doctorId = context.GetData<string>("DoctorId");
            if (string.IsNullOrEmpty(doctorId))
            {
                return LogCompensationWarning(StepName, "DoctorId");
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
        catch (Exception ex)
        {
            return HandleGrpcException(ex, "compensating doctor profile", StepName);
        }
    }
}

/// <summary>
/// Step 1: Create External Account in Auth Service (for Google/Facebook login)
/// </summary>
public class CreateExternalAccountGrpcStep : BaseGrpcStep
{
    private readonly IConfiguration _configuration;

    public override string StepName => "CreateExternalAccount";
    public override int Order => 1;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(2);

    public CreateExternalAccountGrpcStep(ILogger<CreateExternalAccountGrpcStep> logger, IConfiguration configuration)
        : base(logger)
    {
        _configuration = configuration;
    }

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[CreateExternalAccountGrpcStep] Creating external account for saga {SagaId}", context.SagaId);

            // Get external user data from context (limited information from Google/Facebook)
            var email = context.GetData<string>("Email");
            var fullName = context.GetData<string>("FullName");
            var avatarUrl = context.GetData<string>("AvatarUrl");
            var externalProvider = context.GetData<string>("ExternalProvider");
            var externalUserId = context.GetData<string>("ExternalUserId");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(externalProvider) || string.IsNullOrEmpty(externalUserId))
            {
                return Failure("Missing required data: Email, ExternalProvider, or ExternalUserId");
            }

            // Create gRPC client for Auth Service
            var authGrpcUrl = _configuration["Services:Auth:GrpcUrl"];
            if (string.IsNullOrEmpty(authGrpcUrl))
            {
                return Failure("Auth service gRPC URL not configured");
            }

            using var grpcChannel = GrpcChannel.ForAddress(authGrpcUrl);
            var client = new AuthService.AuthServiceClient(grpcChannel);

            // Call CreateExternalAccount gRPC method
            var request = new CreateExternalAccountRequest
            {
                Email = email,
                FullName = fullName ?? "",
                AvatarUrl = avatarUrl ?? "",
                ExternalProvider = externalProvider,
                ExternalUserId = externalUserId
            };

            var response = await client.CreateExternalAccountAsync(request, cancellationToken: cancellationToken);

            if (response.Success)
            {
                // Store AccountId for next steps and compensation
                context.SetData(SagaConstants.ACCOUNT_ID_KEY, response.AccountId);

                _logger.LogInformation("[CreateExternalAccountGrpcStep] External account created successfully: {AccountId}", response.AccountId);

                return Success(new Dictionary<string, object>
                {
                    { SagaConstants.ACCOUNT_ID_KEY, response.AccountId },
                    { "AccountEmail", response.Email }
                });
            }
            else
            {
                return Failure($"Failed to create external account: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            return HandleGrpcException(ex, "creating external account", StepName);
        }
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        return await CompensateAccountDeletionAsync(context, _configuration, StepName, "deleting external account", SagaConstants.ACCOUNT_ID_KEY, cancellationToken);
    }
}

/// <summary>
/// Step 2: Create Hospital Profile in Hospital Service
/// </summary>
public class CreateHospitalProfileGrpcStep : BaseGrpcStep
{
    private const string HospitalIdKey = "HospitalId";

    private readonly IConfiguration _configuration;

    public override string StepName => "CreateHospitalProfile";
    public override int Order => 2;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(2);

    public CreateHospitalProfileGrpcStep(ILogger<CreateHospitalProfileGrpcStep> logger, IConfiguration configuration)
        : base(logger)
    {
        _configuration = configuration;
    }

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[CreateHospitalProfileGrpcStep] Creating hospital profile for saga {SagaId}", context.SagaId);

            // Get data from context
            var accountId = context.GetData<string>(SagaConstants.ACCOUNT_ID_KEY);
            var email = context.GetData<string>("Email");
            var hospitalName = context.GetData<string>("HospitalName");
            var phone = context.GetData<string>("PhoneNumber");
            var address = context.GetData<string>("Address");

            if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(hospitalName))
            {
                return Failure("Missing required hospital data: AccountId, Email, or HospitalName");
            }

            // Create gRPC client for Hospital Service
            var hospitalGrpcUrl = _configuration["Services:Hospital:GrpcUrl"];
            if (string.IsNullOrEmpty(hospitalGrpcUrl))
            {
                return Failure("Hospital service gRPC URL not configured");
            }

            using var grpcChannel = GrpcChannel.ForAddress(hospitalGrpcUrl);
            var client = new HospitalService.HospitalServiceClient(grpcChannel);

            // Call CreateHospital gRPC method
            var request = new CreateHospitalGrpcRequest
            {
                AccountId = accountId,
                Name = hospitalName,
                Address = address ?? "",
                Phone = phone ?? "",
                Email = email,
                Description = $"Bệnh viện {hospitalName}",
                BackgroundUrl = "",
                AvatarUrl = ""
            };

            var response = await client.CreateHospitalAsync(request, cancellationToken: cancellationToken);

            if (!string.IsNullOrEmpty(response.Id))
            {
                context.SetData(HospitalIdKey, response.Id);

                _logger.LogInformation("[CreateHospitalProfileGrpcStep] Hospital profile created successfully: {HospitalId}", response.Id);

                return Success(new Dictionary<string, object>
                {
                    { HospitalIdKey, response.Id }
                });
            }
            else
            {
                return Failure("Failed to create hospital profile: No hospital ID returned");
            }
        }
        catch (Exception ex)
        {
            return HandleGrpcException(ex, "creating hospital profile", StepName);
        }
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            LogCompensationStart(StepName, context.SagaId, "deleting hospital profile");

            var hospitalId = context.GetData<string>(HospitalIdKey);
            if (string.IsNullOrEmpty(hospitalId))
            {
                return LogCompensationWarning(StepName, HospitalIdKey);
            }

            // Create gRPC client for Hospital Service
            var hospitalGrpcUrl = _configuration["Services:Hospital:GrpcUrl"];
            if (string.IsNullOrEmpty(hospitalGrpcUrl))
            {
                return Failure("Hospital service gRPC URL not configured for compensation");
            }

            using var grpcChannel = GrpcChannel.ForAddress(hospitalGrpcUrl);
            var client = new HospitalService.HospitalServiceClient(grpcChannel);

            // Call DeleteHospital gRPC method
            var request = new DeleteHospitalRequest
            {
                Id = hospitalId
            };

            var response = await client.DeleteHospitalAsync(request, cancellationToken: cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation("[CreateHospitalProfileGrpcStep] Hospital profile compensated successfully: {HospitalId}", hospitalId);
                return Success();
            }
            else
            {
                return Failure($"Failed to compensate hospital profile: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            return HandleGrpcException(ex, "compensating hospital profile", StepName);
        }
    }
}

/// <summary>
/// Helper class for deserializing service prices from JSON
/// </summary>
internal class PriceData
{
    public string ServiceTypeId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}