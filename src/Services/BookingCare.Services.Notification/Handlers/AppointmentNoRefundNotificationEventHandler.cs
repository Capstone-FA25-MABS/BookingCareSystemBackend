using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for appointment no-refund notification events
/// Sends notification to patient about cancellation with no refund policy
/// </summary>
public class AppointmentNoRefundNotificationEventHandler
    : BaseRefundNotificationHandler,
      IIntegrationEventHandler<AppointmentNoRefundNotificationEvent>
{
    public AppointmentNoRefundNotificationEventHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ILogger<AppointmentNoRefundNotificationEventHandler> logger)
        : base(emailService, fcmService, deviceStore, logger)
    {
    }

    public async Task HandleAsync(AppointmentNoRefundNotificationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation(
                "Processing no-refund notification for appointment {AppointmentId}, PatientId: {PatientId}",
                @event.AppointmentId, @event.PatientId);

            // Use patient information from event
            var patientName = @event.PatientFullName ?? "Quý khách";

            // Build notification content for no refund case
            var emailSubject = "Lịch hẹn đã được hủy - Thông báo chính sách hoàn tiền";
            var emailContent = EmailTemplate.BuildNoRefundEmailHtml(
                patientName,
                @event.AppointmentDate,
                @event.CancellationReason);
            var smsContent = SmsTemplate.BuildNoRefundSms(@event.AppointmentDate);

            // Send Email
            await SendEmailNotificationAsync(
                @event.PatientEmail,
                emailSubject,
                emailContent,
                @event.AppointmentId.ToString(),
                @event.PatientId.ToString(),
                "no-refund cancellation",
                cancellationToken);

            // Send SMS
            await SendSmsNotificationAsync(
                @event.PatientPhone,
                smsContent,
                @event.AppointmentId.ToString(),
                @event.PatientId.ToString(),
                "no-refund cancellation");

            Logger.LogInformation(
                "Completed no-refund notification processing for appointment {AppointmentId}",
                @event.AppointmentId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error processing no-refund notification for appointment {AppointmentId}: {Error}",
                @event.AppointmentId, ex.Message);
            // Don't re-throw - notifications are best-effort
        }
    }
}
