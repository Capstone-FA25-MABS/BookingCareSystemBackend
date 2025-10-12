using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for AppointmentRefundRequestedIntegrationEvent
/// Sends email and SMS notifications to patient about refund
/// </summary>
public class AppointmentRefundRequestedEventHandler
    : BaseRefundNotificationHandler<AppointmentRefundRequestedIntegrationEvent>,
      IIntegrationEventHandler<AppointmentRefundRequestedIntegrationEvent>
{
    public AppointmentRefundRequestedEventHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ILogger<AppointmentRefundRequestedEventHandler> logger)
        : base(emailService, fcmService, deviceStore, logger)
    {
    }

    public async Task HandleAsync(AppointmentRefundRequestedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation(
                "Processing refund notification for RefundHistoryId: {RefundHistoryId}, PatientId: {PatientId}",
                @event.RefundHistoryId, @event.PatientId);

            // Determine notification content based on bank account status
            string emailSubject;
            string emailContent;
            string? smsContent;

            var patientName = @event.PatientFullName ?? "Quý khách";

            if (@event.HasBankAccount)
            {
                // Patient has bank account → refund is PENDING
                emailSubject = "Lịch hẹn đã được hủy - Xác nhận hoàn tiền";
                emailContent = EmailTemplate.BuildRefundEmailWithBankAccountHtml(
                    patientName,
                    @event.AppointmentDate,
                    @event.CancellationReason,
                    @event.RefundAmount);
                smsContent = SmsTemplate.BuildRefundSmsWithBankAccount(
                    @event.AppointmentDate,
                    @event.RefundAmount);
            }
            else
            {
                // Patient has NO bank account → refund is WAITING
                emailSubject = "Lịch hẹn đã được hủy - Cần cung cấp thông tin tài khoản";
                emailContent = EmailTemplate.BuildRefundEmailNoBankAccountHtml(
                    patientName,
                    @event.AppointmentDate,
                    @event.CancellationReason,
                    @event.RefundAmount);
                smsContent = SmsTemplate.BuildRefundSmsNoBankAccount(
                    @event.AppointmentDate,
                    @event.RefundAmount);
            }

            // Send Email
            await SendEmailNotificationAsync(
                @event.PatientEmail,
                emailSubject,
                emailContent,
                @event.RefundHistoryId.ToString(),
                @event.PatientId.ToString(),
                "refund",
                cancellationToken);

            // Send SMS
            if (!string.IsNullOrWhiteSpace(smsContent))
            {
                await SendSmsNotificationAsync(
                    @event.PatientPhone,
                    smsContent,
                    @event.RefundHistoryId.ToString(),
                    @event.PatientId.ToString(),
                    "refund");
            }

            Logger.LogInformation(
                "Completed notification processing for refund {RefundHistoryId}",
                @event.RefundHistoryId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error processing refund notification for RefundHistoryId {RefundHistoryId}: {Error}",
                @event.RefundHistoryId, ex.Message);
            // Don't re-throw - notifications are best-effort
        }
    }

}

