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
        try
        {
            _logger.LogInformation(
                "Handling DailyMealPlanGeneratedEvent for UserId: {UserId}, Date: {Date}. Will send notification after 1 minute delay.",
                @event.UserId, @event.Date);

            // Delay 1 minute before sending notification (for testing purposes)
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);

            _logger.LogInformation(
                "1 minute delay completed. Now sending meal plan notification for UserId: {UserId}",
                @event.UserId);

            // Create notification event to be handled by CreateInAppNotificationEventHandler
            var notificationEvent = new CreateInAppNotificationEvent
            {
                UserId = @event.UserId.ToString(),
                Type = NotificationType.NutritionMealPlan,
                Content = new NotificationContent
                {
                    TitleVi = "Thực đơn hôm nay đã sẵn sàng!",
                    TitleEn = "Today's Meal Plan is Ready!",
                    ContentVi = $"Thực đơn dinh dưỡng cho ngày {@event.Date:dd/MM/yyyy} đã được tạo với {@event.MealCount} bữa ăn và tổng {@event.TotalCalories} kcal. Xem ngay để bắt đầu chế độ ăn lành mạnh!",
                    ContentEn = $"Your nutrition meal plan for {@event.Date:MM/dd/yyyy} is ready with {@event.MealCount} meals and {@event.TotalCalories} kcal total. Check it out to start your healthy eating!",
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

            // Send email notification
            await SendMealPlanEmailAsync(@event, cancellationToken);

            _logger.LogInformation(
                "Successfully published meal plan notification event for UserId: {UserId}",
                @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling DailyMealPlanGeneratedEvent for UserId: {UserId}",
                @event.UserId);
            throw;
        }
    }

    private async Task SendMealPlanEmailAsync(
        DailyMealPlanGeneratedEvent @event,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Get user email from event or cache
            // For now, skip email sending if email not provided in event
            // This will be implemented when event includes user email

            _logger.LogInformation(
                "Meal plan email sending skipped for UserId: {UserId} (email not available in event)",
                @event.UserId
            );

            // Uncomment when event includes user email:
            /*
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
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send meal plan email for UserId: {UserId}", @event.UserId);
            // Don't throw - email failure shouldn't break notification flow
        }
    }
}
