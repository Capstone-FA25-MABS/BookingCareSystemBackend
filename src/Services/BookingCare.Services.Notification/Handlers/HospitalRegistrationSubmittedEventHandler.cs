using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Helpers;
using BookingCare.Services.Notification.Exceptions;
using BookingCare.Shared.Common.Models;
using BookingCare.Services.Auth.Protos;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for HospitalRegistrationSubmittedEvent
/// Sends confirmation email to hospital and creates notification for admin
/// </summary>
public class HospitalRegistrationSubmittedEventHandler : IIntegrationEventHandler<HospitalRegistrationSubmittedEvent>
{
    private readonly ILogger<HospitalRegistrationSubmittedEventHandler> _logger;
    private readonly EmailService _emailService;
    private readonly IEventBus _eventBus;
    private readonly AuthService.AuthServiceClient _authGrpcClient;

    public HospitalRegistrationSubmittedEventHandler(
        ILogger<HospitalRegistrationSubmittedEventHandler> logger,
        EmailService emailService,
        IEventBus eventBus,
        AuthService.AuthServiceClient authGrpcClient)
    {
        _logger = logger;
        _emailService = emailService;
        _eventBus = eventBus;
        _authGrpcClient = authGrpcClient;
    }

    public async Task HandleAsync(HospitalRegistrationSubmittedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[HospitalRegistrationSubmittedEventHandler] Processing event for hospital: {HospitalName}, Representative: {RepresentativeName}, Hospital Email: {HospitalEmail}",
                @event.HospitalName,
                @event.RepresentativeName,
                @event.HospitalEmail
            );

            // Build email content using template
            var emailHtml = EmailTemplate.BuildHospitalRegistrationSubmittedEmailHtml(
                hospitalName: @event.HospitalName,
                hospitalEmail: @event.HospitalEmail,
                hospitalPhone: @event.HospitalPhone,
                address: @event.Address,
                taxCode: @event.TaxCode
            );

            var subject = $"Xác nhận đăng ký hợp tác - {@event.HospitalName}";

            // Send email to representative
            await _emailService.SendEmailAsync(
                toEmail: @event.RepresentativeEmail,
                subject: subject,
                content: emailHtml,
                isHtml: true,
                cancellationToken: cancellationToken
            );

            // Create notification for admin about new hospital registration
            await CreateAdminNotificationAsync(@event, cancellationToken);

            _logger.LogInformation(
                "[HospitalRegistrationSubmittedEventHandler] Successfully sent confirmation email to representative: {RepresentativeEmail}",
                @event.RepresentativeEmail
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalRegistrationSubmittedEventHandler] Failed to send confirmation email to representative: {RepresentativeEmail}",
                @event.RepresentativeEmail
            );

            // Re-throw as NotificationException for proper error handling
            throw new EmailDeliveryException(
                $"Failed to send hospital registration confirmation email to {@event.RepresentativeEmail}",
                @event.RepresentativeEmail,
                ex
            );
        }
    }

    /// <summary>
    /// Create notification for admin about new hospital registration
    /// </summary>
    private async Task CreateAdminNotificationAsync(HospitalRegistrationSubmittedEvent @event, CancellationToken cancellationToken)
    {
        var content = new NotificationContent
        {
            TitleVi = "Đăng ký hợp tác bệnh viện mới",
            TitleEn = "New Hospital Partnership Registration",
            ContentVi = $"Bệnh viện {@event.HospitalName} đã gửi đơn đăng ký hợp tác. Vui lòng xem xét và phê duyệt.",
            ContentEn = $"Hospital {@event.HospitalName} has submitted a partnership registration. Please review and approve.",
            ActionUrl = "/admin/hospital-registrations",
            Metadata = new Dictionary<string, object>
            {
                { "registrationId", @event.RegistrationId },
                { "hospitalName", @event.HospitalName },
                { "hospitalEmail", @event.HospitalEmail },
                { "hospitalPhone", @event.HospitalPhone },
                { "taxCode", @event.TaxCode }
            }
        };

        var helper = new AdminNotificationHelper(_logger, _eventBus, _authGrpcClient);
        await helper.NotifyAllAdminsAsync(content, nameof(HospitalRegistrationSubmittedEventHandler), cancellationToken);
    }
}

