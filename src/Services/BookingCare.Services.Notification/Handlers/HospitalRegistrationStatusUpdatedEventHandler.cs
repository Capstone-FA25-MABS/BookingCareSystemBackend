using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Exceptions;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for HospitalRegistrationStatusUpdatedEvent
/// Sends email when admin updates the registration status (CONFIRMED or CANCELLED)
/// </summary>
public class HospitalRegistrationStatusUpdatedEventHandler : IIntegrationEventHandler<HospitalRegistrationStatusUpdatedEvent>
{
    private readonly ILogger<HospitalRegistrationStatusUpdatedEventHandler> _logger;
    private readonly EmailService _emailService;

    // Status enum: 0=PENDING, 1=CONFIRMED, 2=CANCELLED
    private const int STATUS_PENDING = 0;
    private const int STATUS_CONFIRMED = 1;
    private const int STATUS_CANCELLED = 2;

    public HospitalRegistrationStatusUpdatedEventHandler(
        ILogger<HospitalRegistrationStatusUpdatedEventHandler> logger,
        EmailService _emailService)
    {
        _logger = logger;
        this._emailService = _emailService;
    }

    public async Task HandleAsync(HospitalRegistrationStatusUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[HospitalRegistrationStatusUpdatedEventHandler] Processing event for hospital: {HospitalName}, Status: {Status}",
                @event.HospitalName,
                @event.StatusText
            );

            string emailHtml;
            string subject;

            // Build appropriate email based on status
            switch (@event.Status)
            {
                case STATUS_CONFIRMED:
                    emailHtml = EmailTemplate.BuildHospitalRegistrationApprovedEmailHtml(
                        hospitalName: @event.HospitalName,
                        contractFileUrl: @event.ContractFileUrl
                    );
                    subject = $"🎉 Chúc mừng - Đăng ký hợp tác được chấp thuận - {@event.HospitalName}";
                    break;

                case STATUS_CANCELLED:
                    var reason = @event.Reason ?? "Hồ sơ đăng ký chưa đáp ứng đủ các tiêu chí cần thiết.";
                    emailHtml = EmailTemplate.BuildHospitalRegistrationRejectedEmailHtml(
                        hospitalName: @event.HospitalName,
                        reason: reason
                    );
                    subject = $"Thông báo về đơn đăng ký hợp tác - {@event.HospitalName}";
                    break;

                default:
                    _logger.LogWarning(
                        "[HospitalRegistrationStatusUpdatedEventHandler] Unexpected status {Status} for hospital {HospitalName}. Skipping email.",
                        @event.Status,
                        @event.HospitalName
                    );
                    return;
            }

            // Send email to representative
            await _emailService.SendEmailAsync(
                toEmail: @event.RepresentativeEmail,
                subject: subject,
                content: emailHtml,
                isHtml: true,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "[HospitalRegistrationStatusUpdatedEventHandler] Successfully sent status update email to: {RepresentativeEmail}, Status: {Status}",
                @event.RepresentativeEmail,
                @event.StatusText
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalRegistrationStatusUpdatedEventHandler] Failed to send status update email to: {RepresentativeEmail}",
                @event.RepresentativeEmail
            );

            // Re-throw as NotificationException for proper error handling
            throw new EmailDeliveryException(
                $"Failed to send hospital registration status update email to {@event.RepresentativeEmail}",
                @event.RepresentativeEmail,
                ex
            );
        }
    }
}

