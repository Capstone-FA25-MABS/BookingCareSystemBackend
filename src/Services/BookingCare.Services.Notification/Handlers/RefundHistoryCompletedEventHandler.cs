using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for RefundHistoryCompletedIntegrationEvent
/// Sends email and SMS notifications to patient about successful refund transfer
/// </summary>
public class RefundHistoryCompletedEventHandler : IIntegrationEventHandler<RefundHistoryCompletedIntegrationEvent>
{
    private readonly EmailService _emailService;
    private readonly FcmV1Service _fcmService;
    private readonly DeviceStore _deviceStore;
    private readonly ILogger<RefundHistoryCompletedEventHandler> _logger;

    public RefundHistoryCompletedEventHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ILogger<RefundHistoryCompletedEventHandler> logger)
    {
        _emailService = emailService;
        _fcmService = fcmService;
        _deviceStore = deviceStore;
        _logger = logger;
    }

    public async Task HandleAsync(RefundHistoryCompletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing refund completed notification for RefundHistoryId: {RefundHistoryId}, UserId: {UserId}",
                @event.RefundHistoryId, @event.UserId);

            // Use user information from event
            var patientName = @event.UserFullName ?? "Quý khách";
            var patientEmail = @event.UserEmail;
            var patientPhone = @event.UserPhone;

            // Build notification content
            string emailSubject = "Hoàn tiền thành công - BookingCare";
            string emailContent = EmailTemplate.BuildRefundCompletedEmailHtml(
                patientName,
                @event.RefundAmount,
                @event.BankAccount.BankName,
                @event.BankAccount.AccountNumber,
                @event.TransferDate);

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
                        "Email sent successfully to {Email} for refund completion {RefundHistoryId}",
                        patientEmail, @event.RefundHistoryId);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx,
                        "Failed to send email to {Email} for refund completion {RefundHistoryId}",
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
                    var smsContent = $"BookingCare: Hoan tien thanh cong {@event.RefundAmount:N0} VND vao TK {@event.BankAccount.BankName} - {@event.BankAccount.AccountNumber}. Vui long kiem tra tai khoan ngan hang cua quy khach.";

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
                                "SMS sent successfully for refund completion {RefundHistoryId} to {Phone} via device {DeviceId}. Result: {Result}",
                                @event.RefundHistoryId, normalizedPhone, device.Id, result);
                        }
                        catch (Exception fcmEx)
                        {
                            _logger.LogError(fcmEx,
                                "Failed to send SMS via FCM for refund completion {RefundHistoryId} to {Phone} via device {DeviceId}",
                                @event.RefundHistoryId, normalizedPhone, device.Id);
                            // Don't throw - continue processing
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "No active SMS gateway device available for refund completion {RefundHistoryId}. SMS content: {Message}",
                            @event.RefundHistoryId, smsContent);
                    }
                }
                catch (Exception smsEx)
                {
                    _logger.LogError(smsEx,
                        "Error processing SMS notification for refund completion {RefundHistoryId}",
                        @event.RefundHistoryId);
                    // Don't throw - SMS is optional
                }
            }
            else
            {
                _logger.LogWarning(
                    "No phone number available for user {UserId} refund completion notification",
                    @event.UserId);
            }

            _logger.LogInformation(
                "Completed notification processing for refund completion {RefundHistoryId}",
                @event.RefundHistoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing refund completion notification for RefundHistoryId {RefundHistoryId}: {Error}",
                @event.RefundHistoryId, ex.Message);
            // Don't re-throw - notifications are best-effort
        }
    }
}

