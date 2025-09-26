using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Core;
using BookingCare.Shared.Saga.Steps;
using Microsoft.Extensions.DependencyInjection;

namespace BookingCare.Shared.Saga.SagaDefinition;

/// <summary>
/// Saga definition for User registration process
/// </summary>
public class UserRegistrationSaga : SagaDefinitionBase
{
    public override string SagaName => "UserRegistration";
    public override TimeSpan GlobalTimeout => TimeSpan.FromMinutes(10);

    public UserRegistrationSaga(IServiceProvider serviceProvider)
    {
        // Step 1: Create Account in Auth Service
        AddStep(serviceProvider.GetRequiredService<CreateAccountGrpcStep>());

        // Step 2: Create User Profile in User Service
        AddStep(serviceProvider.GetRequiredService<CreateUserProfileGrpcStep>());
    }
}

/// <summary>
/// Saga definition for Doctor registration process
/// </summary>
public class DoctorRegistrationSaga : SagaDefinitionBase
{
    public override string SagaName => "DoctorRegistration";
    public override TimeSpan GlobalTimeout => TimeSpan.FromMinutes(15);

    public DoctorRegistrationSaga(IServiceProvider serviceProvider)
    {
        // Step 1: Create Account in Auth Service
        AddStep(serviceProvider.GetRequiredService<CreateAccountGrpcStep>());

        // Step 2: Create Doctor Profile in Doctor Service
        AddStep(serviceProvider.GetRequiredService<CreateDoctorProfileGrpcStep>());
    }
}

