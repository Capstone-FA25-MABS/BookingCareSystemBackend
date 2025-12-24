using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.EventBus.Abstractions;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Event handler for daily meal plan generated notifications
/// Creates in-app notification only (email is sent via NotificationSendEvent)
/// Pattern matches AppointmentBookingSuccessNotificationEventHandler
/// </summary>
public class DailyMealPlanGeneratedEventHandler : IIntegrationEventHandler<DailyMealPlanGeneratedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<DailyMealPlanGeneratedEventHandler> _logger;

    public DailyMealPlanGeneratedEventHandler(
        IEventBus eventBus,
        ILogger<DailyMealPlanGeneratedEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(DailyMealPlanGeneratedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NotificationService] Received DailyMealPlanGeneratedEvent - UserId: {UserId}, Date: {Date}, MealPlanId: {MealPlanId}",
            @event.UserId, @event.Date, @event.MealPlanId);

        try
        {
            // Create in-app notification event
            var userName = !string.IsNullOrEmpty(@event.UserFullName) ? @event.UserFullName : "bạn";
            var notificationEvent = new CreateInAppNotificationEvent
            {
                UserId = @event.UserId.ToString(),
                Type = NotificationType.NutritionMealPlan,
                Content = new NotificationContent
                {
                    TitleVi = "Thực đơn hôm nay đã sẵn sàng!",
                    TitleEn = "Today's Meal Plan is Ready!",
                    ContentVi = $"Chào {userName}! Thực đơn dinh dưỡng cho ngày {@event.Date:dd/MM/yyyy} đã được tạo với {@event.MealCount} bữa ăn và tổng {@event.TotalCalories} kcal. Xem ngay để bắt đầu chế độ ăn lành mạnh!",
                    ContentEn = $"Hi {userName}! Your nutrition meal plan for {@event.Date:MM/dd/yyyy} is ready with {@event.MealCount} meals and {@event.TotalCalories} kcal total. Check it out to start your healthy eating!",
                    Metadata = new Dictionary<string, object>
                    {
                        ["mealPlanId"] = @event.MealPlanId.ToString(),
                        ["date"] = @event.Date.ToString("yyyy-MM-dd"),
                        ["totalCalories"] = @event.TotalCalories,
                        ["mealCount"] = @event.MealCount,
                        ["protein"] = @event.TotalProteinG,
                        ["carbs"] = @event.TotalCarbsG,
                        ["fat"] = @event.TotalFatG
                    },
                    ActionUrl = "/user/profile?tab=notifications&category=nutrition",
                    Icon = "isax isax-cake",
                    Priority = NotificationPriority.Normal,
                    ExpirationDays = 7
                }
            };

            // Publish in-app notification event
            await _eventBus.PublishAsync(notificationEvent, null, cancellationToken);

            _logger.LogInformation(
                "[NotificationService] Successfully processed meal plan notification - UserId: {UserId}, MealPlanId: {MealPlanId}",
                @event.UserId, @event.MealPlanId);

            // Send email notification via NotificationSendEvent (matches appointment pattern)
            if (!string.IsNullOrEmpty(@event.UserEmail))
            {
                var emailEvent = new NotificationSendEvent
                {
                    UserId = @event.UserId,
                    Title = "🍽️ Thực đơn dinh dưỡng hôm nay",
                    Message = BuildMealPlanEmailContent(@event, userName),
                    Type = "email",
                    Data = new Dictionary<string, object>
                    {
                        ["email"] = @event.UserEmail,
                        ["subject"] = "🍽️ Thực đơn dinh dưỡng hôm nay",
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
                "[NotificationService] Failed to process meal plan notification - UserId: {UserId}, MealPlanId: {MealPlanId}",
                @event.UserId, @event.MealPlanId);

            // Don't re-throw to avoid breaking the event processing pipeline
        }
    }

    private static string BuildMealPlanEmailContent(DailyMealPlanGeneratedEvent @event, string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .stats {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .stat-item {{ display: inline-block; margin: 10px 20px; }}
        .stat-value {{ font-size: 24px; font-weight: bold; color: #667eea; }}
        .stat-label {{ font-size: 14px; color: #666; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🍽️ Thực đơn dinh dưỡng hôm nay</h1>
        </div>
        <div class=""content"">
            <p>Chào {userName},</p>
            <p>Thực đơn dinh dưỡng cho ngày <strong>{@event.Date:dd/MM/yyyy}</strong> đã được tạo thành công!</p>
            
            <div class=""stats"">
                <div class=""stat-item"">
                    <div class=""stat-value"">{@event.MealCount}</div>
                    <div class=""stat-label"">Bữa ăn</div>
                </div>
                <div class=""stat-item"">
                    <div class=""stat-value"">{@event.TotalCalories}</div>
                    <div class=""stat-label"">Calories</div>
                </div>
                <div class=""stat-item"">
                    <div class=""stat-value"">{@event.TotalProteinG}g</div>
                    <div class=""stat-label"">Protein</div>
                </div>
                <div class=""stat-item"">
                    <div class=""stat-value"">{@event.TotalCarbsG}g</div>
                    <div class=""stat-label"">Carbs</div>
                </div>
                <div class=""stat-item"">
                    <div class=""stat-value"">{@event.TotalFatG}g</div>
                    <div class=""stat-label"">Fat</div>
                </div>
            </div>

            <p>Hãy bắt đầu chế độ ăn lành mạnh của bạn ngay hôm nay!</p>
            
            <p style=""text-align: center;"">
                <a href=""http://localhost:3000/nutrition/dashboard"" class=""button"">Xem thực đơn</a>
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
