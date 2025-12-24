using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Communication.Handlers;

/// <summary>
/// ?? Handler for UserProfileUpdatedEvent from Doctor Service to invalidate doctor cache in Communication Service
/// This handler processes both User and Doctor profile updates since doctors also use UserProfileUpdatedEvent
/// </summary>
public class DoctorProfileUpdatedEventHandler : BaseService, IIntegrationEventHandler<UserProfileUpdatedEvent>
{
    private readonly IParticipantEnrichmentService _participantEnrichmentService;

    public DoctorProfileUpdatedEventHandler(
        IParticipantEnrichmentService participantEnrichmentService,
        ILogger<DoctorProfileUpdatedEventHandler> logger) : base(logger)
    {
        _participantEnrichmentService = participantEnrichmentService;
    }

    /// <summary>
    /// Handle doctor profile updated event by invalidating cached doctor info
    /// Note: This handles UserProfileUpdatedEvent with Role = "DOCTOR"
    /// </summary>
    public async Task HandleAsync(UserProfileUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            // Only process if this is a doctor profile update
            if (@event.Role != "DOCTOR")
            {
                LogDebug("Ignoring UserProfileUpdatedEvent with Role: {Role} (not DOCTOR)", null, @event.Role);
                return;
            }

            LogInfo("Processing UserProfileUpdatedEvent for doctor - DoctorId: {DoctorId}, AccountId: {AccountId}, CorrelationId: {CorrelationId}",
                null, @event.UserId, @event.AccountId, @event.CorrelationId);

            LogInfo("Updated fields: {Fields}", null, string.Join(", ", @event.UpdatedFields));

            // Invalidate cache for the updated doctor account
            await _participantEnrichmentService.ClearAccountCacheAsync(@event.AccountId.ToString().ToLowerInvariant());

            LogInfo("Successfully cleared cache for Doctor AccountId: {AccountId}", null, @event.AccountId);

            // If email changed, also clear old email cache key (if applicable)
            if (!string.IsNullOrEmpty(@event.PreviousEmail) && @event.PreviousEmail != @event.Email)
            {
                LogInfo("Doctor email changed from {OldEmail} to {NewEmail}, clearing old email cache",
                    null, @event.PreviousEmail, @event.Email);

                // Clear any email-based cache keys if your service uses them
                await _participantEnrichmentService.ClearAccountCacheAsync(@event.PreviousEmail.ToLowerInvariant());

                LogInfo("Successfully cleared cache for previous doctor email: {PreviousEmail}", null, @event.PreviousEmail);
            }

            LogInfo("UserProfileUpdatedEvent processed successfully for doctor - DoctorId: {DoctorId}, CorrelationId: {CorrelationId}",
                null, @event.UserId, @event.CorrelationId);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error processing UserProfileUpdatedEvent for doctor - DoctorId: {DoctorId}, CorrelationId: {CorrelationId}",
                null, @event.UserId, @event.CorrelationId);

            // Don't throw - cache invalidation failure shouldn't break the system
            // The cache will eventually expire naturally
        }
    }
}