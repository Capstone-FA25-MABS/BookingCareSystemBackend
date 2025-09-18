using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;
using BookingCare.Services.Notification.Utils.OTP;
using BookingCare.Shared.Cache.Constants;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Notification.Exceptions;

namespace BookingCare.Services.Notification.Handlers;

public class NotificationSendEventHandler : IIntegrationEventHandler<NotificationSendEvent>
{
    private readonly ILogger<NotificationSendEventHandler> _logger;
    private readonly EmailService _emailService;
    private readonly FcmV1Service _fcmService;
    private readonly DeviceStore _deviceStore;
    private readonly ManageOtp _otpManager;
    private readonly EmailTemplate _emailTemplate;

    public NotificationSendEventHandler(
        ILogger<NotificationSendEventHandler> logger,
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ManageOtp otpManager,
        EmailTemplate emailTemplate)
    {
        _logger = logger;
        _emailService = emailService;
        _fcmService = fcmService;
        _deviceStore = deviceStore;
        _otpManager = otpManager;
        _emailTemplate = emailTemplate;
    }

    public async Task HandleAsync(NotificationSendEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (@event.Type.ToLower())
            {
                case "email":
                    await HandleEmailNotification(@event, cancellationToken);
                    break;
                case "sms":
                    await HandleSmsNotification(@event);
                    break;
                default:
                    _logger.LogWarning("Unsupported notification type: {Type}", @event.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling NotificationSendEvent");
            // Re-throw as NotificationException for proper error handling
            throw new NotificationException($"Failed to handle notification event of type '{@event.Type}'", "NOTIFICATION_HANDLER_ERROR", System.Net.HttpStatusCode.InternalServerError, ex);
        }
    }

    private async Task HandleEmailNotification(NotificationSendEvent @event, CancellationToken cancellationToken)
    {
        if (!@event.Data.TryGetValue("email", out var emailObj) || emailObj is null)
        {
            throw new EmailDeliveryException("NotificationSendEvent missing 'email' in Data");
        }

        var email = emailObj.ToString() ?? string.Empty;
        var subject = @event.Data.TryGetValue("subject", out var subjectObj) ? subjectObj?.ToString() ?? @event.Title : @event.Title;
        var isHtml = @event.Data.TryGetValue("html", out var htmlObj) && bool.TryParse(htmlObj?.ToString(), out var html) ? html : false;
        var purpose = @event.Data.TryGetValue("purpose", out var purposeObj) ? purposeObj?.ToString() ?? string.Empty : string.Empty;

        string message = @event.Message;

        // Check if this is a password reset email and use template if resetUrl is provided
        if (purpose.Equals(OtpPurpose.FORGOT_PASSWORD.ToKey(), StringComparison.OrdinalIgnoreCase) &&
            @event.Data.TryGetValue("resetUrl", out var resetUrlObj) &&
            !string.IsNullOrEmpty(resetUrlObj?.ToString()))
        {
            var resetUrl = resetUrlObj.ToString()!;
            message = _emailTemplate.BuildPasswordResetEmailHtml(resetUrl);
            isHtml = true; // Force HTML for template
            _logger.LogInformation("Using password reset email template for {Email}", email);
        }

        try
        {
            await _emailService.SendEmailAsync(email, subject, message, isHtml, cancellationToken);
            _logger.LogInformation("Email notification sent to {Email} for purpose {Purpose}", email, purpose);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
            throw new EmailDeliveryException($"Failed to send email to {email}", email, ex);
        }
    }

    private async Task HandleSmsNotification(NotificationSendEvent @event)
    {
        if (!@event.Data.TryGetValue("phone", out var phoneObj) || phoneObj is null)
        {
            throw new SmsDeliveryException("NotificationSendEvent missing 'phone' in Data");
        }

        if (!@event.Data.TryGetValue("deviceId", out var deviceIdObj) || deviceIdObj is null)
        {
            throw new SmsDeliveryException("NotificationSendEvent missing 'deviceId' in Data");
        }

        var phone = phoneObj.ToString() ?? string.Empty;
        var deviceId = deviceIdObj.ToString() ?? string.Empty;
        var device = await _deviceStore.GetAsync(deviceId);

        if (device == null)
        {
            throw new DeviceException("Device not found for SMS notification");
        }

        var normalizedPhone = _fcmService.NormalizePhone(phone);

        // Purpose-specific OTP generation and storage
        var purposeKey = @event.Data.TryGetValue("purpose", out var p) ? (p?.ToString() ?? string.Empty) : string.Empty;
        var isForgot = purposeKey.Equals(OtpPurpose.FORGOT_PASSWORD.ToKey(), StringComparison.OrdinalIgnoreCase);
        if (isForgot)
        {
            var otp = _otpManager.GenerateNumericOtp();
            // Use a namespaced cache key to avoid cross-purpose collisions
            var phoneKey = CacheKeys.Format(CacheKeys.OtpPurposePhone, purposeKey, normalizedPhone);
            await _otpManager.StoreOtpAsync(phoneKey, otp, TimeSpan.FromMinutes(5));
            // Overwrite message with OTP content if not provided
            if (string.IsNullOrWhiteSpace(@event.Message))
            {
                @event.Message = $"Your OTP is {otp} (valid 5 minutes) for password reset.";
            }
        }

        var data = new { phone = normalizedPhone, message = @event.Message };

        try
        {
            var result = await _fcmService.SendDataMessageAsync(device.Token, data);

            // Update last used timestamp
            await _deviceStore.UpdateLastUsedAsync(deviceId);

            _logger.LogInformation("SMS notification sent to {Phone} via device {DeviceId}. Result: {Result}", phone, deviceId, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Phone} via device {DeviceId}", phone, deviceId);
            throw new SmsDeliveryException($"Failed to send SMS to {phone}", phone, deviceId, ex);
        }
    }
}

