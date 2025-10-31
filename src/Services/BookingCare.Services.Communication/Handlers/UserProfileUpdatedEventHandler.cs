using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Communication.Handlers;

/// <summary>
/// ?? Handler for UserProfileUpdatedEvent to invalidate user cache in Communication Service
/// </summary>
public class UserProfileUpdatedEventHandler : BaseService, IIntegrationEventHandler<UserProfileUpdatedEvent>
{
    private readonly IParticipantEnrichmentService _participantEnrichmentService;

    public UserProfileUpdatedEventHandler(
        IParticipantEnrichmentService participantEnrichmentService,
        ILogger<UserProfileUpdatedEventHandler> logger) : base(logger)
    {
        _participantEnrichmentService = participantEnrichmentService;
    }

    /// <summary>
    /// Handle user profile updated event by invalidating cached user info
    /// </summary>
    public async Task HandleAsync(UserProfileUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            LogInfo("Processing UserProfileUpdatedEvent - UserId: {UserId}, AccountId: {AccountId}, CorrelationId: {CorrelationId}",
                null, @event.UserId, @event.AccountId, @event.CorrelationId);

            LogInfo("Updated fields: {Fields}", null, string.Join(", ", @event.UpdatedFields));

            // Invalidate cache for the updated account
            await _participantEnrichmentService.ClearAccountCacheAsync(@event.AccountId.ToString().ToLowerInvariant());

            LogInfo("Successfully cleared cache for AccountId: {AccountId}", null, @event.AccountId);

            // If email changed, also clear old email cache key (if applicable)
            if (!string.IsNullOrEmpty(@event.PreviousEmail) && @event.PreviousEmail != @event.Email)
            {
                LogInfo("Email changed from {OldEmail} to {NewEmail}, clearing old email cache",
                    null, @event.PreviousEmail, @event.Email);

                // Clear any email-based cache keys if your service uses them
                await _participantEnrichmentService.ClearAccountCacheAsync(@event.PreviousEmail.ToLowerInvariant());

                LogInfo("Successfully cleared cache for previous email: {PreviousEmail}", null, @event.PreviousEmail);
            }

            LogInfo("UserProfileUpdatedEvent processed successfully - UserId: {UserId}, CorrelationId: {CorrelationId}",
                null, @event.UserId, @event.CorrelationId);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error processing UserProfileUpdatedEvent - UserId: {UserId}, CorrelationId: {CorrelationId}",
                null, @event.UserId, @event.CorrelationId);

            // Don't throw - cache invalidation failure shouldn't break the system
            // The cache will eventually expire naturally
        }
    }
}