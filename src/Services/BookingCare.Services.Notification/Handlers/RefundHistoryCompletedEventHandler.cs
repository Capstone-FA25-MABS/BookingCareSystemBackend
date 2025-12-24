using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for RefundHistoryCompletedIntegrationEvent
/// Sends email and SMS notifications to patient about successful refund transfer
/// </summary>
public class RefundHistoryCompletedEventHandler
    : BaseRefundNotificationHandler,
      IIntegrationEventHandler<RefundHistoryCompletedIntegrationEvent>
{
    public RefundHistoryCompletedEventHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ILogger<RefundHistoryCompletedEventHandler> logger)
        : base(emailService, fcmService, deviceStore, logger)
    {
    }

    public async Task HandleAsync(RefundHistoryCompletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation(
                "Processing refund completed notification for RefundHistoryId: {RefundHistoryId}, UserId: {UserId}",
                @event.RefundHistoryId, @event.UserId);

            // Use user information from event
            var patientName = @event.UserFullName ?? "Quý khách";

            // Build notification content
            var emailSubject = "Hoàn tiền thành công - MedCure";
            var emailContent = EmailTemplate.BuildRefundCompletedEmailHtml(
                patientName,
                @event.RefundAmount,
                @event.BankAccount.BankName,
                @event.BankAccount.AccountNumber,
                @event.TransferDate);

            var smsContent = $"MedCure: Hoan tien thanh cong {@event.RefundAmount:N0} VND vao TK {@event.BankAccount.BankName} - {@event.BankAccount.AccountNumber}. Vui long kiem tra tai khoan ngan hang cua quy khach.";

            // Send Email
            await SendEmailNotificationAsync(
                @event.UserEmail,
                emailSubject,
                emailContent,
                @event.RefundHistoryId.ToString(),
                @event.UserId.ToString(),
                "refund completion",
                cancellationToken);

            // Send SMS
            await SendSmsNotificationAsync(
                @event.UserPhone,
                smsContent,
                @event.RefundHistoryId.ToString(),
                @event.UserId.ToString(),
                "refund completion");

            Logger.LogInformation(
                "Completed notification processing for refund completion {RefundHistoryId}",
                @event.RefundHistoryId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error processing refund completion notification for RefundHistoryId {RefundHistoryId}: {Error}",
                @event.RefundHistoryId, ex.Message);
            // Don't re-throw - notifications are best-effort
        }
    }
}

