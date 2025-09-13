using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Examples;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Auth.Controllers;

/// <summary>
/// Controller demonstrating gRPC-based saga orchestration for user registration
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserRegistrationController : ControllerBase
{
    private readonly ISagaManager _sagaManager;
    private readonly ISagaStateStore _sagaStateStore;
    private readonly ILogger<UserRegistrationController> _logger;

    public UserRegistrationController(
        ISagaManager sagaManager, 
        ISagaStateStore sagaStateStore,
        ILogger<UserRegistrationController> logger)
    {
        _sagaManager = sagaManager;
        _sagaStateStore = sagaStateStore;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user using saga orchestration with gRPC calls
    /// This demonstrates distributed transaction management across multiple services:
    /// 1. Creates user account in Auth Service via gRPC
    /// 2. Creates user profile in User Service via gRPC  
    /// 3. Sends verification email via Notification Service
    /// 
    /// If any step fails, all completed steps are automatically compensated
    /// </summary>
    /// <param name="request">User registration data</param>
    /// <returns>Saga execution result with tracking ID</returns>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterUser([FromBody] UserRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("Starting user registration saga for email: {Email}", request.Email);

            // Validate request
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { Error = "Email and password are required" });
            }

            // Create saga context
            var context = UserRegistrationSagaFactory.CreateContext(request);

            // Start the saga
            var sagaId = await _sagaManager.StartSagaAsync<UserRegistrationGrpcSaga>(context);

            _logger.LogInformation("User registration saga started successfully. SagaId: {SagaId}, Email: {Email}", 
                sagaId, request.Email);

            return Ok(new
            {
                SagaId = sagaId,
                Message = "User registration process started successfully",
                Email = request.Email,
                UserId = context.GetData<string>("UserId")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting user registration saga for email: {Email}", request.Email);
            return StatusCode(500, new { Error = "Failed to start user registration process", Details = ex.Message });
        }
    }

    /// <summary>
    /// Gets the status of a user registration saga
    /// </summary>
    /// <param name="sagaId">The saga identifier</param>
    /// <returns>Current saga status and execution details</returns>
    [HttpGet("status/{sagaId}")]
    public async Task<IActionResult> GetRegistrationStatus(Guid sagaId)
    {
        try
        {
            _logger.LogInformation("Getting registration status for SagaId: {SagaId}", sagaId);

            // Get the actual saga state from the state store
            var sagaState = await _sagaStateStore.GetSagaStateAsync(sagaId);

            if (sagaState == null)
            {
                return NotFound(new { Error = "Saga not found", SagaId = sagaId });
            }

            return Ok(new
            {
                SagaId = sagaId,
                Status = sagaState.Status.ToString(),
                Message = GetStatusMessage(sagaState.Status),
                CurrentStep = sagaState.CurrentStep,
                CompletedSteps = sagaState.CompletedSteps,
                CompensatedSteps = sagaState.CompensatedSteps,
                CreatedAt = sagaState.CreatedAt,
                UpdatedAt = sagaState.UpdatedAt,
                CompletedAt = sagaState.CompletedAt,
                ErrorMessage = sagaState.ErrorMessage,
                RetryCount = sagaState.RetryCount,
                NextRetryAt = sagaState.NextRetryAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registration status for SagaId: {SagaId}", sagaId);
            return StatusCode(500, new { Error = "Failed to get registration status", Details = ex.Message });
        }
    }

    private static string GetStatusMessage(BookingCare.Shared.Saga.Models.SagaStatus status)
    {
        return status switch
        {
            BookingCare.Shared.Saga.Models.SagaStatus.Pending => "Registration process is pending",
            BookingCare.Shared.Saga.Models.SagaStatus.Running => "Registration is in progress",
            BookingCare.Shared.Saga.Models.SagaStatus.Completed => "Registration completed successfully",
            BookingCare.Shared.Saga.Models.SagaStatus.Failed => "Registration failed",
            BookingCare.Shared.Saga.Models.SagaStatus.Compensated => "Registration was rolled back due to failure",
            BookingCare.Shared.Saga.Models.SagaStatus.Compensating => "Rolling back registration due to failure",
            BookingCare.Shared.Saga.Models.SagaStatus.Cancelled => "Registration was cancelled",
            BookingCare.Shared.Saga.Models.SagaStatus.TimedOut => "Registration timed out",
            _ => "Unknown status"
        };
    }

    /// <summary>
    /// Demonstrates quick user registration with minimal data
    /// </summary>
    /// <param name="request">Minimal registration data</param>
    /// <returns>Saga execution result</returns>
    [HttpPost("register/quick")]
    public async Task<IActionResult> QuickRegister([FromBody] QuickRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("Starting quick user registration for email: {Email}", request.Email);

            // Create minimal saga context
            var context = UserRegistrationSagaFactory.CreateMinimalContext(
                request.Email, 
                request.Password, 
                request.FirstName, 
                request.LastName);

            // Start the saga
            var sagaId = await _sagaManager.StartSagaAsync<UserRegistrationGrpcSaga>(context);

            return Ok(new
            {
                SagaId = sagaId,
                Message = "Quick registration started successfully",
                Email = request.Email,
                UserId = context.GetData<string>("UserId")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in quick registration for email: {Email}", request.Email);
            return StatusCode(500, new { Error = "Failed to start quick registration", Details = ex.Message });
        }
    }
}

/// <summary>
/// Minimal registration request for quick signup
/// </summary>
public class QuickRegistrationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
