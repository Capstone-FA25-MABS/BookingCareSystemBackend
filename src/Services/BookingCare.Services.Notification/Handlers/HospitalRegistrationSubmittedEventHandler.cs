using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Exceptions;
using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Services.Notification.Models.DTOs;
using BookingCare.Shared.Common.Enums;
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
    private readonly INotificationService _notificationService;
    private readonly AuthService.AuthServiceClient _authGrpcClient;

    public HospitalRegistrationSubmittedEventHandler(
        ILogger<HospitalRegistrationSubmittedEventHandler> logger,
        EmailService emailService,
        INotificationService notificationService,
        AuthService.AuthServiceClient authGrpcClient)
    {
        _logger = logger;
        _emailService = emailService;
        _notificationService = notificationService;
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
        try
        {
            // Get all admin account IDs via gRPC
            var adminRequest = new GetAccountIdsByRoleRequest
            {
                Role = "Admin",
                ActiveOnly = true
            };

            var adminResponse = await _authGrpcClient.GetAccountIdsByRoleAsync(adminRequest, cancellationToken: cancellationToken);

            if (!adminResponse.Success || !adminResponse.AccountIds.Any())
            {
                _logger.LogWarning(
                    "[HospitalRegistrationSubmittedEventHandler] No admin accounts found to send notification. Message: {Message}",
                    adminResponse.Message
                );
                return;
            }

            _logger.LogInformation(
                "[HospitalRegistrationSubmittedEventHandler] Found {Count} admin accounts to notify",
                adminResponse.AccountIds.Count
            );

            // Create notification for each admin
            var notificationTasks = adminResponse.AccountIds.Select(async adminId =>
            {
                try
                {
                    var notificationDto = new CreateNotificationDto
                    {
                        UserId = adminId,
                        Type = NotificationType.HospitalRegistration,
                        TitleVi = "Đăng ký hợp tác bệnh viện mới",
                        TitleEn = "New Hospital Partnership Registration",
                        ContentVi = $"Bệnh viện {@event.HospitalName} đã gửi đơn đăng ký hợp tác. Vui lòng xem xét và phê duyệt.",
                        ContentEn = $"Hospital {@event.HospitalName} has submitted a partnership registration. Please review and approve.",
                        ActionUrl = $"/admin/hospital-registrations",
                        Metadata = new Dictionary<string, object>
                        {
                            { "registrationId", @event.RegistrationId },
                            { "hospitalName", @event.HospitalName },
                            { "hospitalEmail", @event.HospitalEmail },
                            { "hospitalPhone", @event.HospitalPhone },
                            { "taxCode", @event.TaxCode }
                        }
                    };

                    await _notificationService.CreateNotificationAsync(notificationDto, cancellationToken);

                    _logger.LogDebug(
                        "[HospitalRegistrationSubmittedEventHandler] Created notification for admin: {AdminId}",
                        adminId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[HospitalRegistrationSubmittedEventHandler] Failed to create notification for admin: {AdminId}",
                        adminId
                    );
                }
            });

            await Task.WhenAll(notificationTasks);

            _logger.LogInformation(
                "[HospitalRegistrationSubmittedEventHandler] Completed creating notifications for {Count} admins",
                adminResponse.AccountIds.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalRegistrationSubmittedEventHandler] Failed to create admin notifications for hospital: {HospitalName}",
                @event.HospitalName
            );
            // Don't re-throw here as email sending is more critical
        }
    }
}

