using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for RefundHistoryBankIssueReportedIntegrationEvent
/// Sends email and SMS notifications to patient about bank account issues preventing refund
/// </summary>
public class RefundHistoryBankIssueReportedEventHandler : IIntegrationEventHandler<RefundHistoryBankIssueReportedIntegrationEvent>
{
    private readonly EmailService _emailService;
    private readonly FcmV1Service _fcmService;
    private readonly DeviceStore _deviceStore;
    private readonly ILogger<RefundHistoryBankIssueReportedEventHandler> _logger;

    public RefundHistoryBankIssueReportedEventHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ILogger<RefundHistoryBankIssueReportedEventHandler> logger)
    {
        _emailService = emailService;
        _fcmService = fcmService;
        _deviceStore = deviceStore;
        _logger = logger;
    }

    public async Task HandleAsync(RefundHistoryBankIssueReportedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing bank issue notification for RefundHistoryId: {RefundHistoryId}, UserId: {UserId}",
                @event.RefundHistoryId, @event.UserId);

            // Use user information from event - fallback to "Quý khách" if not provided
            var patientName = "Quý khách"; // We don't have UserFullName in BankIssueReportedEvent
            var patientEmail = @event.UserEmail;
            var patientPhone = @event.UserPhone;

            // Build notification content
            string emailSubject = "Sự cố hoàn tiền - Cần cập nhật thông tin tài khoản - BookingCare";
            string emailContent = EmailTemplate.BuildRefundBankIssueReportedEmailHtml(
                patientName,
                @event.RefundAmount,
                @event.IssueDescription,
                @event.BankAccount?.BankName,
                @event.BankAccount?.AccountNumber);

            // Send Email if email address is available
            if (!string.IsNullOrWhiteSpace(patientEmail))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        patientEmail,
                        emailSubject,
                        emailContent,
                        isHtml: true,
                        cancellationToken);

                    _logger.LogInformation(
                        "Email sent successfully to {Email} for bank issue {RefundHistoryId}",
                        patientEmail, @event.RefundHistoryId);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx,
                        "Failed to send email to {Email} for bank issue {RefundHistoryId}",
                        patientEmail, @event.RefundHistoryId);
                    // Don't throw - continue to try SMS
                }
            }
            else
            {
                _logger.LogWarning(
                    "No email address available for user {UserId}",
                    @event.UserId);
            }

            // Send SMS if phone number is available
            if (!string.IsNullOrWhiteSpace(patientPhone))
            {
                try
                {
                    var normalizedPhone = FcmV1Service.NormalizePhone(patientPhone);
                    var smsContent = $"BookingCare: Su co hoan tien {@event.RefundAmount:N0} VND. Ly do: {@event.IssueDescription}. Vui long cap nhat lai thong tin tai khoan ngan hang de nhan tien. Hotline: 1900-xxxx";

                    // Get first active device (SMS gateway)
                    var device = await _deviceStore.GetFirstActiveDeviceAsync();

                    if (device != null)
                    {
                        try
                        {
                            var data = new { phone = normalizedPhone, message = smsContent };
                            var result = await _fcmService.SendDataMessageAsync(device.Token, data);

                            // Update last used timestamp
                            await _deviceStore.UpdateLastUsedAsync(device.Id);

                            _logger.LogInformation(
                                "SMS sent successfully for bank issue {RefundHistoryId} to {Phone} via device {DeviceId}. Result: {Result}",
                                @event.RefundHistoryId, normalizedPhone, device.Id, result);
                        }
                        catch (Exception fcmEx)
                        {
                            _logger.LogError(fcmEx,
                                "Failed to send SMS via FCM for bank issue {RefundHistoryId} to {Phone} via device {DeviceId}",
                                @event.RefundHistoryId, normalizedPhone, device.Id);
                            // Don't throw - continue processing
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "No active SMS gateway device available for bank issue {RefundHistoryId}. SMS content: {Message}",
                            @event.RefundHistoryId, smsContent);
                    }
                }
                catch (Exception smsEx)
                {
                    _logger.LogError(smsEx,
                        "Error processing SMS notification for bank issue {RefundHistoryId}",
                        @event.RefundHistoryId);
                    // Don't throw - SMS is optional
                }
            }
            else
            {
                _logger.LogWarning(
                    "No phone number available for user {UserId} bank issue notification",
                    @event.UserId);
            }

            _logger.LogInformation(
                "Completed notification processing for bank issue {RefundHistoryId}",
                @event.RefundHistoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing bank issue notification for RefundHistoryId {RefundHistoryId}: {Error}",
                @event.RefundHistoryId, ex.Message);
            // Don't re-throw - notifications are best-effort
        }
    }
}

