using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Helpers;
using BookingCare.Services.Notification.Exceptions;
using BookingCare.Shared.Common.Models;
using BookingCare.Services.Auth.Protos;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Handler for HospitalContractSignedEvent
/// Sends confirmation email to hospital and creates in-app notification for admin
/// </summary>
public class HospitalContractSignedEventHandler : IIntegrationEventHandler<HospitalContractSignedEvent>
{
    private readonly ILogger<HospitalContractSignedEventHandler> _logger;
    private readonly EmailService _emailService;
    private readonly IEventBus _eventBus;
    private readonly AuthService.AuthServiceClient _authGrpcClient;

    public HospitalContractSignedEventHandler(
        ILogger<HospitalContractSignedEventHandler> logger,
        EmailService emailService,
        IEventBus eventBus,
        AuthService.AuthServiceClient authGrpcClient)
    {
        _logger = logger;
        _emailService = emailService;
        _eventBus = eventBus;
        _authGrpcClient = authGrpcClient;
    }

    public async Task HandleAsync(HospitalContractSignedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[HospitalContractSignedEventHandler] Processing contract signed event for hospital: {HospitalName}, Contract: {ContractNumber}",
                @event.HospitalName,
                @event.ContractNumber
            );

            // Build email content for hospital confirmation
            var emailHtml = EmailTemplate.BuildContractSignedConfirmationEmailHtml(
                hospitalName: @event.HospitalName,
                representativeName: @event.RepresentativeName,
                contractNumber: @event.ContractNumber,
                signedAt: @event.SignedAt
            );

            var subject = $"Xác nhận ký hợp đồng thành công - {@event.ContractNumber}";

            // Send confirmation email to hospital representative
            await _emailService.SendEmailAsync(
                toEmail: @event.RepresentativeEmail,
                subject: subject,
                content: emailHtml,
                isHtml: true,
                cancellationToken: cancellationToken
            );

            // Create in-app notification for all admins
            await CreateAdminNotificationsAsync(@event, cancellationToken);

            _logger.LogInformation(
                "[HospitalContractSignedEventHandler] Successfully sent contract signed confirmation to hospital: {Email}, Contract: {ContractNumber}",
                @event.RepresentativeEmail,
                @event.ContractNumber
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalContractSignedEventHandler] Failed to send contract signed notification for contract: {ContractNumber}",
                @event.ContractNumber
            );

            // Re-throw as EmailDeliveryException for proper error handling
            throw new EmailDeliveryException(
                $"Failed to send contract signed confirmation for contract {@event.ContractNumber}",
                @event.RepresentativeEmail,
                ex
            );
        }
    }

    /// <summary>
    /// Create in-app notifications for all admin accounts
    /// </summary>
    private async Task CreateAdminNotificationsAsync(HospitalContractSignedEvent @event, CancellationToken cancellationToken)
    {
        var content = new NotificationContent
        {
            TitleVi = "Hợp đồng đã được ký",
            TitleEn = "Contract Signed",
            ContentVi = $"Bệnh viện {@event.HospitalName} đã ký hợp đồng {@event.ContractNumber}. Vui lòng xem xét và phê duyệt.",
            ContentEn = $"Hospital {@event.HospitalName} has signed contract {@event.ContractNumber}. Please review and approve.",
            ActionUrl = "/admin/hospital-registrations",
            Metadata = new Dictionary<string, object>
            {
                { "registrationId", @event.RegistrationId },
                { "hospitalName", @event.HospitalName },
                { "contractNumber", @event.ContractNumber },
                { "signedAt", @event.SignedAt },
                { "signedContractUrl", @event.SignedContractUrl }
            }
        };

        var helper = new AdminNotificationHelper(_logger, _eventBus, _authGrpcClient);
        await helper.NotifyAllAdminsAsync(content, nameof(HospitalContractSignedEventHandler), cancellationToken);
    }
}
