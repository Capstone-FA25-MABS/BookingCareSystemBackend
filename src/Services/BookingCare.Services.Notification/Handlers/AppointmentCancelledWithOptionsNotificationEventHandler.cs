using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Models.DTOs;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for staff-initiated appointment cancellation with reschedule options
/// Sends notification with 4 options for patient to choose
/// This is a NOTIFICATION-ONLY handler - does NOT trigger refund processing
/// </summary>
public class AppointmentCancelledWithOptionsNotificationEventHandler
    : BaseRefundNotificationHandler,
      IIntegrationEventHandler<AppointmentCancelledWithOptionsNotificationEvent>
{
    public AppointmentCancelledWithOptionsNotificationEventHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ILogger<AppointmentCancelledWithOptionsNotificationEventHandler> logger)
        : base(emailService, fcmService, deviceStore, logger)
    {
    }

    public async Task HandleAsync(AppointmentCancelledWithOptionsNotificationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation(
                "Processing staff cancellation WITH OPTIONS notification for appointment {AppointmentId}, PatientId: {PatientId}",
                @event.AppointmentId, @event.PatientId);

            // Use patient information from event
            var patientName = @event.PatientFullName ?? "Quý khách";

            // Build notification content with 4 reschedule options
            var emailSubject = "Lịch hẹn đã được hủy - Vui lòng chọn phương án xử lý";
            var emailContent = EmailTemplate.BuildCancellationWithOptionsEmailHtml(new CancellationWithOptionsEmailData
            {
                PatientName = patientName,
                AppointmentDate = @event.AppointmentDate,
                CancellationReason = @event.CancellationReason,
                DoctorName = @event.DoctorName,
                HospitalName = @event.HospitalName,
                PotentialRefundAmount = @event.PotentialRefundAmount,
                PotentialRefundPercentage = @event.PotentialRefundPercentage,
                SameDoctorRescheduleUrl = @event.SameDoctorRescheduleUrl,
                ConfirmNewDoctorUrl = @event.ConfirmNewDoctorUrl,
                ChooseNewDoctorUrl = @event.ChooseNewDoctorUrl,
                RefundRequestUrl = @event.RefundRequestUrl,
                TokenExpiry = @event.RescheduleTokenExpiry
            });

            var smsContent = SmsTemplate.BuildCancellationWithOptionsSms(
                @event.AppointmentDate,
                @event.ChooseNewDoctorUrl ?? @event.RefundRequestUrl);

            // Send Email
            await SendEmailNotificationAsync(
                @event.PatientEmail,
                emailSubject,
                emailContent,
                @event.AppointmentId.ToString(),
                @event.PatientId.ToString(),
                "staff cancellation with options",
                cancellationToken);

            // Send SMS
            await SendSmsNotificationAsync(
                @event.PatientPhone,
                smsContent,
                @event.AppointmentId.ToString(),
                @event.PatientId.ToString(),
                "staff cancellation with options");

            Logger.LogInformation(
                "Completed staff cancellation WITH OPTIONS notification for appointment {AppointmentId}",
                @event.AppointmentId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error processing staff cancellation WITH OPTIONS notification for appointment {AppointmentId}: {Error}",
                @event.AppointmentId, ex.Message);
            // Don't re-throw - notifications are best-effort
        }
    }
}

