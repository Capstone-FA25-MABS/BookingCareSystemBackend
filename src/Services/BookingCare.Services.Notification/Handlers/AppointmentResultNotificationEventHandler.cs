using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Event handler for processing appointment result notifications
/// - Sends email to patient when appointment result is updated
/// - Creates in-app notification via CreateInAppNotificationEvent
/// </summary>
public class AppointmentResultNotificationEventHandler
    : IIntegrationEventHandler<AppointmentResultUpdatedEvent>
{
    private readonly EmailService _emailService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AppointmentResultNotificationEventHandler> _logger;

    public AppointmentResultNotificationEventHandler(
        EmailService emailService,
        IEventBus eventBus,
        ILogger<AppointmentResultNotificationEventHandler> logger
    )
    {
        _emailService = emailService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(
        AppointmentResultUpdatedEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "[NotificationService] Received AppointmentResultUpdatedEvent - AppointmentId: {AppointmentId}, PatientId: {PatientId}, PatientEmail: {PatientEmail}",
            @event.AppointmentId,
            @event.PatientId,
            @event.PatientEmail
        );

        try
        {
            // Validate required fields
            if (string.IsNullOrEmpty(@event.PatientEmail))
            {
                _logger.LogWarning(
                    "[NotificationService] Patient email is empty for appointment result notification - AppointmentId: {AppointmentId}",
                    @event.AppointmentId
                );
                return;
            }

            if (string.IsNullOrEmpty(@event.ResultUrl))
            {
                _logger.LogWarning(
                    "[NotificationService] Result URL is empty for appointment result notification - AppointmentId: {AppointmentId}",
                    @event.AppointmentId
                );
                return;
            }

            // Handle empty patient name with fallback
            var patientName = string.IsNullOrEmpty(@event.PatientName)
                ? "Quý khách"
                : @event.PatientName;

            // Generate HTML email content using the template
            var emailContent = EmailTemplate.BuildAppointmentResultEmailHtml(
                patientName: patientName,
                appointmentDate: @event.AppointmentDate,
                appointmentTime: @event.AppointmentTime,
                resultUrl: @event.ResultUrl,
                doctorName: @event.DoctorName,
                hospitalName: @event.HospitalName
            );

            // Send the email
            await _emailService.SendEmailAsync(
                toEmail: @event.PatientEmail,
                subject: "Kết quả khám bệnh - BookingCare",
                content: emailContent,
                isHtml: true,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "[NotificationService] Successfully sent appointment result email - AppointmentId: {AppointmentId}, PatientEmail: {PatientEmail}",
                @event.AppointmentId,
                @event.PatientEmail
            );

            // Create in-app notification (bilingual)
            var formattedDateVi = @event.AppointmentDate.ToString("dd/MM/yyyy");
            var formattedDateEn = @event.AppointmentDate.ToString("MM/dd/yyyy");
            var doctorDisplay = !string.IsNullOrEmpty(@event.DoctorName)
                ? @event.DoctorName
                : "bác sĩ";

            var notificationEvent = new CreateInAppNotificationEvent
            {
                UserId = @event.PatientId.ToString(),
                Type = NotificationType.General,
                Content = new NotificationContent
                {
                    TitleVi = "Kết quả khám bệnh đã sẵn sàng",
                    TitleEn = "Medical Result Available",
                    ContentVi =
                        $"Kết quả khám của bạn với {doctorDisplay} vào ngày {formattedDateVi} đã được cập nhật. Nhấp để xem chi tiết.",
                    ContentEn =
                        $"Your medical result from {doctorDisplay} on {formattedDateEn} is now available. Click to view details.",
                    Metadata = new Dictionary<string, object>
                    {
                        { "appointmentId", @event.AppointmentId.ToString() },
                        { "resultUrl", @event.ResultUrl },
                        { "doctorName", doctorDisplay },
                        { "appointmentDate", @event.AppointmentDate.ToString("O") },
                        { "appointmentTime", @event.AppointmentTime },
                    },
                    ActionUrl = $"/user/profile?tab=appointments&id={@event.AppointmentId}",
                    Icon = "isax isax-document-text",
                    Priority = NotificationPriority.High,
                    ExpirationDays = 365, // Keep result notification for 1 year
                },
            };

            await _eventBus.PublishAsync(notificationEvent);

            _logger.LogInformation(
                "[NotificationService] Successfully published CreateInAppNotificationEvent for appointment result - AppointmentId: {AppointmentId}",
                @event.AppointmentId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[NotificationService] Failed to handle appointment result notification - AppointmentId: {AppointmentId}, Error: {Error}",
                @event.AppointmentId,
                ex.Message
            );
            throw new InvalidOperationException(
                $"Failed to handle appointment result notification for AppointmentId: {@event.AppointmentId}",
                ex
            );
        }
    }
}
