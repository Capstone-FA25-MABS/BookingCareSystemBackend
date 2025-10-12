using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Enums;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for managing refund histories
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class RefundHistoriesController : BaseApiController
{
    private const string RefundHistoriesListError = "An error occurred while retrieving refund histories list";

    private readonly IRefundHistoryService _refundHistoryService;
    private readonly IValidator<CreateRefundHistoryRequest> _createValidator;
    private readonly IValidator<UpdateRefundHistoryStatusRequest> _updateValidator;
    private readonly IValidator<GetRefundHistoriesRequest> _getValidator;
    private readonly IValidator<MarkAsTransferredRequest> _markTransferredValidator;
    private readonly IValidator<ReportBankIssueRequest> _reportIssueValidator;
    private readonly ILogger<RefundHistoriesController> _logger;

    public RefundHistoriesController(
        IRefundHistoryService refundHistoryService,
        IValidator<CreateRefundHistoryRequest> createValidator,
        IValidator<UpdateRefundHistoryStatusRequest> updateValidator,
        IValidator<GetRefundHistoriesRequest> getValidator,
        IValidator<MarkAsTransferredRequest> markTransferredValidator,
        IValidator<ReportBankIssueRequest> reportIssueValidator,
        ILogger<RefundHistoriesController> logger)
    {
        _refundHistoryService = refundHistoryService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _getValidator = getValidator;
        _markTransferredValidator = markTransferredValidator;
        _reportIssueValidator = reportIssueValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get refund history by ID
    /// </summary>
    /// <param name="id">Refund history ID</param>
    /// <returns>Refund history info</returns>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRefundHistory(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid refund history ID");
            }

            var refundHistory = await _refundHistoryService.GetByIdAsync(id);
            if (refundHistory == null)
            {
                return NotFound($"Refund history with ID {id} was not found");
            }

            return Success(refundHistory, "Get refund history successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund history with ID: {RefundHistoryId}", id);
            return StatusCode(500, new { Message = "An error occurred while retrieving refund history" });
        }
    }

    /// <summary>
    /// Get refund history by payment ID
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <returns>Refund history info</returns>
    [HttpGet("payment/{paymentId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRefundHistoryByPayment(Guid paymentId)
    {
        try
        {
            if (paymentId == Guid.Empty)
            {
                return BadRequest("Invalid payment ID");
            }

            var refundHistory = await _refundHistoryService.GetByPaymentIdAsync(paymentId);
            if (refundHistory == null)
            {
                return NotFound($"Refund history for payment {paymentId} was not found");
            }

            return Success(refundHistory, "Get refund history successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund history for payment: {PaymentId}", paymentId);
            return StatusCode(500, new { Message = "An error occurred while retrieving refund history" });
        }
    }

    /// <summary>
    /// Get refund histories by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of refund histories</returns>
    [HttpGet("user/{userId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRefundHistoriesByUser(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            var refundHistories = await _refundHistoryService.GetByUserIdAsync(userId);
            var refundHistoriesList = refundHistories.ToList();
            var count = refundHistoriesList.Count;

            var responseData = new
            {
                refundHistories = refundHistoriesList,
                count = count
            };

            var message = count switch
            {
                0 => "No refund history found",
                1 => "Get 1 refund history successful",
                _ => $"Get {count} refund histories successful"
            };

            return Success(responseData, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund histories for user: {UserId}", userId);
            return StatusCode(500, new { Message = RefundHistoriesListError });
        }
    }

    /// <summary>
    /// Get refund histories by hospital ID
    /// </summary>
    /// <param name="hospitalId">Hospital ID</param>
    /// <returns>List of refund histories</returns>
    [HttpGet("hospital/{hospitalId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRefundHistoriesByHospital(Guid hospitalId)
    {
        try
        {
            if (hospitalId == Guid.Empty)
            {
                return BadRequest("Invalid hospital ID");
            }

            var refundHistories = await _refundHistoryService.GetByHospitalIdAsync(hospitalId);
            var refundHistoriesList = refundHistories.ToList();
            var count = refundHistoriesList.Count;

            var responseData = new
            {
                refundHistories = refundHistoriesList,
                count = count
            };

            var message = count switch
            {
                0 => "No refund history found for hospital",
                1 => "Get 1 refund history for hospital successful",
                _ => $"Get {count} refund histories for hospital successful"
            };

            return Success(responseData, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund histories for hospital: {HospitalId}", hospitalId);
            return StatusCode(500, new { Message = RefundHistoriesListError });
        }
    }

    /// <summary>
    /// Get refund histories by status
    /// </summary>
    /// <param name="status">Refund status</param>
    /// <returns>List of refund histories</returns>
    [HttpGet("status/{status}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRefundHistoriesByStatus(RefundStatus status)
    {
        try
        {
            var refundHistories = await _refundHistoryService.GetByStatusAsync(status);
            var refundHistoriesList = refundHistories.ToList();
            var count = refundHistoriesList.Count;

            var responseData = new
            {
                refundHistories = refundHistoriesList,
                count = count
            };

            var message = count switch
            {
                0 => $"No refund history found with status {status}",
                1 => $"Get 1 refund history with status {status} successful",
                _ => $"Get {count} refund histories with status {status} successful"
            };

            return Success(responseData, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund histories by status: {Status}", status);
            return StatusCode(500, new { Message = RefundHistoriesListError });
        }
    }

    /// <summary>
    /// Get paged refund histories with filters and optional status counts
    /// </summary>
    /// <param name="request">Paging and filter info</param>
    /// <returns>Paged refund histories with optional status counts</returns>
    [HttpPost("search")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPagedRefundHistories([FromBody] GetRefundHistoriesRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _getValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Invalid request data", errors);
            }

            var listResponse = await _refundHistoryService.GetPagedAsync(request);
            return Success(listResponse, "Get paged refund histories successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged refund histories");
            return StatusCode(500, new { Message = RefundHistoriesListError });
        }
    }

    /// <summary>
    /// Create a new refund history
    /// </summary>
    /// <param name="request">Refund history info</param>
    /// <returns>Created refund history</returns>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateRefundHistory([FromBody] CreateRefundHistoryRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Invalid request data", errors);
            }

            var refundHistory = await _refundHistoryService.CreateAsync(request);
            return Created(refundHistory, "Create refund history successful");
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict when creating refund history");
            return Conflict(ex.Message);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Payment not found when creating refund history");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating refund history");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund history");
            return StatusCode(500, new { Message = "An error occurred while creating refund history" });
        }
    }

    /// <summary>
    /// Update refund history status
    /// </summary>
    /// <param name="id">Refund history ID</param>
    /// <param name="request">Status update info</param>
    /// <returns>Updated refund history</returns>
    [HttpPut("{id}/status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateRefundHistoryStatus(Guid id, [FromBody] UpdateRefundHistoryStatusRequest request)
    {
        try
        {
            request.Id = id; // Ensure ID matches route parameter

            // Validate request
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Invalid request data", errors);
            }

            var refundHistory = await _refundHistoryService.UpdateStatusAsync(request);
            return Success(refundHistory, "Update refund history status successful");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Refund history not found when updating status");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when updating refund history status");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating refund history status with ID: {RefundHistoryId}", id);
            return StatusCode(500, new { Message = "An error occurred while updating refund history status" });
        }
    }

    /// <summary>
    /// Delete refund history
    /// </summary>
    /// <param name="id">Refund history ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteRefundHistory(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid refund history ID");
            }

            var result = await _refundHistoryService.DeleteAsync(id);
            if (!result)
            {
                return NotFound($"Refund history with ID {id} was not found");
            }

            return Success("Delete refund history successful");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when deleting refund history");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting refund history with ID: {RefundHistoryId}", id);
            return StatusCode(500, new { Message = "An error occurred while deleting refund history" });
        }
    }

    /// <summary>
    /// Automatically process refunds from WAITING -> PENDING
    /// </summary>
    /// <returns>Number of processed refund histories</returns>
    [HttpPost("process-waiting")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ProcessWaitingRefunds()
    {
        try
        {
            var processedCount = await _refundHistoryService.ProcessWaitingRefundsAsync();

            var responseData = new
            {
                processedCount = processedCount
            };

            var message = processedCount switch
            {
                0 => "No refund history requires processing",
                1 => "Processed 1 refund history successfully",
                _ => $"Processed {processedCount} refund histories successfully"
            };

            return Success(responseData, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing waiting refunds");
            return StatusCode(500, new { Message = "An error occurred while processing refund histories" });
        }
    }

    /// <summary>
    /// Get refund statistics by status
    /// </summary>
    /// <returns>Refund statistics</returns>
    [HttpGet("statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRefundStatistics()
    {
        try
        {
            var statistics = await _refundHistoryService.GetRefundStatisticsAsync();
            return Success(statistics, "Get refund statistics successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund statistics");
            return StatusCode(500, new { Message = "An error occurred while retrieving refund statistics" });
        }
    }

    /// <summary>
    /// Check if a payment can be refunded
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <returns>Check result</returns>
    [HttpGet("payment/{paymentId}/can-refund")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CanRefundPayment(Guid paymentId)
    {
        try
        {
            if (paymentId == Guid.Empty)
            {
                return BadRequest("Invalid payment ID");
            }

            var canRefund = await _refundHistoryService.CanRefundPaymentAsync(paymentId);

            var responseData = new
            {
                paymentId = paymentId,
                canRefund = canRefund
            };

            var message = canRefund ? "Payment can be refunded" : "Payment cannot be refunded";
            return Success(responseData, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if payment can refund: {PaymentId}", paymentId);
            return StatusCode(500, new { Message = "An error occurred while checking the payment" });
        }
    }

    /// <summary>
    /// Get refund histories by user ID only with status PENDING and COMPLETED
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of processable refund histories</returns>
    [HttpGet("user/{userId}/processable")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetProcessableRefundHistoriesByUser(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }
            var refundHistories = await _refundHistoryService.GetProcessableRefundsByUserIdAsync(userId);
            var refundHistoriesList = refundHistories.ToList();
            var count = refundHistoriesList.Count;

            var responseData = new
            {
                refundHistories = refundHistoriesList,
                count = count
            };

            var message = count switch
            {
                0 => "No processable or completed refund histories found",
                1 => "Get 1 processable/completed refund history successful",
                _ => $"Get {count} processable/completed refund histories successful"
            };

            return Success(responseData, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processable refund histories for user: {UserId}", userId);
            return StatusCode(500, new { Message = "An error occurred while retrieving processable refund histories" });
        }
    }

    /// <summary>
    /// Mark refund as transferred (completed)
    /// Updates refund status to COMPLETED, updates payment status to REFUNDED,
    /// and sends notification to patient
    /// </summary>
    /// <param name="id">Refund history ID</param>
    /// <param name="request">Transfer notes</param>
    /// <returns>Updated refund history</returns>
    [HttpPost("{id}/mark-transferred")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> MarkAsTransferred(Guid id, [FromBody] MarkAsTransferredRequest request)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid refund history ID");
            }

            // Validate request
            var validationResult = await _markTransferredValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Invalid request data", errors);
            }

            var refundHistory = await _refundHistoryService.MarkAsTransferredAsync(id, request.StaffNotes);
            return Success(refundHistory, "Refund marked as transferred successfully");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Refund history not found when marking as transferred");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when marking refund as transferred");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking refund as transferred: {RefundHistoryId}", id);
            return StatusCode(500, new { Message = "An error occurred while marking refund as transferred" });
        }
    }

    /// <summary>
    /// Report bank account issue
    /// Sends notification to patient about incorrect bank account information
    /// </summary>
    /// <param name="id">Refund history ID</param>
    /// <param name="request">Issue description</param>
    /// <returns>Success result</returns>
    [HttpPost("{id}/report-issue")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ReportBankIssue(Guid id, [FromBody] ReportBankIssueRequest request)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid refund history ID");
            }

            // Validate request
            var validationResult = await _reportIssueValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Invalid request data", errors);
            }

            await _refundHistoryService.ReportBankIssueAsync(id, request.IssueDescription);
            return Success("Bank account issue reported successfully. Patient will be notified.");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Refund history not found when reporting bank issue");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when reporting bank issue");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting bank issue for refund: {RefundHistoryId}", id);
            return StatusCode(500, new { Message = "An error occurred while reporting bank account issue" });
        }
    }
}
