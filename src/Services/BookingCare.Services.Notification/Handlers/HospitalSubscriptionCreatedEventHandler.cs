using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Exceptions;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for HospitalSubscriptionCreatedEvent
/// Sends email to hospital when a new subscription is successfully created
/// </summary>
public class HospitalSubscriptionCreatedEventHandler : IIntegrationEventHandler<HospitalSubscriptionCreatedEvent>
{
    private readonly ILogger<HospitalSubscriptionCreatedEventHandler> _logger;
    private readonly EmailService _emailService;

    public HospitalSubscriptionCreatedEventHandler(
        ILogger<HospitalSubscriptionCreatedEventHandler> logger,
        EmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task HandleAsync(HospitalSubscriptionCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[HospitalSubscriptionCreatedEventHandler] Processing event for hospital: {HospitalId}, Plan: {PlanName}",
                @event.HospitalId,
                @event.PlanName
            );

            // Validate required fields
            if (string.IsNullOrWhiteSpace(@event.HospitalEmail))
            {
                _logger.LogWarning(
                    "[HospitalSubscriptionCreatedEventHandler] Hospital email is missing for HospitalId: {HospitalId}",
                    @event.HospitalId
                );
                return;
            }

            // Build email content using template
            var emailHtml = EmailTemplate.BuildHospitalSubscriptionCreatedEmailHtml(
                hospitalName: @event.HospitalName,
                contactPersonName: @event.ContactPersonName,
                planName: @event.PlanName,
                billingCycle: @event.BillingCycle,
                price: @event.Price,
                startDate: @event.StartDate,
                endDate: @event.EndDate,
                maxDoctors: @event.MaxDoctors,
                maxAppointmentsPerMonth: @event.MaxAppointmentsPerMonth,
                features: @event.Features
            );

            var subject = $"Đăng ký gói dịch vụ {@event.PlanName} thành công - BookingCare";

            // Send email
            await _emailService.SendEmailAsync(
                toEmail: @event.HospitalEmail,
                subject: subject,
                content: emailHtml,
                isHtml: true,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "[HospitalSubscriptionCreatedEventHandler] Successfully sent subscription confirmation email to: {Email}",
                @event.HospitalEmail
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalSubscriptionCreatedEventHandler] Failed to send subscription confirmation email to: {Email}",
                @event.HospitalEmail
            );

            // Re-throw as NotificationException for proper error handling
            throw new EmailDeliveryException(
                $"Failed to send hospital subscription confirmation email to {@event.HospitalEmail}",
                @event.HospitalEmail,
                ex
            );
        }
    }
}

