using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Models.DTOs;
using System.Globalization;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Event handler for processing appointment booking success notifications
/// - Sends success email to patient when appointment is booked and payment is completed
/// - Creates in-app notification via CreateInAppNotificationEvent
/// </summary>
public class AppointmentBookingSuccessNotificationEventHandler : IIntegrationEventHandler<AppointmentBookingSuccessNotificationEvent>
{
    private readonly EmailService _emailService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AppointmentBookingSuccessNotificationEventHandler> _logger;

    public AppointmentBookingSuccessNotificationEventHandler(
        EmailService emailService,
        IEventBus eventBus,
        ILogger<AppointmentBookingSuccessNotificationEventHandler> logger)
    {
        _emailService = emailService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(AppointmentBookingSuccessNotificationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NotificationService] Received AppointmentBookingSuccessNotificationEvent - AppointmentId: {AppointmentId}, PatientId: {PatientId}, PatientEmail: {PatientEmail}, CorrelationId: {CorrelationId}",
            @event.AppointmentId, @event.PatientId, @event.PatientEmail, @event.CorrelationId);

        try
        {
            // Validate required fields
            if (string.IsNullOrEmpty(@event.PatientEmail))
            {
                _logger.LogWarning(
                    "[NotificationService] Patient email is empty for appointment booking success notification - AppointmentId: {AppointmentId}",
                    @event.AppointmentId);
                return;
            }

            // Handle empty patient name with fallback
            if (string.IsNullOrEmpty(@event.AppointmentData.PatientName))
            {
                @event.AppointmentData.PatientName = "Qu� kh�ch"; // Fixed Unicode fallback
            }

            // Create email data using direct AppointmentData assignment (eliminates duplication)
            var emailData = AppointmentBookingEmailData.FromEvent(@event);

            // Generate HTML email content using the template with DTO
            var emailContent = EmailTemplate.BuildAppointmentBookedSuccessEmailHtml(emailData);

            // Send the email
            await _emailService.SendEmailAsync(
                toEmail: @event.PatientEmail,
                subject: @event.EmailSubject,
                content: emailContent,
                isHtml: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[NotificationService] Successfully sent appointment booking success email - AppointmentId: {AppointmentId}, PatientEmail: {PatientEmail}",
                @event.AppointmentId, @event.PatientEmail);

            // Publish generic notification event for creating in-app notification (bilingual)
            var doctorName = @event.AppointmentData?.DoctorName ?? "bác sĩ";
            var appointmentTime = @event.AppointmentData?.AppointmentTime ?? "thời gian đã chọn";
            var formattedDateVi = FormatAppointmentDate(@event.AppointmentData?.AppointmentDate);
            var formattedDateEn = FormatAppointmentDateEn(@event.AppointmentData?.AppointmentDate);

            var notificationEvent = new CreateInAppNotificationEvent
            {
                UserId = @event.AccountId, // Use AccountId from event instead of PatientId
                Type = NotificationType.BookingConfirmation,
                Content = new NotificationContent
                {
                    TitleVi = "Đặt lịch khám thành công",
                    TitleEn = "Appointment Confirmed",
                    ContentVi = $"Lịch khám của bạn với {doctorName} vào {formattedDateVi} lúc {appointmentTime} đã được xác nhận.",
                    ContentEn = $"Your appointment with {doctorName} on {formattedDateEn} at {appointmentTime} has been confirmed.",
                    Metadata = new Dictionary<string, object>
                    {
                        { "appointmentId", @event.AppointmentId.ToString() },
                        { "doctorName", doctorName },
                        { "appointmentDate", @event.AppointmentData?.AppointmentDate.ToString("O") ?? "" },
                        { "appointmentTime", @event.AppointmentData?.AppointmentTime ?? "" }
                    },
                    ActionUrl = $"/user/profile?tab=appointments&id={@event.AppointmentId}",
                    Icon = "isax isax-calendar-tick",
                    Priority = NotificationPriority.High,
                    ExpirationDays = 90
                }
            };

            await _eventBus.PublishAsync(notificationEvent, null, cancellationToken);

            _logger.LogInformation(
                "[NotificationService] Successfully published in-app notification event - AppointmentId: {AppointmentId}",
                @event.AppointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NotificationService] Failed to process appointment booking success notification - AppointmentId: {AppointmentId}, PatientEmail: {PatientEmail}",
                @event.AppointmentId, @event.PatientEmail);

            // Don't re-throw to avoid breaking the event processing pipeline
            // The error is already logged for investigation
        }
    }

    private static string FormatAppointmentDate(DateTime? date)
    {
        if (!date.HasValue) return "ngày đã chọn";

        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(date.Value, vietnamTimeZone);

        return vietnamTime.ToString("'ngày' dd/MM/yyyy");
    }

    private static string FormatAppointmentDateEn(DateTime? date)
    {
        if (!date.HasValue) return "your selected date";

        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(date.Value, vietnamTimeZone);

        return vietnamTime.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);
    }
}