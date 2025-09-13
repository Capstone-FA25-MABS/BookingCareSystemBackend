using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Core;
using BookingCare.Shared.Saga.Steps.Grpc;
using Microsoft.Extensions.DependencyInjection;
using BookingCare.Shared.Saga.Models;

namespace BookingCare.Shared.Saga.Examples;

/// <summary>
/// User Registration Saga that coordinates user account creation and profile setup across services
/// This saga demonstrates distributed transaction management using gRPC calls between microservices
/// 
/// Saga Flow:
/// 1. CreateUserAccountGrpcStep - Creates user account in Auth Service
/// 2. CreateUserProfileGrpcStep - Creates user profile in User Service  
/// 3. SendVerificationEmailGrpcStep - Sends verification email via Notification Service
/// 
/// On failure, all completed steps are compensated in reverse order
/// </summary>
public class UserRegistrationGrpcSaga : SagaDefinitionBase
{
    private readonly IServiceProvider _serviceProvider;

    public UserRegistrationGrpcSaga(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ConfigureSteps();
    }

    public override string SagaName => "UserRegistrationGrpc";
    public override TimeSpan GlobalTimeout => TimeSpan.FromMinutes(10);

    private void ConfigureSteps()
    {
        // Step 1: Create user account in Auth Service
        AddStep(_serviceProvider.GetRequiredService<CreateUserAccountGrpcStep>());
        
        // Step 2: Create user profile in User Service
        AddStep(_serviceProvider.GetRequiredService<CreateUserProfileGrpcStep>());
        
        // Step 3: Send verification email
        AddStep(_serviceProvider.GetRequiredService<SendVerificationEmailGrpcStep>());
    }
}

/// <summary>
/// Data Transfer Object for User Registration Request
/// </summary>
public class UserRegistrationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Role { get; set; } = "patient";
}

/// <summary>
/// Factory for creating User Registration Saga contexts
/// </summary>
public static class UserRegistrationSagaFactory
{
    /// <summary>
    /// Creates a saga context for user registration
    /// </summary>
    /// <param name="request">User registration request data</param>
    /// <returns>Configured saga context</returns>
    public static SagaContext CreateContext(UserRegistrationRequest request)
    {
        var context = new SagaContext
        {
            SagaId = Guid.NewGuid(),
            SagaName = "UserRegistrationGrpc",
            CreatedAt = DateTime.UtcNow
        };

        // Set user registration data
        context.SetData("Email", request.Email);
        context.SetData("Password", request.Password);
        context.SetData("FirstName", request.FirstName);
        context.SetData("LastName", request.LastName);
        context.SetData("PhoneNumber", request.PhoneNumber);
        context.SetData("DateOfBirth", request.DateOfBirth);
        context.SetData("Gender", request.Gender);
        context.SetData("Address", request.Address);
        context.SetData("Role", request.Role);

        // Generate UserId for the saga
        context.SetData("UserId", Guid.NewGuid().ToString());

        return context;
    }

    /// <summary>
    /// Creates a saga context with minimal required data
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <param name="firstName">User first name</param>
    /// <param name="lastName">User last name</param>
    /// <returns>Configured saga context</returns>
    public static SagaContext CreateMinimalContext(string email, string password, string firstName, string lastName)
    {
        return CreateContext(new UserRegistrationRequest
        {
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName,
            Role = "patient"
        });
    }
}
