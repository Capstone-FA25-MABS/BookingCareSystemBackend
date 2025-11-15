using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;


namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Event handler for Hospital Account Created Event
/// Sends credentials email to the hospital
/// </summary>
public class HospitalAccountCreatedEventHandler
    : IIntegrationEventHandler<HospitalAccountCreatedEvent>
{
    private readonly EmailService _emailService;
    private readonly ILogger<HospitalAccountCreatedEventHandler> _logger;

    public HospitalAccountCreatedEventHandler(
        EmailService emailService,
        ILogger<HospitalAccountCreatedEventHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(
        HospitalAccountCreatedEvent @event,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[HospitalAccountCreatedEventHandler] Sending credentials email to {RepresentativeEmail} for hospital {HospitalName}",
            @event.RepresentativeEmail, @event.HospitalName);

        try
        {
            var emailBody = EmailTemplate.BuildHospitalAccountCredentialsEmailHtml(
                @event.HospitalName,
                @event.HospitalEmail,
                @event.GeneratedPassword,
                @event.LoginUrl,
                @event.ContractFileUrl);

            await _emailService.SendEmailAsync(
                toEmail: @event.RepresentativeEmail,
                subject: "Thông Tin Tài Khoản Bệnh Viện - BookingCare",
                content: emailBody,
                isHtml: true,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "[HospitalAccountCreatedEventHandler] Credentials email sent successfully to {RepresentativeEmail}",
                @event.RepresentativeEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalAccountCreatedEventHandler] Failed to send credentials email to {RepresentativeEmail}",
                @event.RepresentativeEmail);
        }
    }
}

