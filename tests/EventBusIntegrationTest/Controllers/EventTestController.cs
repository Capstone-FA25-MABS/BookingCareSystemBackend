using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using Microsoft.AspNetCore.Mvc;

namespace EventBusIntegrationTest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventTestController : ControllerBase
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<EventTestController> _logger;

    public EventTestController(IEventBus eventBus, ILogger<EventTestController> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    [HttpPost("publish/user-registered")]
    public async Task<IActionResult> PublishUserRegistered([FromBody] UserRegisteredRequest request)
    {
        try
        {
            var @event = new UserRegisteredEvent
            {
                UserId = request.UserId ?? Guid.NewGuid(),
                Email = request.Email ?? "test@example.com",
                Role = request.Role ?? "Patient",
                FullName = request.FullName ?? "Test User",
                RegisteredAt = DateTime.UtcNow
            };

            _logger.LogInformation("📤 Publishing UserRegisteredEvent: {UserId}", @event.UserId);
            await _eventBus.PublishAsync(@event);
            
            return Ok(new { 
                Success = true, 
                Message = "UserRegisteredEvent published successfully",
                EventId = @event.Id,
                UserId = @event.UserId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to publish UserRegisteredEvent");
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    [HttpPost("publish/appointment-created")]
    public async Task<IActionResult> PublishAppointmentCreated([FromBody] AppointmentCreatedRequest request)
    {
        try
        {
            var @event = new AppointmentCreatedEvent
            {
                AppointmentId = request.AppointmentId ?? Guid.NewGuid(),
                PatientId = request.PatientId ?? Guid.NewGuid(),
                DoctorId = request.DoctorId ?? Guid.NewGuid(),
                AppointmentDate = request.AppointmentDate ?? DateTime.UtcNow.AddDays(1),
                Amount = request.Amount ?? 150.00m,
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("📤 Publishing AppointmentCreatedEvent: {AppointmentId}", @event.AppointmentId);
            await _eventBus.PublishAsync(@event);
            
            return Ok(new { 
                Success = true, 
                Message = "AppointmentCreatedEvent published successfully",
                EventId = @event.Id,
                AppointmentId = @event.AppointmentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to publish AppointmentCreatedEvent");
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    [HttpPost("publish/payment-processed")]
    public async Task<IActionResult> PublishPaymentProcessed([FromBody] PaymentProcessedRequest request)
    {
        try
        {
            var @event = new PaymentProcessedEvent
            {
                PaymentId = request.PaymentId ?? Guid.NewGuid(),
                AppointmentId = request.AppointmentId ?? Guid.NewGuid(),
                UserId = request.UserId ?? Guid.NewGuid(),
                Amount = request.Amount ?? 100.00m,
                PaymentMethod = request.PaymentMethod ?? "Credit Card",
                Status = request.Status ?? "Completed",
                TransactionId = request.TransactionId ?? Guid.NewGuid().ToString(),
                ProcessedAt = DateTime.UtcNow
            };

            _logger.LogInformation("📤 Publishing PaymentProcessedEvent: {PaymentId}", @event.PaymentId);
            await _eventBus.PublishAsync(@event);
            
            return Ok(new { 
                Success = true, 
                Message = "PaymentProcessedEvent published successfully",
                EventId = @event.Id,
                PaymentId = @event.PaymentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to publish PaymentProcessedEvent");
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    [HttpPost("publish/notification")]
    public async Task<IActionResult> PublishNotification([FromBody] NotificationRequest request)
    {
        try
        {
            var @event = new NotificationSendEvent
            {
                UserId = request.UserId ?? Guid.NewGuid(),
                Type = request.Type ?? "Email",
                Message = request.Message ?? "Test notification message",
                Title = request.Title ?? "Test Title",
                ScheduledAt = request.ScheduledAt ?? DateTime.UtcNow
            };

            _logger.LogInformation("📤 Publishing NotificationSendEvent: {EventId}", @event.Id);
            
            // Publish with routing key if specified
            if (!string.IsNullOrEmpty(request.RoutingKey))
            {
                await _eventBus.PublishAsync(@event, request.RoutingKey);
            }
            else
            {
                await _eventBus.PublishAsync(@event);
            }
            
            return Ok(new { 
                Success = true, 
                Message = "NotificationSendEvent published successfully",
                EventId = @event.Id,
                NotificationId = @event.Id,
                RoutingKey = request.RoutingKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to publish NotificationSendEvent");
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    [HttpGet("status")]
    public IActionResult GetServiceStatus()
    {
        return Ok(new
        {
            ServiceName = "EventBus Integration Test Service",
            Status = "Running",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            EventBusStatus = "Connected"
        });
    }
}

// Request DTOs
public class UserRegisteredRequest
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? FullName { get; set; }
}

public class AppointmentCreatedRequest
{
    public Guid? AppointmentId { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public decimal? Amount { get; set; }
}

public class PaymentProcessedRequest
{
    public Guid? PaymentId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? UserId { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Status { get; set; }
    public string? TransactionId { get; set; }
}

public class NotificationRequest
{
    public Guid? UserId { get; set; }
    public string? Type { get; set; }
    public string? Message { get; set; }
    public string? Title { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? RoutingKey { get; set; }
}
