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
    : BaseRefundNotificationHandler,
      IIntegrationEventHandler<AppointmentRefundRequestedIntegrationEvent>
{
    private const string DefaultOldDoctorLabel = "Bác sĩ cũ";
    private const string DefaultNewDoctorLabel = "Bác sĩ mới";

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

            // Determine notification content based on cancellation source and bank account status
            string emailSubject;
            string emailContent;
            string? smsContent;

            var patientName = @event.PatientFullName ?? "Quý khách";

            // Check if this is a doctor change refund (Option 3: Choose new doctor with lower price)
            if (@event.CancellationSource == "DOCTOR_CHANGE_REFUND")
            {
                // Doctor change refund - special template
                emailSubject = "Xác nhận thay đổi bác sĩ - Hoàn tiền chênh lệch";

                if (@event.HasBankAccount)
                {
                    emailContent = EmailTemplate.BuildDoctorChangeRefundEmailWithBankAccountHtml(
                        patientName,
                        @event.AppointmentDate,
                        @event.OriginalDoctorName ?? DefaultOldDoctorLabel,
                        @event.OriginalConsultationFee ?? 0,
                        @event.NewDoctorName ?? DefaultNewDoctorLabel,
                        @event.NewConsultationFee ?? 0,
                        @event.RefundAmount);
                    smsContent = SmsTemplate.BuildDoctorChangeRefundSms(
                        @event.OriginalDoctorName ?? DefaultOldDoctorLabel,
                        @event.NewDoctorName ?? DefaultNewDoctorLabel,
                        @event.RefundAmount);
                }
                else
                {
                    emailContent = EmailTemplate.BuildDoctorChangeRefundEmailNoBankAccountHtml(
                        patientName,
                        @event.AppointmentDate,
                        @event.OriginalDoctorName ?? DefaultOldDoctorLabel,
                        @event.OriginalConsultationFee ?? 0,
                        @event.NewDoctorName ?? DefaultNewDoctorLabel,
                        @event.NewConsultationFee ?? 0,
                        @event.RefundAmount);
                    smsContent = SmsTemplate.BuildDoctorChangeRefundSmsNoBankAccount(
                        @event.OriginalDoctorName ?? DefaultOldDoctorLabel,
                        @event.NewDoctorName ?? DefaultNewDoctorLabel,
                        @event.RefundAmount);
                }
            }
            else
            {
                // Regular cancellation refund (patient or staff cancelled)
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

