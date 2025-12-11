using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.EventBus.Abstractions;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Event handler for daily workout plan generated notifications
/// Sends both email and in-app notification
/// NOTE: Email sending requires user email to be passed in the event
/// </summary>
public class DailyWorkoutPlanGeneratedEventHandler : IIntegrationEventHandler<DailyWorkoutPlanGeneratedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly EmailService _emailService;
    private readonly ILogger<DailyWorkoutPlanGeneratedEventHandler> _logger;

    public DailyWorkoutPlanGeneratedEventHandler(
        IEventBus eventBus,
        EmailService emailService,
        ILogger<DailyWorkoutPlanGeneratedEventHandler> logger)
    {
        _eventBus = eventBus;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(DailyWorkoutPlanGeneratedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Handling DailyWorkoutPlanGeneratedEvent for UserId: {UserId}, Date: {Date}",
                @event.UserId, @event.Date);

            // Create notification event to be handled by CreateInAppNotificationEventHandler
            var notificationEvent = new CreateInAppNotificationEvent
            {
                UserId = @event.UserId.ToString(),
                Type = NotificationType.NutritionWorkoutPlan,
                Content = new NotificationContent
                {
                    TitleVi = "Kế hoạch tập luyện hôm nay!",
                    TitleEn = "Today's Workout Plan!",
                    ContentVi = $"Kế hoạch tập luyện {@event.WorkoutType} cho ngày {@event.Date:dd/MM/yyyy} đã sẵn sàng với {@event.ExerciseCount} bài tập trong {@event.DurationMinutes} phút. Đốt cháy {@event.EstimatedCaloriesBurned} kcal!",
                    ContentEn = $"Your {@event.WorkoutType} workout plan for {@event.Date:MM/dd/yyyy} is ready with {@event.ExerciseCount} exercises in {@event.DurationMinutes} minutes. Burn {@event.EstimatedCaloriesBurned} kcal!",
                    Metadata = new Dictionary<string, object>
                    {
                        ["workoutPlanId"] = @event.WorkoutPlanId.ToString(),
                        ["date"] = @event.Date.ToString("yyyy-MM-dd"),
                        ["workoutType"] = @event.WorkoutType,
                        ["durationMinutes"] = @event.DurationMinutes,
                        ["exerciseCount"] = @event.ExerciseCount,
                        ["caloriesBurned"] = @event.EstimatedCaloriesBurned
                    },
                    ActionUrl = $"/nutrition/workout-plans/{@event.Date:yyyy-MM-dd}",
                    Icon = "isax isax-weight",
                    Priority = NotificationPriority.Normal,
                    ExpirationDays = 1
                }
            };

            // Publish in-app notification event
            await _eventBus.PublishAsync(notificationEvent, null, cancellationToken);

            // Send email notification
            await SendWorkoutPlanEmailAsync(@event, cancellationToken);

            _logger.LogInformation(
                "Successfully published workout plan notification event for UserId: {UserId}",
                @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling DailyWorkoutPlanGeneratedEvent for UserId: {UserId}",
                @event.UserId);
            throw;
        }
    }

    private async Task SendWorkoutPlanEmailAsync(
        DailyWorkoutPlanGeneratedEvent @event,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Get user email from event or cache
            // For now, skip email sending if email not provided in event
            // This will be implemented when event includes user email
            
            _logger.LogInformation(
                "Workout plan email sending skipped for UserId: {UserId} (email not available in event)",
                @event.UserId
            );
            
            // Uncomment when event includes user email:
            /*
            if (string.IsNullOrEmpty(@event.UserEmail))
            {
                _logger.LogWarning("User email not found in event for UserId: {UserId}", @event.UserId);
                return;
            }
            
            var emailContent = NutritionEmailTemplate.BuildWorkoutPlanEmail(@event);
            
            await _emailService.SendEmailAsync(
                @event.UserEmail,
                "💪 Kế hoạch tập luyện hôm nay",
                emailContent,
                isHtml: true,
                cancellationToken
            );
            
            _logger.LogInformation(
                "Sent workout plan email to {Email} for UserId: {UserId}",
                @event.UserEmail, @event.UserId
            );
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send workout plan email for UserId: {UserId}", @event.UserId);
            // Don't throw - email failure shouldn't break notification flow
        }
    }
}
