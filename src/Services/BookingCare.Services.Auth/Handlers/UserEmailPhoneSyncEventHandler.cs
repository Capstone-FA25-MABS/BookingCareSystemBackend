using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Auth.Repositories;

namespace BookingCare.Services.Auth.Handlers;

/// <summary>
/// Event handler for synchronizing email and phone updates from User Service to Auth Service
/// </summary>
public class UserEmailPhoneSyncEventHandler : IIntegrationEventHandler<UserEmailPhoneSyncRequestedEvent>
{
    private readonly IAuthRepository _authRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<UserEmailPhoneSyncEventHandler> _logger;

    public UserEmailPhoneSyncEventHandler(
        IAuthRepository authRepository,
        IEventBus eventBus,
        ILogger<UserEmailPhoneSyncEventHandler> logger)
    {
        _authRepository = authRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(UserEmailPhoneSyncRequestedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[AuthService] Received UserEmailPhoneSyncRequestedEvent - AccountId: {AccountId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
            @event.AccountId, @event.UserId, @event.CorrelationId);

        try
        {
            // Get account from database
            var account = await _authRepository.GetAccountByIdAsync(@event.AccountId);
            if (account == null)
            {
                _logger.LogError(
                    "[AuthService] Account not found for sync - AccountId: {AccountId}, CorrelationId: {CorrelationId}",
                    @event.AccountId, @event.CorrelationId);

                // Publish failure event
                await PublishFailureEventAsync(@event, "Account not found");
                return;
            }

            var hasChanges = false;

            // Update email if provided and different
            if (!string.IsNullOrEmpty(@event.NewEmail) && @event.NewEmail != account.Email)
            {
                _logger.LogInformation(
                    "[AuthService] Updating email from {OldEmail} to {NewEmail} for AccountId: {AccountId}",
                    account.Email, @event.NewEmail, @event.AccountId);

                account.Email = @event.NewEmail;
                hasChanges = true;
            }

            // Update phone if provided and different
            if (!string.IsNullOrEmpty(@event.NewPhone) && @event.NewPhone != account.PhoneNumber)
            {
                _logger.LogInformation(
                    "[AuthService] Updating phone from {OldPhone} to {NewPhone} for AccountId: {AccountId}",
                    account.PhoneNumber ?? "null", @event.NewPhone, @event.AccountId);

                account.PhoneNumber = @event.NewPhone;
                hasChanges = true;
            }

            // Save changes if any
            if (hasChanges)
            {
                await _authRepository.UpdateAccountAsync(account);
                _logger.LogInformation(
                    "[AuthService] Successfully updated account - AccountId: {AccountId}, CorrelationId: {CorrelationId}",
                    @event.AccountId, @event.CorrelationId);

                // Publish success event
                await PublishSuccessEventAsync(@event);
            }
            else
            {
                _logger.LogInformation(
                    "[AuthService] No changes needed for account - AccountId: {AccountId}, CorrelationId: {CorrelationId}",
                    @event.AccountId, @event.CorrelationId);

                // Still publish success event
                await PublishSuccessEventAsync(@event);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AuthService] Failed to sync email/phone - AccountId: {AccountId}, CorrelationId: {CorrelationId}",
                @event.AccountId, @event.CorrelationId);

            // Publish failure event
            await PublishFailureEventAsync(@event, ex.Message);
        }
    }

    private async Task PublishSuccessEventAsync(UserEmailPhoneSyncRequestedEvent requestEvent)
    {
        var successEvent = new UserEmailPhoneSyncCompletedEvent
        {
            AccountId = requestEvent.AccountId,
            UserId = requestEvent.UserId,
            UpdatedEmail = requestEvent.NewEmail,
            UpdatedPhone = requestEvent.NewPhone,
            CorrelationId = requestEvent.CorrelationId,
            Success = true,
            CompletedAt = DateTime.UtcNow
        };

        await _eventBus.PublishAsync(successEvent);
        _logger.LogInformation(
            "[AuthService] Published UserEmailPhoneSyncCompletedEvent - CorrelationId: {CorrelationId}",
            requestEvent.CorrelationId);
    }

    private async Task PublishFailureEventAsync(UserEmailPhoneSyncRequestedEvent requestEvent, string errorMessage)
    {
        var failureEvent = new UserEmailPhoneSyncFailedEvent
        {
            AccountId = requestEvent.AccountId,
            UserId = requestEvent.UserId,
            OriginalEmail = requestEvent.OriginalEmail,
            OriginalPhone = requestEvent.OriginalPhone,
            NewEmail = requestEvent.NewEmail,
            NewPhone = requestEvent.NewPhone,
            CorrelationId = requestEvent.CorrelationId,
            ErrorMessage = errorMessage,
            FailedAt = DateTime.UtcNow
        };

        await _eventBus.PublishAsync(failureEvent);
        _logger.LogError(
            "[AuthService] Published UserEmailPhoneSyncFailedEvent - CorrelationId: {CorrelationId}, Error: {Error}",
            requestEvent.CorrelationId, errorMessage);
    }
}

