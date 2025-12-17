using BookingCare.Services.Notification.Helpers;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Event handler for processing appointment reminders
/// Sends notifications via 3 channels:
/// - SMS (via FCM push notification to SMS gateway)
/// - Email
/// - In-App Notification (via CreateInAppNotificationEvent which handles SignalR)
/// </summary>
public class AppointmentReminderEventHandler : IIntegrationEventHandler<AppointmentReminderEvent>
{
    private readonly EmailService _emailService;
    private readonly FcmV1Service _fcmService;
    private readonly DeviceStore _deviceStore;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AppointmentReminderEventHandler> _logger;

    public AppointmentReminderEventHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        IEventBus eventBus,
        ILogger<AppointmentReminderEventHandler> logger)
    {
        _emailService = emailService;
        _fcmService = fcmService;
        _deviceStore = deviceStore;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(
        AppointmentReminderEvent @event,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NotificationService] Received AppointmentReminderEvent - " +
            "AppointmentId: {AppointmentId}, ReminderType: {ReminderType}, PatientEmail: {Email}",
            @event.AppointmentId, @event.ReminderType, @event.PatientEmail);

        try
        {
            // Send notifications via all 3 channels in parallel
            var tasks = new List<Task>
            {
                SendSmsNotificationAsync(@event),
                SendEmailNotificationAsync(@event, cancellationToken),
                SendInAppNotificationAsync(@event, cancellationToken)
            };

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "[NotificationService] Successfully sent all reminder notifications - " +
                "AppointmentId: {AppointmentId}",
                @event.AppointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NotificationService] Error processing appointment reminder - " +
                "AppointmentId: {AppointmentId}",
                @event.AppointmentId);
        }
    }

    /// <summary>
    /// Send SMS notification via FCM to SMS gateway device
    /// </summary>
    private async Task SendSmsNotificationAsync(AppointmentReminderEvent @event)
    {
        if (string.IsNullOrWhiteSpace(@event.PatientPhone))
        {
            _logger.LogWarning(
                "[SMS] Patient phone is empty - AppointmentId: {AppointmentId}",
                @event.AppointmentId);
            return;
        }

        try
        {
            var normalizedPhone = FcmV1Service.NormalizePhone(@event.PatientPhone);
            var smsContent = SmsTemplate.BuildSmsContent(@event);

            var device = await _deviceStore.GetFirstActiveDeviceAsync();

            if (device != null)
            {
                try
                {
                    var data = new { phone = normalizedPhone, message = smsContent };
                    var result = await _fcmService.SendDataMessageAsync(device.Token, data);

                    await _deviceStore.UpdateLastUsedAsync(device.Id);

                    _logger.LogInformation(
                        "[SMS] Sent reminder SMS for appointment {AppointmentId} to {Phone} via device {DeviceId}. Result: {Result}",
                        @event.AppointmentId, normalizedPhone, device.Id, result);
                }
                catch (Exception fcmEx)
                {
                    _logger.LogError(fcmEx,
                        "[SMS] Failed to send SMS via FCM for appointment {AppointmentId} to {Phone}",
                        @event.AppointmentId, normalizedPhone);
                }
            }
            else
            {
                _logger.LogWarning(
                    "[SMS] No active SMS gateway device available for appointment {AppointmentId}",
                    @event.AppointmentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[SMS] Error processing SMS notification for appointment {AppointmentId}",
                @event.AppointmentId);
        }
    }

    /// <summary>
    /// Send email notification
    /// </summary>
    private async Task SendEmailNotificationAsync(
        AppointmentReminderEvent @event,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(@event.PatientEmail))
        {
            _logger.LogWarning(
                "[Email] Patient email is empty - AppointmentId: {AppointmentId}",
                @event.AppointmentId);
            return;
        }

        try
        {
            var subject = GetEmailSubject(@event);
            var content = EmailTemplate.BuildReminderEmailHtml(@event);

            await _emailService.SendEmailAsync(
                toEmail: @event.PatientEmail,
                subject: subject,
                content: content,
                isHtml: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[Email] Sent reminder email to {Email} for appointment {AppointmentId}",
                @event.PatientEmail, @event.AppointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Email] Failed to send reminder email - Email: {Email}, AppointmentId: {AppointmentId}",
                @event.PatientEmail, @event.AppointmentId);
        }
    }

    /// <summary>
    /// Create in-app notification via event bus (handles SignalR internally)
    /// </summary>
    private async Task SendInAppNotificationAsync(
        AppointmentReminderEvent @event,
        CancellationToken cancellationToken)
    {
        try
        {
            var formattedDateVi = DateTimeHelper.FormatAppointmentDateVi(@event.AppointmentDate);
            var formattedDateEn = DateTimeHelper.FormatAppointmentDateEn(@event.AppointmentDate);
            var timeDisplay = @event.ReminderType == "24_HOURS" ? "24 giờ" : "1 giờ";
            var timeDisplayEn = @event.ReminderType == "24_HOURS" ? "24 hours" : "1 hour";

            var notificationEvent = new CreateInAppNotificationEvent
            {
                UserId = @event.PatientAccountId.ToString(),
                Type = NotificationType.BookingReminder,
                Content = new NotificationContent
                {
                    TitleVi = $"Nhắc lịch hẹn - Còn {timeDisplay}",
                    TitleEn = $"Appointment Reminder - {timeDisplayEn} left",
                    ContentVi = BuildContentVi(@event, formattedDateVi),
                    ContentEn = BuildContentEn(@event, formattedDateEn),
                    Metadata = new Dictionary<string, object>
                    {
                        { "appointmentId", @event.AppointmentId.ToString() },
                        { "reminderType", @event.ReminderType },
                        { "appointmentDate", @event.AppointmentDate.ToString("O") },
                        { "appointmentTime", @event.AppointmentTime },
                        { "hospitalName", @event.HospitalName }
                    },
                    ActionUrl = $"/user/profile?tab=appointments&id={@event.AppointmentId}",
                    Icon = "isax isax-clock",
                    Priority = @event.ReminderType == "1_HOUR"
                        ? NotificationPriority.Urgent
                        : NotificationPriority.High,
                    ExpirationDays = 1
                }
            };

            await _eventBus.PublishAsync(notificationEvent, null, cancellationToken);

            _logger.LogInformation(
                "[InApp] Published in-app notification for appointment {AppointmentId}",
                @event.AppointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[InApp] Failed to create in-app notification - AppointmentId: {AppointmentId}",
                @event.AppointmentId);
        }
    }

    #region Helper Methods

    private static string GetEmailSubject(AppointmentReminderEvent @event)
    {
        var timeText = @event.ReminderType == "24_HOURS" ? "24 giờ" : "1 giờ";
        return $"[MedCure] Nhắc lịch hẹn - Còn {timeText} nữa";
    }

    private static string BuildContentVi(AppointmentReminderEvent @event, string formattedDate)
    {
        var doctorInfo = !string.IsNullOrEmpty(@event.DoctorName)
            ? $" với BS. {@event.DoctorName}"
            : "";

        return $"Lịch hẹn khám{doctorInfo} tại {@event.HospitalName} " +
               $"vào {formattedDate} lúc {@event.AppointmentTime} sắp đến. " +
               "Vui lòng đến đúng giờ.";
    }

    private static string BuildContentEn(AppointmentReminderEvent @event, string formattedDate)
    {
        var doctorInfo = !string.IsNullOrEmpty(@event.DoctorName)
            ? $" with Dr. {@event.DoctorName}"
            : "";

        return $"Your appointment{doctorInfo} at {@event.HospitalName} " +
               $"on {formattedDate} at {@event.AppointmentTime} is coming up. " +
               "Please arrive on time.";
    }

    #endregion
}
