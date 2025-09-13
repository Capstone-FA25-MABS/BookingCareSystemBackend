using BookingCare.Services.Notification.Protos;
using Grpc.Core;

namespace BookingCare.Services.Notification.Services;

public class NotificationGrpcService : NotificationService.NotificationServiceBase
{
    private readonly ILogger<NotificationGrpcService> _logger;

    public NotificationGrpcService(ILogger<NotificationGrpcService> logger)
    {
        _logger = logger;
    }

    public override async Task<SendVerificationEmailResponse> SendVerificationEmail(
        SendVerificationEmailRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Sending verification email to UserId: {UserId}, Email: {Email}",
                request.UserId, request.Email);

            // Simulate email sending logic
            await Task.Delay(100); // Simulate email service call

            var emailId = Guid.NewGuid().ToString();

            _logger.LogInformation("Verification email sent successfully. EmailId: {EmailId}, UserId: {UserId}",
                emailId, request.UserId);

            return new SendVerificationEmailResponse
            {
                Success = true,
                Message = "Verification email sent successfully",
                EmailId = emailId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email to UserId: {UserId}, Email: {Email}",
                request.UserId, request.Email);
            return new SendVerificationEmailResponse
            {
                Success = false,
                Message = $"Failed to send verification email: {ex.Message}"
            };
        }
    }

    public override async Task<SendNotificationResponse> SendNotification(
        SendNotificationRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Sending notification to UserId: {UserId}, Type: {Type}",
                request.UserId, request.Type);

            // Simulate notification sending logic
            await Task.Delay(50); // Simulate notification service call

            var notificationId = Guid.NewGuid().ToString();

            _logger.LogInformation("Notification sent successfully. NotificationId: {NotificationId}, UserId: {UserId}",
                notificationId, request.UserId);

            return new SendNotificationResponse
            {
                Success = true,
                Message = "Notification sent successfully",
                NotificationId = notificationId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to UserId: {UserId}, Type: {Type}",
                request.UserId, request.Type);
            return new SendNotificationResponse
            {
                Success = false,
                Message = $"Failed to send notification: {ex.Message}"
            };
        }
    }
}
