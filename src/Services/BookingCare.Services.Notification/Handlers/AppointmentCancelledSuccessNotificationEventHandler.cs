using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for appointment cancellation success notification events
/// Sends notification to patient about successful cancellation (without payment)
/// Used when appointment is eligible for refund but no payment record exists
/// </summary>
public class AppointmentCancelledSuccessNotificationEventHandler
    : BaseRefundNotificationHandler,
      IIntegrationEventHandler<AppointmentCancelledSuccessNotificationEvent>
{
    public AppointmentCancelledSuccessNotificationEventHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ILogger<AppointmentCancelledSuccessNotificationEventHandler> logger)
        : base(emailService, fcmService, deviceStore, logger)
    {
    }

    public async Task HandleAsync(AppointmentCancelledSuccessNotificationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation(
                "Processing cancellation success notification for appointment {AppointmentId}, PatientId: {PatientId}",
                @event.AppointmentId, @event.PatientId);

            // Use patient information from event
            var patientName = @event.PatientFullName ?? "Quý khách";

            // Build notification content for cancellation success
            var emailSubject = "Hủy lịch hẹn thành công - MedCure";
            var emailContent = EmailTemplate.BuildCancellationSuccessEmailHtml(
                patientName,
                @event.AppointmentDate,
                @event.CancellationReason,
                @event.DoctorName,
                @event.HospitalName);

            var smsContent = $"[MedCure] Lịch hẹn ngày {@event.AppointmentDate:dd/MM/yyyy HH:mm} của bạn đã được hủy thành công. Cảm ơn bạn đã sử dụng dịch vụ.";

            // Send Email
            await SendEmailNotificationAsync(
                @event.PatientEmail,
                emailSubject,
                emailContent,
                @event.AppointmentId.ToString(),
                @event.PatientId.ToString(),
                "cancellation success",
                cancellationToken);

            // Send SMS
            await SendSmsNotificationAsync(
                @event.PatientPhone,
                smsContent,
                @event.AppointmentId.ToString(),
                @event.PatientId.ToString(),
                "cancellation success");

            Logger.LogInformation(
                "Completed cancellation success notification processing for appointment {AppointmentId}",
                @event.AppointmentId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error processing cancellation success notification for appointment {AppointmentId}: {Error}",
                @event.AppointmentId, ex.Message);
            // Don't re-throw - notifications are best-effort
        }
    }
}

