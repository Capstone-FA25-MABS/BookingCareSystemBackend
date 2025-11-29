using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Exceptions;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for HospitalContractGeneratedEvent
/// Sends email with contract signing link to hospital representative
/// </summary>
public class HospitalContractGeneratedEventHandler : IIntegrationEventHandler<HospitalContractGeneratedEvent>
{
    private readonly ILogger<HospitalContractGeneratedEventHandler> _logger;
    private readonly EmailService _emailService;

    public HospitalContractGeneratedEventHandler(
        ILogger<HospitalContractGeneratedEventHandler> logger,
        EmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task HandleAsync(HospitalContractGeneratedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[HospitalContractGeneratedEventHandler] Processing contract generated event for hospital: {HospitalName}, Contract: {ContractNumber}",
                @event.HospitalName,
                @event.ContractNumber
            );

            // Build email content using template
            var emailHtml = EmailTemplate.BuildContractGeneratedEmailHtml(
                hospitalName: @event.HospitalName,
                representativeName: @event.RepresentativeName,
                contractNumber: @event.ContractNumber,
                signingLink: @event.SigningLink,
                linkExpiresAt: @event.LinkExpiresAt
            );

            var subject = $"Hợp đồng hợp tác - {@event.ContractNumber} - {@event.HospitalName}";

            // Send email to representative with signing link
            await _emailService.SendEmailAsync(
                toEmail: @event.RepresentativeEmail,
                subject: subject,
                content: emailHtml,
                isHtml: true,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "[HospitalContractGeneratedEventHandler] Successfully sent contract signing email to: {RepresentativeEmail}, Contract: {ContractNumber}",
                @event.RepresentativeEmail,
                @event.ContractNumber
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalContractGeneratedEventHandler] Failed to send contract signing email to: {RepresentativeEmail}, Contract: {ContractNumber}",
                @event.RepresentativeEmail,
                @event.ContractNumber
            );

            // Re-throw as EmailDeliveryException for proper error handling
            throw new EmailDeliveryException(
                $"Failed to send contract signing email to {@event.RepresentativeEmail} for contract {@event.ContractNumber}",
                @event.RepresentativeEmail,
                ex
            );
        }
    }
}
