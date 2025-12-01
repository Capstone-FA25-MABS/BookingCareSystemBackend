using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Helpers;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Event handler for processing auto-cancelled appointment notifications
/// when hospital fails to assign a doctor before the appointment date
/// - Sends email notification to patient
/// - Creates in-app notification via CreateInAppNotificationEvent for real-time updates
/// </summary>
public class AppointmentAutoCancelledDueToNoDoctorEventHandler : IIntegrationEventHandler<AppointmentAutoCancelledDueToNoDoctorEvent>
{
    private readonly EmailService _emailService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AppointmentAutoCancelledDueToNoDoctorEventHandler> _logger;

    public AppointmentAutoCancelledDueToNoDoctorEventHandler(
        EmailService emailService,
        IEventBus eventBus,
        ILogger<AppointmentAutoCancelledDueToNoDoctorEventHandler> logger)
    {
        _emailService = emailService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(AppointmentAutoCancelledDueToNoDoctorEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NotificationService] Received AppointmentAutoCancelledDueToNoDoctorEvent - AppointmentId: {AppointmentId}, PatientId: {PatientId}",
            @event.AppointmentId, @event.PatientId);

        try
        {
            // Validate required fields
            if (string.IsNullOrEmpty(@event.PatientEmail))
            {
                _logger.LogWarning(
                    "[NotificationService] Patient email is empty for auto-cancelled notification - AppointmentId: {AppointmentId}",
                    @event.AppointmentId);
                return;
            }

            // Format appointment date
            var formattedDateVi = DateTimeHelper.FormatAppointmentDateVi(@event.AppointmentDate);
            var formattedDateEn = DateTimeHelper.FormatAppointmentDateEn(@event.AppointmentDate);
            var patientName = string.IsNullOrEmpty(@event.PatientFullName) ? "Quý khách" : @event.PatientFullName;
            var hospitalName = string.IsNullOrEmpty(@event.HospitalName) ? "bệnh viện" : @event.HospitalName;
            var specialtyName = string.IsNullOrEmpty(@event.SpecialtyName) ? "chuyên khoa" : @event.SpecialtyName;

            // Build email content
            var emailSubject = $"[BookingCare] Thông báo hủy lịch hẹn - Bệnh viện không gán bác sĩ";
            var emailContent = EmailTemplate.BuildAutoCancelledEmailHtml(
                patientName,
                hospitalName,
                specialtyName,
                formattedDateVi,
                @event.AppointmentTime);

            // Send the email
            await _emailService.SendEmailAsync(
                toEmail: @event.PatientEmail,
                subject: emailSubject,
                content: emailContent,
                isHtml: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[NotificationService] Successfully sent auto-cancelled appointment email - AppointmentId: {AppointmentId}, PatientEmail: {PatientEmail}",
                @event.AppointmentId, @event.PatientEmail);

            // Publish in-app notification event for real-time notification
            if (@event.PatientAccountId != Guid.Empty)
            {
                var notificationEvent = new CreateInAppNotificationEvent
                {
                    UserId = @event.PatientAccountId.ToString(),
                    Type = NotificationType.BookingCancellation,
                    Content = new NotificationContent
                    {
                        TitleVi = "Lịch hẹn đã bị hủy",
                        TitleEn = "Appointment Cancelled",
                        ContentVi = $"Lịch hẹn khám {specialtyName} tại {hospitalName} vào {formattedDateVi} lúc {@event.AppointmentTime} đã bị hủy do bệnh viện không gán bác sĩ trước ngày hẹn. Chúng tôi thành thật xin lỗi vì sự bất tiện này.",
                        ContentEn = $"Your {specialtyName} appointment at {hospitalName} on {formattedDateEn} at {@event.AppointmentTime} has been cancelled because the hospital did not assign a doctor before the appointment date. We sincerely apologize for this inconvenience.",
                        Metadata = new Dictionary<string, object>
                        {
                            { "appointmentId", @event.AppointmentId.ToString() },
                            { "hospitalName", hospitalName },
                            { "specialtyName", specialtyName },
                            { "appointmentDate", @event.AppointmentDate.ToString("O") },
                            { "appointmentTime", @event.AppointmentTime },
                            { "cancellationReason", "no_doctor_assigned" },
                            { "notificationType", "auto_cancelled_no_doctor" }
                        },
                        ActionUrl = $"/user/profile?tab=appointments",
                        Icon = "isax isax-close-circle",
                        Priority = NotificationPriority.High,
                        ExpirationDays = 30
                    }
                };

                await _eventBus.PublishAsync(notificationEvent, null, cancellationToken);

                _logger.LogInformation(
                    "[NotificationService] Successfully published in-app notification for auto-cancelled appointment - AppointmentId: {AppointmentId}, UserId: {UserId}",
                    @event.AppointmentId, @event.PatientAccountId);
            }
            else
            {
                _logger.LogWarning(
                    "[NotificationService] PatientAccountId is not available, skipping in-app notification - AppointmentId: {AppointmentId}",
                    @event.AppointmentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NotificationService] Failed to process auto-cancelled appointment notification - AppointmentId: {AppointmentId}, PatientEmail: {PatientEmail}",
                @event.AppointmentId, @event.PatientEmail);

            // Don't re-throw to avoid breaking the event processing pipeline
        }
    }

}
