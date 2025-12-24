using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Enums;

namespace BookingCare.Services.Hospital.Handlers;

/// <summary>
/// Event handler for Hospital Account Creation Failed Event
/// Reverts registration status back to PENDING
/// </summary>
public class HospitalAccountCreationFailedEventHandler
    : IIntegrationEventHandler<HospitalAccountCreationFailedEvent>
{
    private readonly IHospitalRegistrationRepository _registrationRepository;
    private readonly ILogger<HospitalAccountCreationFailedEventHandler> _logger;

    public HospitalAccountCreationFailedEventHandler(
        IHospitalRegistrationRepository registrationRepository,
        ILogger<HospitalAccountCreationFailedEventHandler> logger)
    {
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    public async Task HandleAsync(
        HospitalAccountCreationFailedEvent @event,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "[HospitalAccountCreationFailedEventHandler] Hospital account creation failed for registration {RegistrationId}: {Error}",
            @event.RegistrationId,
            @event.ErrorMessage);

        try
        {
            // Get registration
            var registration = await _registrationRepository.GetByIdAsync(@event.RegistrationId);
            if (registration == null)
            {
                _logger.LogWarning(
                    "[HospitalAccountCreationFailedEventHandler] Registration {RegistrationId} not found. Skipping status update.",
                    @event.RegistrationId);
                return;
            }

            // Revert status back to CANCELLED
            registration.Status = RegistrationStatus.CANCELLED;
            registration.Reason = $"Tạo tài khoản thất bại: {@event.ErrorMessage}";

            await _registrationRepository.UpdateAsync(registration);

            _logger.LogInformation(
                "[HospitalAccountCreationFailedEventHandler] Registration {RegistrationId} status reverted to CANCELLED",
                @event.RegistrationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalAccountCreationFailedEventHandler] Error handling account creation failure for registration {RegistrationId}",
                @event.RegistrationId);
        }
    }
}

