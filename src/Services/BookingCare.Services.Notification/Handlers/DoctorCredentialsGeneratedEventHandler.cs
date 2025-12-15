using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Exceptions;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for DoctorCredentialsGeneratedEvent
/// Sends email to doctor with auto-generated login credentials
/// </summary>
public class DoctorCredentialsGeneratedEventHandler : IIntegrationEventHandler<DoctorCredentialsGeneratedEvent>
{
    private readonly ILogger<DoctorCredentialsGeneratedEventHandler> _logger;
    private readonly EmailService _emailService;

    public DoctorCredentialsGeneratedEventHandler(
        ILogger<DoctorCredentialsGeneratedEventHandler> logger,
        EmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task HandleAsync(DoctorCredentialsGeneratedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[DoctorCredentialsGeneratedEventHandler] Processing event for doctor: {Email}",
                @event.Email
            );

            // Build email content using template
            var emailHtml = EmailTemplate.BuildDoctorCredentialsEmailHtml(
                fullName: @event.FullName,
                email: @event.Email,
                password: @event.GeneratedPassword,
                loginUrl: @event.LoginUrl
            );

            var subject = "Chào mừng đến với Hệ thống MedCure - Thông tin đăng nhập";

            // Send email
            await _emailService.SendEmailAsync(
                toEmail: @event.Email,
                subject: subject,
                content: emailHtml,
                isHtml: true,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "[DoctorCredentialsGeneratedEventHandler] Successfully sent credentials email to: {Email}",
                @event.Email
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[DoctorCredentialsGeneratedEventHandler] Failed to send credentials email to: {Email}",
                @event.Email
            );

            // Re-throw as NotificationException for proper error handling
            throw new EmailDeliveryException(
                $"Failed to send doctor credentials email to {@event.Email}",
                @event.Email,
                ex
            );
        }
    }
}

