using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.Hospital.Handlers;

/// <summary>
/// Event handler for updating hospital registration with created hospital account details
/// </summary>
public class HospitalRegistrationAccountLinkedEventHandler
    : IIntegrationEventHandler<HospitalRegistrationAccountLinkedEvent>
{
    private readonly IHospitalRegistrationRepository _registrationRepository;
    private readonly ILogger<HospitalRegistrationAccountLinkedEventHandler> _logger;

    public HospitalRegistrationAccountLinkedEventHandler(
        IHospitalRegistrationRepository registrationRepository,
        ILogger<HospitalRegistrationAccountLinkedEventHandler> logger)
    {
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    public async Task HandleAsync(
        HospitalRegistrationAccountLinkedEvent @event,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[HospitalRegistrationAccountLinkedEventHandler] Received account linked event for RegistrationId {RegistrationId}, HospitalId {HospitalId}",
            @event.RegistrationId,
            @event.HospitalId);

        try
        {
            // Get registration
            var registration = await _registrationRepository.GetByIdAsync(@event.RegistrationId);

            if (registration == null)
            {
                _logger.LogWarning(
                    "[HospitalRegistrationAccountLinkedEventHandler] Registration {RegistrationId} not found",
                    @event.RegistrationId);
                return;
            }

            // Update hospitalId
            registration.HospitalId = @event.HospitalId;

            await _registrationRepository.UpdateAsync(registration);

            _logger.LogInformation(
                "[HospitalRegistrationAccountLinkedEventHandler] Successfully linked HospitalId {HospitalId} to RegistrationId {RegistrationId}",
                @event.HospitalId,
                @event.RegistrationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalRegistrationAccountLinkedEventHandler] Error linking hospital account for RegistrationId {RegistrationId}",
                @event.RegistrationId);

            // Don't throw - this is an async background process
            // The hospitalId can be manually updated by admin if needed
        }
    }
}

