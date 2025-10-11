using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for AppointmentRefundRequestedIntegrationEvent
/// Sends email and SMS notifications to patient about refund
/// </summary>
public class AppointmentRefundRequestedEventHandler : IIntegrationEventHandler<AppointmentRefundRequestedIntegrationEvent>
{
    private readonly EmailService _emailService;
    private readonly FcmV1Service _fcmService;
    private readonly DeviceStore _deviceStore;
    private readonly ILogger<AppointmentRefundRequestedEventHandler> _logger;

    public AppointmentRefundRequestedEventHandler(
        EmailService emailService,
        FcmV1Service fcmService,
        DeviceStore deviceStore,
        ILogger<AppointmentRefundRequestedEventHandler> logger)
    {
        _emailService = emailService;
        _fcmService = fcmService;
        _deviceStore = deviceStore;
        _logger = logger;
    }

    public async Task HandleAsync(AppointmentRefundRequestedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing refund notification for RefundHistoryId: {RefundHistoryId}, PatientId: {PatientId}",
                @event.RefundHistoryId, @event.PatientId);

            // Determine notification content based on bank account status
            string emailSubject;
            string emailContent;
            string? smsContent = null;

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

            // Send Email if email address is available
            if (!string.IsNullOrWhiteSpace(@event.PatientEmail))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        @event.PatientEmail,
                        emailSubject,
                        emailContent,
                        isHtml: true,
                        cancellationToken);

                    _logger.LogInformation(
                        "Email sent successfully to {Email} for refund {RefundHistoryId}",
                        @event.PatientEmail, @event.RefundHistoryId);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx,
                        "Failed to send email to {Email} for refund {RefundHistoryId}",
                        @event.PatientEmail, @event.RefundHistoryId);
                    // Don't throw - continue to try SMS
                }
            }
            else
            {
                _logger.LogWarning(
                    "No email address available for patient {PatientId}",
                    @event.PatientId);
            }

            // Send SMS if phone number is available
            if (!string.IsNullOrWhiteSpace(@event.PatientPhone) && !string.IsNullOrWhiteSpace(smsContent))
            {
                try
                {
                    var normalizedPhone = FcmV1Service.NormalizePhone(@event.PatientPhone);

                    // Get first active device (SMS gateway)
                    var device = await _deviceStore.GetFirstActiveDeviceAsync();

                    if (device != null)
                    {
                        try
                        {
                            var data = new { phone = normalizedPhone, message = smsContent };
                            var result = await _fcmService.SendDataMessageAsync(device.Token, data);

                            // Update last used timestamp
                            await _deviceStore.UpdateLastUsedAsync(device.Id);

                            _logger.LogInformation(
                                "SMS sent successfully for refund {RefundHistoryId} to {Phone} via device {DeviceId}. Result: {Result}",
                                @event.RefundHistoryId, normalizedPhone, device.Id, result);
                        }
                        catch (Exception fcmEx)
                        {
                            _logger.LogError(fcmEx,
                                "Failed to send SMS via FCM for refund {RefundHistoryId} to {Phone} via device {DeviceId}",
                                @event.RefundHistoryId, normalizedPhone, device.Id);
                            // Don't throw - continue processing
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "No active SMS gateway device available for refund {RefundHistoryId}. SMS content: {Message}",
                            @event.RefundHistoryId, smsContent);
                    }
                }
                catch (Exception smsEx)
                {
                    _logger.LogError(smsEx,
                        "Error processing SMS notification for refund {RefundHistoryId}",
                        @event.RefundHistoryId);
                    // Don't throw - SMS is optional
                }
            }
            else
            {
                _logger.LogWarning(
                    "No phone number available for patient {PatientId} refund notification",
                    @event.PatientId);
            }

            _logger.LogInformation(
                "Completed notification processing for refund {RefundHistoryId}",
                @event.RefundHistoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing refund notification for RefundHistoryId {RefundHistoryId}: {Error}",
                @event.RefundHistoryId, ex.Message);
            // Don't re-throw - notifications are best-effort
        }
    }

}

