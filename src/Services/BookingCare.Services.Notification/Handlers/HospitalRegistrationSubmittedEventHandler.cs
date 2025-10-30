using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Exceptions;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for HospitalRegistrationSubmittedEvent
/// Sends confirmation email to hospital when they submit a partnership registration
/// </summary>
public class HospitalRegistrationSubmittedEventHandler : IIntegrationEventHandler<HospitalRegistrationSubmittedEvent>
{
    private readonly ILogger<HospitalRegistrationSubmittedEventHandler> _logger;
    private readonly EmailService _emailService;

    public HospitalRegistrationSubmittedEventHandler(
        ILogger<HospitalRegistrationSubmittedEventHandler> logger,
        EmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task HandleAsync(HospitalRegistrationSubmittedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[HospitalRegistrationSubmittedEventHandler] Processing event for hospital: {HospitalName}, Email: {Email}",
                @event.HospitalName,
                @event.Email
            );

            // Build email content using template
            var emailHtml = EmailTemplate.BuildHospitalRegistrationSubmittedEmailHtml(
                hospitalName: @event.HospitalName,
                email: @event.Email,
                phone: @event.Phone,
                address: @event.Address,
                taxCode: @event.TaxCode
            );

            var subject = $"Xác nhận đăng ký hợp tác - {@event.HospitalName}";

            // Send email
            await _emailService.SendEmailAsync(
                toEmail: @event.Email,
                subject: subject,
                content: emailHtml,
                isHtml: true,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "[HospitalRegistrationSubmittedEventHandler] Successfully sent confirmation email to: {Email}",
                @event.Email
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalRegistrationSubmittedEventHandler] Failed to send confirmation email to: {Email}",
                @event.Email
            );

            // Re-throw as NotificationException for proper error handling
            throw new EmailDeliveryException(
                $"Failed to send hospital registration confirmation email to {@event.Email}",
                @event.Email,
                ex
            );
        }
    }
}

