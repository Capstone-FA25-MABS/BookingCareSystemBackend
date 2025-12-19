using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.EventBus.Abstractions;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Event handler for daily meal plan generated notifications
/// Sends both email and in-app notification
/// NOTE: Email sending requires user email to be passed in the event
/// </summary>
public class DailyMealPlanGeneratedEventHandler : IIntegrationEventHandler<DailyMealPlanGeneratedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly EmailService _emailService;
    private readonly ILogger<DailyMealPlanGeneratedEventHandler> _logger;

    public DailyMealPlanGeneratedEventHandler(
        IEventBus eventBus,
        EmailService emailService,
        ILogger<DailyMealPlanGeneratedEventHandler> logger)
    {
        _eventBus = eventBus;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(DailyMealPlanGeneratedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NotificationService] Received DailyMealPlanGeneratedEvent - UserId: {UserId}, Date: {Date}, MealPlanId: {MealPlanId}",
            @event.UserId, @event.Date, @event.MealPlanId);

        try
        {
            // Send email notification first
            await SendMealPlanEmailAsync(@event, cancellationToken);

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
                    ExpirationDays = 1
                }
            };

            // Publish in-app notification event
            await _eventBus.PublishAsync(notificationEvent, null, cancellationToken);

            _logger.LogInformation(
                "[NotificationService] Successfully processed meal plan notification - UserId: {UserId}, MealPlanId: {MealPlanId}",
                @event.UserId, @event.MealPlanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NotificationService] Failed to process meal plan notification - UserId: {UserId}, MealPlanId: {MealPlanId}",
                @event.UserId, @event.MealPlanId);

            // Don't re-throw to avoid breaking the event processing pipeline
        }
    }

    private async Task SendMealPlanEmailAsync(
        DailyMealPlanGeneratedEvent @event,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(@event.UserEmail))
            {
                _logger.LogWarning("User email not found in event for UserId: {UserId}", @event.UserId);
                return;
            }

            var emailContent = NutritionEmailTemplate.BuildMealPlanEmail(@event);

            await _emailService.SendEmailAsync(
                @event.UserEmail,
                "🍽️ Thực đơn dinh dưỡng hôm nay",
                emailContent,
                isHtml: true,
                cancellationToken
            );

            _logger.LogInformation(
                "Sent meal plan email to {Email} for UserId: {UserId}",
                @event.UserEmail, @event.UserId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send meal plan email for UserId: {UserId}", @event.UserId);
            // Don't throw - email failure shouldn't break notification flow
        }
    }
}
