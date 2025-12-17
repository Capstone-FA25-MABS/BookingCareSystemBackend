using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for appointment rejection notification events
/// Sends notification to patient when hospital staff rejects a pending appointment (before payment)
/// </summary>
public class AppointmentRejectedNotificationEventHandler
    : BaseRefundNotificationHandler,
      IIntegrationEventHandler<AppointmentRejectedNotificationEvent>
{
    public AppointmentRejectedNotificationEventHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ILogger<AppointmentRejectedNotificationEventHandler> logger)
        : base(emailService, fcmService, deviceStore, logger)
    {
    }

    public async Task HandleAsync(
        AppointmentRejectedNotificationEvent @event,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation(
                "Processing rejection notification for appointment {AppointmentId}, PatientId: {PatientId}",
                @event.AppointmentId,
                @event.PatientId);

            var patientName = @event.PatientFullName ?? "Quý khách";
            var appointmentTime = @event.AppointmentTimeId;

            // Build email content
            var emailSubject = "Thông báo từ chối lịch hẹn - MedCure";
            var emailContent = EmailTemplate.BuildAppointmentRejectedEmailHtml(
                patientName,
                @event.AppointmentDate,
                appointmentTime,
                @event.RejectionReason,
                @event.DoctorName,
                @event.HospitalName);

            // Build SMS content
            var smsContent = SmsTemplate.BuildSmsContent(@event, appointmentTime);

            // Send Email
            await SendEmailNotificationAsync(
                @event.PatientEmail,
                emailSubject,
                emailContent,
                @event.AppointmentId.ToString(),
                @event.PatientId.ToString(),
                "appointment rejection",
                cancellationToken);

            // Send SMS
            await SendSmsNotificationAsync(
                @event.PatientPhone,
                smsContent,
                @event.AppointmentId.ToString(),
                @event.PatientId.ToString(),
                "appointment rejection");

            Logger.LogInformation(
                "Completed rejection notification processing for appointment {AppointmentId}",
                @event.AppointmentId);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Error processing rejection notification for appointment {AppointmentId}: {Error}",
                @event.AppointmentId,
                ex.Message);
            // Don't re-throw - notifications are best-effort
        }
    }

}
