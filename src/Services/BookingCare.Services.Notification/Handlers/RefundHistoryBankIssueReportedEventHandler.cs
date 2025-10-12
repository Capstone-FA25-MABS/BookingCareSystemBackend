using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for RefundHistoryBankIssueReportedIntegrationEvent
/// Sends email and SMS notifications to patient about bank account issues preventing refund
/// </summary>
public class RefundHistoryBankIssueReportedEventHandler
    : BaseRefundNotificationHandler<RefundHistoryBankIssueReportedIntegrationEvent>,
      IIntegrationEventHandler<RefundHistoryBankIssueReportedIntegrationEvent>
{
    public RefundHistoryBankIssueReportedEventHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ILogger<RefundHistoryBankIssueReportedEventHandler> logger)
        : base(emailService, fcmService, deviceStore, logger)
    {
    }

    public async Task HandleAsync(RefundHistoryBankIssueReportedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation(
                "Processing bank issue notification for RefundHistoryId: {RefundHistoryId}, UserId: {UserId}",
                @event.RefundHistoryId, @event.UserId);

            // Use user information from event - fallback to "Quý khách" if not provided
            var patientName = "Quý khách"; // We don't have UserFullName in BankIssueReportedEvent

            // Build notification content
            var emailSubject = "Sự cố hoàn tiền - Cần cập nhật thông tin tài khoản - BookingCare";
            var emailContent = EmailTemplate.BuildRefundBankIssueReportedEmailHtml(
                patientName,
                @event.RefundAmount,
                @event.IssueDescription,
                @event.BankAccount?.BankName,
                @event.BankAccount?.AccountNumber);

            var smsContent = $"BookingCare: Su co hoan tien {@event.RefundAmount:N0} VND. Ly do: {@event.IssueDescription}. Vui long cap nhat lai thong tin tai khoan ngan hang de nhan tien. Hotline: 1900-xxxx";

            // Send Email
            await SendEmailNotificationAsync(
                @event.UserEmail,
                emailSubject,
                emailContent,
                @event.RefundHistoryId.ToString(),
                @event.UserId.ToString(),
                "bank issue",
                cancellationToken);

            // Send SMS
            await SendSmsNotificationAsync(
                @event.UserPhone,
                smsContent,
                @event.RefundHistoryId.ToString(),
                @event.UserId.ToString(),
                "bank issue");

            Logger.LogInformation(
                "Completed notification processing for bank issue {RefundHistoryId}",
                @event.RefundHistoryId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error processing bank issue notification for RefundHistoryId {RefundHistoryId}: {Error}",
                @event.RefundHistoryId, ex.Message);
            // Don't re-throw - notifications are best-effort
        }
    }
}

