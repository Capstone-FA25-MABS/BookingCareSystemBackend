using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.User.Repositories;

namespace BookingCare.Services.User.Handlers;

/// <summary>
/// Event handler for rollback when Auth Service sync fails
/// </summary>
public class UserEmailPhoneSyncFailedEventHandler : IIntegrationEventHandler<UserEmailPhoneSyncFailedEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserEmailPhoneSyncFailedEventHandler> _logger;

    public UserEmailPhoneSyncFailedEventHandler(
        IUserRepository userRepository,
        ILogger<UserEmailPhoneSyncFailedEventHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task HandleAsync(UserEmailPhoneSyncFailedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogError(
            "[UserService] Received UserEmailPhoneSyncFailedEvent - UserId: {UserId}, CorrelationId: {CorrelationId}, Error: {Error}",
            @event.UserId, @event.CorrelationId, @event.ErrorMessage);

        try
        {
            // Get user from database
            var user = await _userRepository.GetByIdAsync(@event.UserId);
            if (user == null)
            {
                _logger.LogError(
                    "[UserService] User not found for rollback - UserId: {UserId}, CorrelationId: {CorrelationId}",
                    @event.UserId, @event.CorrelationId);
                return;
            }

            bool needsRollback = false;

            // Rollback email if it was being updated
            if (!string.IsNullOrEmpty(@event.NewEmail) && user.Email == @event.NewEmail)
            {
                _logger.LogWarning(
                    "[UserService] Rolling back email - UserId: {UserId}, From: {NewEmail}, To: {OriginalEmail}",
                    @event.UserId, @event.NewEmail, @event.OriginalEmail);

                user.Email = @event.OriginalEmail ?? user.Email;
                needsRollback = true;
            }

            // Rollback phone if it was being updated
            if (!string.IsNullOrEmpty(@event.NewPhone) && user.Phone == @event.NewPhone)
            {
                _logger.LogWarning(
                    "[UserService] Rolling back phone - UserId: {UserId}, From: {NewPhone}, To: {OriginalPhone}",
                    @event.UserId, @event.NewPhone, @event.OriginalPhone);

                user.Phone = @event.OriginalPhone;
                needsRollback = true;
            }

            // Save rollback changes
            if (needsRollback)
            {
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation(
                    "[UserService] Successfully rolled back user changes - UserId: {UserId}, CorrelationId: {CorrelationId}",
                    @event.UserId, @event.CorrelationId);
            }

            _logger.LogInformation(
                "[UserService] Completed handling UserEmailPhoneSyncFailedEvent - CorrelationId: {CorrelationId}",
                @event.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[UserService] Exception while handling UserEmailPhoneSyncFailedEvent - UserId: {UserId}, CorrelationId: {CorrelationId}",
                @event.UserId, @event.CorrelationId);
        }
    }
}

