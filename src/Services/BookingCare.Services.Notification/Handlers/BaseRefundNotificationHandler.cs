using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Base class for refund notification handlers
/// Contains shared logic for sending email and SMS notifications
/// </summary>
public abstract class BaseRefundNotificationHandler
{
    protected readonly EmailService EmailService;
    protected readonly FcmV1Service FcmService;
    protected readonly DeviceStore DeviceStore;
    protected readonly ILogger Logger;

    protected BaseRefundNotificationHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ILogger logger)
    {
        EmailService = emailService;
        FcmService = fcmService;
        DeviceStore = deviceStore;
        Logger = logger;
    }

    /// <summary>
    /// Sends email notification
    /// </summary>
    protected async Task SendEmailNotificationAsync(
        string? emailAddress,
        string subject,
        string htmlContent,
        string refundHistoryId,
        string userId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            Logger.LogWarning(
                "No email address available for user {UserId}",
                userId);
            return;
        }

        try
        {
            await EmailService.SendEmailAsync(
                emailAddress,
                subject,
                htmlContent,
                isHtml: true,
                cancellationToken);

            Logger.LogInformation(
                "Email sent successfully to {Email} for {EventType} {RefundHistoryId}",
                emailAddress, eventType, refundHistoryId);
        }
        catch (Exception emailEx)
        {
            Logger.LogError(emailEx,
                "Failed to send email to {Email} for {EventType} {RefundHistoryId}",
                emailAddress, eventType, refundHistoryId);
            // Don't throw - continue to try SMS
        }
    }

    /// <summary>
    /// Sends SMS notification via FCM
    /// </summary>
    protected async Task SendSmsNotificationAsync(
        string? phoneNumber,
        string smsContent,
        string refundHistoryId,
        string userId,
        string eventType)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            Logger.LogWarning(
                "No phone number available for user {UserId} {EventType} notification",
                userId, eventType);
            return;
        }

        try
        {
            var normalizedPhone = FcmV1Service.NormalizePhone(phoneNumber);

            // Get first active device (SMS gateway)
            var device = await DeviceStore.GetFirstActiveDeviceAsync();

            if (device != null)
            {
                try
                {
                    var data = new { phone = normalizedPhone, message = smsContent };
                    var result = await FcmService.SendDataMessageAsync(device.Token, data);

                    // Update last used timestamp
                    await DeviceStore.UpdateLastUsedAsync(device.Id);

                    Logger.LogInformation(
                        "SMS sent successfully for {EventType} {RefundHistoryId} to {Phone} via device {DeviceId}. Result: {Result}",
                        eventType, refundHistoryId, normalizedPhone, device.Id, result);
                }
                catch (Exception fcmEx)
                {
                    Logger.LogError(fcmEx,
                        "Failed to send SMS via FCM for {EventType} {RefundHistoryId} to {Phone} via device {DeviceId}",
                        eventType, refundHistoryId, normalizedPhone, device.Id);
                    // Don't throw - continue processing
                }
            }
            else
            {
                Logger.LogWarning(
                    "No active SMS gateway device available for {EventType} {RefundHistoryId}. SMS content: {Message}",
                    eventType, refundHistoryId, smsContent);
            }
        }
        catch (Exception smsEx)
        {
            Logger.LogError(smsEx,
                "Error processing SMS notification for {EventType} {RefundHistoryId}",
                eventType, refundHistoryId);
            // Don't throw - SMS is optional
        }
    }
}

