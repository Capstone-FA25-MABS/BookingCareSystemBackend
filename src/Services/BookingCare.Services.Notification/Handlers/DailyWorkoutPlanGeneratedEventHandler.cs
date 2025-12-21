using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.EventBus.Abstractions;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Event handler for daily workout plan generated notifications
/// Creates in-app notification only (email is sent via NotificationSendEvent)
/// Pattern matches AppointmentBookingSuccessNotificationEventHandler
/// </summary>
public class DailyWorkoutPlanGeneratedEventHandler : IIntegrationEventHandler<DailyWorkoutPlanGeneratedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<DailyWorkoutPlanGeneratedEventHandler> _logger;

    public DailyWorkoutPlanGeneratedEventHandler(
        IEventBus eventBus,
        ILogger<DailyWorkoutPlanGeneratedEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(DailyWorkoutPlanGeneratedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NotificationService] Received DailyWorkoutPlanGeneratedEvent - UserId: {UserId}, Date: {Date}, WorkoutPlanId: {WorkoutPlanId}",
            @event.UserId, @event.Date, @event.WorkoutPlanId);

        try
        {
            // Create in-app notification event
            var userName = !string.IsNullOrEmpty(@event.UserFullName) ? @event.UserFullName : "bạn";
            var notificationEvent = new CreateInAppNotificationEvent
            {
                UserId = @event.UserId.ToString(),
                Type = NotificationType.NutritionWorkoutPlan,
                Content = new NotificationContent
                {
                    TitleVi = "Kế hoạch tập luyện hôm nay!",
                    TitleEn = "Today's Workout Plan!",
                    ContentVi = $"Chào {userName}! Kế hoạch tập luyện {@event.WorkoutType} cho ngày {@event.Date:dd/MM/yyyy} đã sẵn sàng với {@event.ExerciseCount} bài tập trong {@event.DurationMinutes} phút. Đốt cháy {@event.EstimatedCaloriesBurned} kcal!",
                    ContentEn = $"Hi {userName}! Your {@event.WorkoutType} workout plan for {@event.Date:MM/dd/yyyy} is ready with {@event.ExerciseCount} exercises in {@event.DurationMinutes} minutes. Burn {@event.EstimatedCaloriesBurned} kcal!",
                    Metadata = new Dictionary<string, object>
                    {
                        ["workoutPlanId"] = @event.WorkoutPlanId.ToString(),
                        ["date"] = @event.Date.ToString("yyyy-MM-dd"),
                        ["workoutType"] = @event.WorkoutType,
                        ["durationMinutes"] = @event.DurationMinutes,
                        ["exerciseCount"] = @event.ExerciseCount,
                        ["caloriesBurned"] = @event.EstimatedCaloriesBurned
                    },
                    ActionUrl = "/user/profile?tab=notifications&category=workout",
                    Icon = "isax isax-activity",
                    Priority = NotificationPriority.Normal,
                    ExpirationDays = 7
                }
            };

            // Publish in-app notification event
            await _eventBus.PublishAsync(notificationEvent, null, cancellationToken);

            _logger.LogInformation(
                "[NotificationService] Successfully processed workout plan notification - UserId: {UserId}, WorkoutPlanId: {WorkoutPlanId}",
                @event.UserId, @event.WorkoutPlanId);

            // Send email notification via NotificationSendEvent (matches appointment pattern)
            if (!string.IsNullOrEmpty(@event.UserEmail))
            {
                var emailEvent = new NotificationSendEvent
                {
                    UserId = @event.UserId,
                    Title = "💪 Kế hoạch tập luyện hôm nay",
                    Message = BuildWorkoutPlanEmailContent(@event, userName),
                    Type = "email",
                    Data = new Dictionary<string, object>
                    {
                        ["email"] = @event.UserEmail,
                        ["subject"] = "💪 Kế hoạch tập luyện hôm nay",
                        ["html"] = true
                    },
                    ScheduledAt = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(emailEvent, null, cancellationToken);

                _logger.LogInformation(
                    "[NotificationService] Published email notification event for UserId: {UserId}",
                    @event.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NotificationService] Failed to process workout plan notification - UserId: {UserId}, WorkoutPlanId: {WorkoutPlanId}",
                @event.UserId, @event.WorkoutPlanId);

            // Don't re-throw to avoid breaking the event processing pipeline
        }
    }

    private static string BuildWorkoutPlanEmailContent(DailyWorkoutPlanGeneratedEvent @event, string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .stats {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .stat-item {{ display: inline-block; margin: 10px 20px; }}
        .stat-value {{ font-size: 24px; font-weight: bold; color: #f5576c; }}
        .stat-label {{ font-size: 14px; color: #666; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #f5576c; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>💪 Kế hoạch tập luyện hôm nay</h1>
        </div>
        <div class=""content"">
            <p>Chào {userName},</p>
            <p>Kế hoạch tập luyện <strong>{@event.WorkoutType}</strong> cho ngày <strong>{@event.Date:dd/MM/yyyy}</strong> đã được tạo thành công!</p>
            
            <div class=""stats"">
                <div class=""stat-item"">
                    <div class=""stat-value"">{@event.ExerciseCount}</div>
                    <div class=""stat-label"">Bài tập</div>
                </div>
                <div class=""stat-item"">
                    <div class=""stat-value"">{@event.DurationMinutes}</div>
                    <div class=""stat-label"">Phút</div>
                </div>
                <div class=""stat-item"">
                    <div class=""stat-value"">{@event.EstimatedCaloriesBurned}</div>
                    <div class=""stat-label"">Calories đốt cháy</div>
                </div>
            </div>

            <p>Hãy bắt đầu tập luyện để đạt được mục tiêu sức khỏe của bạn!</p>
            
            <p style=""text-align: center;"">
                <a href=""http://localhost:3000/nutrition/dashboard"" class=""button"">Xem kế hoạch</a>
            </p>

            <p style=""color: #666; font-size: 12px; margin-top: 30px;"">
                Đây là email tự động từ hệ thống Medcure. Vui lòng không trả lời email này.
            </p>
        </div>
    </div>
</body>
</html>";
    }
}
