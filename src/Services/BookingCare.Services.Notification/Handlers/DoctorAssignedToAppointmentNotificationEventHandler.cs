using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
using BookingCare.Services.Notification.Utils.Email;
using System.Globalization;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Event handler for processing doctor assigned to appointment notifications
/// - Sends email notification to patient when hospital staff assigns a doctor
/// - Creates in-app notification via CreateInAppNotificationEvent for real-time updates
/// </summary>
public class DoctorAssignedToAppointmentNotificationEventHandler : IIntegrationEventHandler<DoctorAssignedToAppointmentNotificationEvent>
{
    private readonly EmailService _emailService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<DoctorAssignedToAppointmentNotificationEventHandler> _logger;

    public DoctorAssignedToAppointmentNotificationEventHandler(
        EmailService emailService,
        IEventBus eventBus,
        ILogger<DoctorAssignedToAppointmentNotificationEventHandler> logger)
    {
        _emailService = emailService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(DoctorAssignedToAppointmentNotificationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NotificationService] Received DoctorAssignedToAppointmentNotificationEvent - AppointmentId: {AppointmentId}, PatientId: {PatientId}, DoctorId: {DoctorId}",
            @event.AppointmentId, @event.PatientId, @event.AssignedDoctorId);

        try
        {
            // Validate required fields
            if (string.IsNullOrEmpty(@event.PatientEmail))
            {
                _logger.LogWarning(
                    "[NotificationService] Patient email is empty for doctor assigned notification - AppointmentId: {AppointmentId}",
                    @event.AppointmentId);
                return;
            }

            // Format appointment date and time
            var formattedDateVi = FormatAppointmentDateVi(@event.AppointmentDate);
            var formattedDateEn = FormatAppointmentDateEn(@event.AppointmentDate);
            var patientName = string.IsNullOrEmpty(@event.PatientFullName) ? "Quý khách" : @event.PatientFullName;
            var doctorName = string.IsNullOrEmpty(@event.DoctorFullName) ? "bác sĩ" : @event.DoctorFullName;
            var hospitalName = string.IsNullOrEmpty(@event.HospitalName) ? "bệnh viện" : @event.HospitalName;
            var specialty = @event.DoctorSpecialty ?? "chuyên khoa";

            // Build email content
            var emailSubject = $"[BookingCare] Bác sĩ đã được gán cho lịch hẹn của bạn";
            var emailContent = EmailTemplate.BuildDoctorAssignedEmailHtml(
                patientName,
                doctorName,
                specialty,
                hospitalName,
                formattedDateVi,
                @event.AppointmentTime,
                @event.StaffNote);

            // Send the email
            await _emailService.SendEmailAsync(
                toEmail: @event.PatientEmail,
                subject: emailSubject,
                content: emailContent,
                isHtml: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[NotificationService] Successfully sent doctor assigned email - AppointmentId: {AppointmentId}, PatientEmail: {PatientEmail}",
                @event.AppointmentId, @event.PatientEmail);

            // Publish in-app notification event for real-time notification
            // Only if PatientAccountId is available (for logged-in users)
            if (@event.PatientAccountId != Guid.Empty)
            {
                var notificationEvent = new CreateInAppNotificationEvent
                {
                    UserId = @event.PatientAccountId.ToString(),
                    Type = NotificationType.BookingConfirmation,
                    Content = new NotificationContent
                    {
                        TitleVi = "Bác sĩ đã được gán cho lịch hẹn",
                        TitleEn = "Doctor Assigned to Your Appointment",
                        ContentVi = $"Bác sĩ {doctorName} ({specialty}) đã được gán cho lịch hẹn của bạn vào {formattedDateVi} lúc {@event.AppointmentTime} tại {hospitalName}.",
                        ContentEn = $"Dr. {doctorName} ({specialty}) has been assigned to your appointment on {formattedDateEn} at {@event.AppointmentTime} at {hospitalName}.",
                        Metadata = new Dictionary<string, object>
                        {
                            { "appointmentId", @event.AppointmentId.ToString() },
                            { "doctorId", @event.AssignedDoctorId.ToString() },
                            { "doctorName", doctorName },
                            { "specialty", specialty },
                            { "hospitalName", hospitalName },
                            { "appointmentDate", @event.AppointmentDate.ToString("O") },
                            { "appointmentTime", @event.AppointmentTime },
                            { "notificationType", "doctor_assigned" }
                        },
                        ActionUrl = $"/user/profile?tab=appointments&id={@event.AppointmentId}",
                        Icon = "isax isax-user-tick",
                        Priority = NotificationPriority.High,
                        ExpirationDays = 30
                    }
                };

                await _eventBus.PublishAsync(notificationEvent, null, cancellationToken);

                _logger.LogInformation(
                    "[NotificationService] Successfully published in-app notification for doctor assigned - AppointmentId: {AppointmentId}, UserId: {UserId}",
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
                "[NotificationService] Failed to process doctor assigned notification - AppointmentId: {AppointmentId}, PatientEmail: {PatientEmail}",
                @event.AppointmentId, @event.PatientEmail);

            // Don't re-throw to avoid breaking the event processing pipeline
        }
    }

    private static string FormatAppointmentDateVi(DateTime date)
    {
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(date, vietnamTimeZone);
        return vietnamTime.ToString("'ngày' dd/MM/yyyy");
    }

    private static string FormatAppointmentDateEn(DateTime date)
    {
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(date, vietnamTimeZone);
        return vietnamTime.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);
    }

}
