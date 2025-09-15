using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;
using BookingCare.Services.Notification.Utils.OTP;
using BookingCare.Shared.Common.Enums;

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
                    await HandleSmsNotification(@event, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Unsupported notification type: {Type}", @event.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling NotificationSendEvent");
        }
    }

    private async Task HandleEmailNotification(NotificationSendEvent @event, CancellationToken cancellationToken)
    {
        if (!@event.Data.TryGetValue("email", out var emailObj) || emailObj is null)
        {
            _logger.LogWarning("NotificationSendEvent missing 'email' in Data");
            return;
        }

        var email = emailObj.ToString() ?? string.Empty;
        var subject = @event.Data.TryGetValue("subject", out var subjectObj) ? subjectObj?.ToString() ?? @event.Title : @event.Title;
        var isHtml = @event.Data.TryGetValue("html", out var htmlObj) && bool.TryParse(htmlObj?.ToString(), out var html) ? html : false;
        var purpose = @event.Data.TryGetValue("purpose", out var purposeObj) ? purposeObj?.ToString() ?? string.Empty : string.Empty;

        string message = @event.Message;

        // Check if this is a password reset email
        if (purpose.Equals(OtpPurpose.FORGOT_PASSWORD.ToKey(), StringComparison.OrdinalIgnoreCase))
        {
            // Use password reset template if resetUrl is provided
            if (@event.Data.TryGetValue("resetUrl", out var resetUrlObj) && !string.IsNullOrEmpty(resetUrlObj?.ToString()))
            {
                var resetUrl = resetUrlObj.ToString()!;
                message = _emailTemplate.BuildPasswordResetEmailHtml(resetUrl);
                isHtml = true; // Force HTML for template
                _logger.LogInformation("Using password reset email template for {Email}", email);
            }
        }

        await _emailService.SendEmailAsync(email, subject, message, isHtml, cancellationToken);
        _logger.LogInformation("Email notification sent to {Email} for purpose {Purpose}", email, purpose);
    }

    private async Task HandleSmsNotification(NotificationSendEvent @event, CancellationToken cancellationToken)
    {
        if (!@event.Data.TryGetValue("phone", out var phoneObj) || phoneObj is null)
        {
            _logger.LogWarning("NotificationSendEvent missing 'phone' in Data");
            return;
        }

        if (!@event.Data.TryGetValue("deviceId", out var deviceIdObj) || deviceIdObj is null)
        {
            _logger.LogWarning("NotificationSendEvent missing 'deviceId' in Data");
            return;
        }

        var phone = phoneObj.ToString() ?? string.Empty;
        var deviceId = deviceIdObj.ToString() ?? string.Empty;
        var device = await _deviceStore.GetAsync(deviceId);

        if (device == null)
        {
            _logger.LogWarning("Device not found for ID: {DeviceId}", deviceId);
            return;
        }

        var normalizedPhone = _fcmService.NormalizePhone(phone);

        // Purpose-specific OTP generation and storage
        var purposeKey = @event.Data.TryGetValue("purpose", out var p) ? (p?.ToString() ?? string.Empty) : string.Empty;
        var isForgot = purposeKey.Equals(OtpPurpose.FORGOT_PASSWORD.ToKey(), StringComparison.OrdinalIgnoreCase);
        if (isForgot)
        {
            var otp = _otpManager.GenerateNumericOtp();
            // Use a namespaced cache key to avoid cross-purpose collisions
            await _otpManager.StoreOtpAsync($"purpose:{purposeKey}:phone:{normalizedPhone}", otp, TimeSpan.FromMinutes(5));
            // Overwrite message with OTP content if not provided
            if (string.IsNullOrWhiteSpace(@event.Message))
            {
                @event.Message = $"Your OTP is {otp} (valid 5 minutes) for password reset.";
            }
        }

        var data = new { phone = normalizedPhone, message = @event.Message };

        var result = await _fcmService.SendDataMessageAsync(device.Token, data);

        // Update last used timestamp
        await _deviceStore.UpdateLastUsedAsync(deviceId);

        _logger.LogInformation("SMS notification sent to {Phone} via device {DeviceId}. Result: {Result}", phone, deviceId, result);
    }
}

