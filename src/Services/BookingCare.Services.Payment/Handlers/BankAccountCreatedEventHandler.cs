using BookingCare.Services.Payment.Enums;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Payment.Handlers;

/// <summary>
/// Handler for BankAccountCreatedIntegrationEvent
/// Updates RefundHistory status from WAITING to PENDING when patient adds bank account
/// </summary>
public class BankAccountCreatedEventHandler : IIntegrationEventHandler<BankAccountCreatedIntegrationEvent>
{
    private readonly IRefundHistoryRepository _refundHistoryRepository;
    private readonly ILogger<BankAccountCreatedEventHandler> _logger;

    public BankAccountCreatedEventHandler(
        IRefundHistoryRepository refundHistoryRepository,
        ILogger<BankAccountCreatedEventHandler> logger)
    {
        _refundHistoryRepository = refundHistoryRepository;
        _logger = logger;
    }

    public async Task HandleAsync(BankAccountCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing BankAccountCreatedIntegrationEvent for UserId: {UserId}, BankAccountId: {BankAccountId}",
                @event.UserId, @event.BankAccountId);

            // Find all WAITING refund histories for this user (optimized - no includes)
            var waitingRefunds = await _refundHistoryRepository.GetByUserIdAndStatusForUpdateAsync(
                @event.UserId,
                RefundStatus.WAITING);

            if (waitingRefunds == null || !waitingRefunds.Any())
            {
                _logger.LogInformation(
                    "No WAITING refund histories found for UserId: {UserId}",
                    @event.UserId);
                return;
            }

            _logger.LogInformation(
                "Found {Count} WAITING refund histories for UserId: {UserId}. Updating to PENDING status...",
                waitingRefunds.Count(), @event.UserId);

            // Update each WAITING refund to PENDING and link to bank account
            foreach (var refund in waitingRefunds)
            {
                refund.Status = RefundStatus.PENDING;
                refund.BankAccountId = @event.BankAccountId;
                refund.UpdatedAt = DateTime.UtcNow;

                await _refundHistoryRepository.UpdateAsync(refund);

                _logger.LogInformation(
                    "Updated RefundHistory {RefundId} status from WAITING to PENDING and linked to BankAccount {BankAccountId}",
                    refund.Id, @event.BankAccountId);
            }

            _logger.LogInformation(
                "Successfully updated {Count} refund histories for UserId: {UserId}",
                waitingRefunds.Count(), @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing BankAccountCreatedIntegrationEvent for UserId: {UserId}: {Error}",
                @event.UserId, ex.Message);
            // Don't re-throw - this is a best-effort operation
        }
    }
}

