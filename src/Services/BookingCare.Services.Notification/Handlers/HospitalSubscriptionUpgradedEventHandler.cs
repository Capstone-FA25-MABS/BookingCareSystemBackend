using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Exceptions;
using BookingCare.Services.Notification.Models.DTOs;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for HospitalSubscriptionUpgradedEvent
/// Sends email to hospital when a subscription is successfully upgraded
/// </summary>
public class HospitalSubscriptionUpgradedEventHandler : IIntegrationEventHandler<HospitalSubscriptionUpgradedEvent>
{
    private readonly ILogger<HospitalSubscriptionUpgradedEventHandler> _logger;
    private readonly EmailService _emailService;

    public HospitalSubscriptionUpgradedEventHandler(
        ILogger<HospitalSubscriptionUpgradedEventHandler> logger,
        EmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task HandleAsync(HospitalSubscriptionUpgradedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[HospitalSubscriptionUpgradedEventHandler] Processing event for hospital: {HospitalId}, New Plan: {NewPlanName}",
                @event.HospitalId,
                @event.NewPlanName
            );

            // Validate required fields
            if (string.IsNullOrWhiteSpace(@event.HospitalEmail))
            {
                _logger.LogWarning(
                    "[HospitalSubscriptionUpgradedEventHandler] Hospital email is missing for HospitalId: {HospitalId}",
                    @event.HospitalId
                );
                return;
            }

            // Build email content using template
            var emailData = new HospitalSubscriptionUpgradedEmailData
            {
                HospitalName = @event.HospitalName,
                ContactPersonName = @event.ContactPersonName,
                PreviousPlanName = @event.PreviousPlanName,
                PreviousBillingCycle = @event.PreviousBillingCycle,
                PreviousPrice = @event.PreviousPrice,
                NewPlanName = @event.NewPlanName,
                NewBillingCycle = @event.NewBillingCycle,
                NewPrice = @event.NewPrice,
                NewStartDate = @event.NewStartDate,
                NewEndDate = @event.NewEndDate,
                BonusDays = @event.BonusDays,
                NewMaxDoctors = @event.NewMaxDoctors,
                NewMaxAppointmentsPerMonth = @event.NewMaxAppointmentsPerMonth,
                NewFeatures = @event.NewFeatures
            };
            var emailHtml = EmailTemplate.BuildHospitalSubscriptionUpgradedEmailHtml(emailData);

            var subject = $"Nâng cấp lên {@event.NewPlanName} thành công - BookingCare";

            // Send email
            await _emailService.SendEmailAsync(
                toEmail: @event.HospitalEmail,
                subject: subject,
                content: emailHtml,
                isHtml: true,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "[HospitalSubscriptionUpgradedEventHandler] Successfully sent subscription upgrade email to: {Email}",
                @event.HospitalEmail
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalSubscriptionUpgradedEventHandler] Failed to send subscription upgrade email to: {Email}",
                @event.HospitalEmail
            );

            // Re-throw as NotificationException for proper error handling
            throw new EmailDeliveryException(
                $"Failed to send hospital subscription upgrade email to {@event.HospitalEmail}",
                @event.HospitalEmail,
                ex
            );
        }
    }
}

