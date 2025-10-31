using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Models;
using BookingCare.Shared.Saga.SagaDefinition;
using Microsoft.Extensions.Configuration;

namespace BookingCare.Services.Auth.Handlers;

/// <summary>
/// Event handler for Hospital Account Creation Requested Event
/// Triggers the Hospital Account Registration Saga
/// </summary>
public class HospitalAccountCreationRequestedEventHandler
    : IIntegrationEventHandler<HospitalAccountCreationRequestedEvent>
{
    private readonly ISagaManager _sagaManager;
    private readonly IEventBus _eventBus;
    private readonly ILogger<HospitalAccountCreationRequestedEventHandler> _logger;
    private readonly IConfiguration _configuration;

    public HospitalAccountCreationRequestedEventHandler(
        ISagaManager sagaManager,
        IEventBus eventBus,
        ILogger<HospitalAccountCreationRequestedEventHandler> logger,
        IConfiguration configuration)
    {
        _sagaManager = sagaManager;
        _eventBus = eventBus;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task HandleAsync(
        HospitalAccountCreationRequestedEvent @event,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[HospitalAccountCreationRequestedEventHandler] Received hospital account creation request for {Email}",
            @event.Email);

        try
        {
            // Create saga context
            var sagaContext = new SagaContext
            {
                SagaName = "HospitalAccountRegistration",
                CorrelationId = @event.RegistrationId.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            // Set saga data
            sagaContext.SetData("RegistrationId", @event.RegistrationId.ToString());
            sagaContext.SetData("Email", @event.Email);
            sagaContext.SetData("PhoneNumber", @event.Phone);
            sagaContext.SetData("Password", @event.GeneratedPassword); // CreateAccountGrpcStep reads "Password"
            sagaContext.SetData("Role", "STAFF"); // Hospital accounts have STAFF role
            sagaContext.SetData("HospitalName", @event.HospitalName);
            sagaContext.SetData("Address", @event.Address);
            sagaContext.SetData("TaxCode", @event.TaxCode);
            sagaContext.SetData("ContractFileUrl", @event.ContractFileUrl);

            // Execute saga
            var result = await _sagaManager.ExecuteSagaAsync<HospitalAccountRegistrationSaga>(sagaContext);

            if (result.Status == SagaStatus.Completed)
            {
                // Get AccountId and HospitalId from saga context
                var accountId = sagaContext.GetData<string>("AccountId");
                var hospitalId = sagaContext.GetData<string>("HospitalId");
                var parsedAccountId = Guid.Parse(accountId!);
                var parsedHospitalId = Guid.Parse(hospitalId!);

                // Publish event to update registration with hospitalId
                var linkedEvent = new HospitalRegistrationAccountLinkedEvent
                {
                    RegistrationId = @event.RegistrationId,
                    HospitalId = parsedHospitalId,
                    AccountId = parsedAccountId,
                    LinkedAt = DateTime.UtcNow
                };
                await _eventBus.PublishAsync(linkedEvent);

                // Publish success event for notification service
                var adminFrontendUrl = _configuration["FrontendOptions:Admin:BaseUrl"]?.TrimEnd('/')
                    ?? "http://localhost:5173";

                var successEvent = new HospitalAccountCreatedEvent
                {
                    RegistrationId = @event.RegistrationId,
                    AccountId = parsedAccountId,
                    HospitalId = parsedHospitalId,
                    Email = @event.Email,
                    HospitalName = @event.HospitalName,
                    GeneratedPassword = @event.GeneratedPassword,
                    LoginUrl = $"{adminFrontendUrl}/login",
                    ContractFileUrl = @event.ContractFileUrl,
                    CreatedAt = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(successEvent);

                _logger.LogInformation(
                    "[HospitalAccountCreationRequestedEventHandler] Hospital account creation saga completed for {Email}. Events published: HospitalRegistrationAccountLinkedEvent (RegistrationId: {RegistrationId}), HospitalAccountCreatedEvent",
                    @event.Email,
                    @event.RegistrationId);
            }
            else
            {
                _logger.LogError(
                    "[HospitalAccountCreationRequestedEventHandler] Hospital account creation saga failed for {Email}: {Error}",
                    @event.Email,
                    result.ErrorMessage);

                // Publish failure event
                var failureEvent = new HospitalAccountCreationFailedEvent
                {
                    RegistrationId = @event.RegistrationId,
                    Email = @event.Email,
                    ErrorMessage = result.ErrorMessage ?? "Unknown error",
                    FailedAt = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(failureEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalAccountCreationRequestedEventHandler] Exception handling hospital account creation for {Email}",
                @event.Email);

            // Publish failure event
            var failureEvent = new HospitalAccountCreationFailedEvent
            {
                RegistrationId = @event.RegistrationId,
                Email = @event.Email,
                ErrorMessage = ex.Message,
                FailedAt = DateTime.UtcNow
            };

            await _eventBus.PublishAsync(failureEvent);
        }
    }
}

