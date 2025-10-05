using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.User.Handlers;

/// <summary>
/// Event handler for successful Auth Service sync - optional for logging/audit
/// </summary>
public class UserEmailPhoneSyncCompletedEventHandler : IIntegrationEventHandler<UserEmailPhoneSyncCompletedEvent>
{
    private readonly ILogger<UserEmailPhoneSyncCompletedEventHandler> _logger;

    public UserEmailPhoneSyncCompletedEventHandler(
        ILogger<UserEmailPhoneSyncCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(UserEmailPhoneSyncCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[UserService] Email/Phone sync completed successfully - UserId: {UserId}, AccountId: {AccountId}, CorrelationId: {CorrelationId}",
            @event.UserId, @event.AccountId, @event.CorrelationId);

        // Optional: Update audit log, send notification to user, etc.
        // For example:
        // - Log to audit table
        // - Send email notification to user about profile update
        // - Update last_synced_at timestamp

        await Task.CompletedTask;
    }
}

