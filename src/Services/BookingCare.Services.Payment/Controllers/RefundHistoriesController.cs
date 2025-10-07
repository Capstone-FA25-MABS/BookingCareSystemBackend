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
/// Controller qu?n lý l?ch s? refund
/// </summary>
[ApiVersion(ApiVersions.V1_0)]
public class RefundHistoriesController : BaseApiController
{
    private readonly IRefundHistoryService _refundHistoryService;
    private readonly IValidator<CreateRefundHistoryRequest> _createValidator;
    private readonly IValidator<UpdateRefundHistoryStatusRequest> _updateValidator;
    private readonly IValidator<GetRefundHistoriesRequest> _getValidator;
    private readonly ILogger<RefundHistoriesController> _logger;

    public RefundHistoriesController(
        IRefundHistoryService refundHistoryService,
        IValidator<CreateRefundHistoryRequest> createValidator,
        IValidator<UpdateRefundHistoryStatusRequest> updateValidator,
        IValidator<GetRefundHistoriesRequest> getValidator,
        ILogger<RefundHistoriesController> logger)
    {
        _refundHistoryService = refundHistoryService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _getValidator = getValidator;
        _logger = logger;
    }

    /// <summary>
    /// L?y refund history theo ID
    /// </summary>
    /// <param name="id">ID c?a refund history</param>
    /// <returns>Thông tin refund history</returns>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRefundHistory(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest("ID refund history không h?p l?");
            }

            var refundHistory = await _refundHistoryService.GetByIdAsync(id);
            if (refundHistory == null)
            {
                return NotFound($"Refund history v?i ID {id} không tìm th?y");
            }

            return Success(refundHistory, "L?y refund history thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund history with ID: {RefundHistoryId}", id);
            return StatusCode(500, new { Message = "Có l?i x?y ra khi l?y refund history" });
        }
    }

    /// <summary>
    /// L?y refund history theo payment ID
    /// </summary>
    /// <param name="paymentId">ID c?a payment</param>
    /// <returns>Thông tin refund history</returns>
    [HttpGet("payment/{paymentId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRefundHistoryByPayment(Guid paymentId)
    {
        try
        {
            if (paymentId == Guid.Empty)
            {
                return BadRequest("Payment ID không h?p l?");
            }

            var refundHistory = await _refundHistoryService.GetByPaymentIdAsync(paymentId);
            if (refundHistory == null)
            {
                return NotFound($"Không tìm th?y refund history cho payment {paymentId}");
            }

            return Success(refundHistory, "L?y refund history thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund history for payment: {PaymentId}", paymentId);
            return StatusCode(500, new { Message = "Có l?i x?y ra khi l?y refund history" });
        }
    }

    /// <summary>
    /// L?y danh sách refund histories theo user ID
    /// </summary>
    /// <param name="userId">ID c?a user</param>
    /// <returns>Danh sách refund histories</returns>
    [HttpGet("user/{userId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRefundHistoriesByUser(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("User ID không h?p l?");
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
                0 => "Không tìm th?y refund history nào",
                1 => "L?y 1 refund history thành công",
                _ => $"L?y {count} refund histories thành công"
            };

            return Success(responseData, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund histories for user: {UserId}", userId);
            return StatusCode(500, new { Message = "Có l?i x?y ra khi l?y danh sách refund histories" });
        }
    }

    /// <summary>
    /// L?y danh sách refund histories theo tr?ng thái
    /// </summary>
    /// <param name="status">Tr?ng thái refund</param>
    /// <returns>Danh sách refund histories</returns>
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
                0 => $"Không tìm th?y refund history nào v?i status {status}",
                1 => $"L?y 1 refund history v?i status {status} thành công",
                _ => $"L?y {count} refund histories v?i status {status} thành công"
            };

            return Success(responseData, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund histories by status: {Status}", status);
            return StatusCode(500, new { Message = "Có l?i x?y ra khi l?y danh sách refund histories" });
        }
    }

    /// <summary>
    /// L?y danh sách refund histories v?i phân trang và filter
    /// </summary>
    /// <param name="request">Thông tin phân trang và filter</param>
    /// <returns>Danh sách refund histories có phân trang</returns>
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
                return BadRequest("D? li?u không h?p l?", errors);
            }

            var pagedResult = await _refundHistoryService.GetPagedAsync(request);
            return Paginated(pagedResult, "L?y danh sách refund histories có phân trang thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged refund histories");
            return StatusCode(500, new { Message = "Có l?i x?y ra khi l?y danh sách refund histories" });
        }
    }

    /// <summary>
    /// T?o refund history m?i
    /// </summary>
    /// <param name="request">Thông tin refund history</param>
    /// <returns>Refund history ???c t?o</returns>
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
                return BadRequest("D? li?u không h?p l?", errors);
            }

            var refundHistory = await _refundHistoryService.CreateAsync(request);
            return Created(refundHistory, "T?o refund history thành công");
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
            return StatusCode(500, new { Message = "Có l?i x?y ra khi t?o refund history" });
        }
    }

    /// <summary>
    /// C?p nh?t tr?ng thái refund history
    /// </summary>
    /// <param name="id">ID c?a refund history</param>
    /// <param name="request">Thông tin c?p nh?t tr?ng thái</param>
    /// <returns>Refund history ?ã c?p nh?t</returns>
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
                return BadRequest("D? li?u không h?p l?", errors);
            }

            var refundHistory = await _refundHistoryService.UpdateStatusAsync(request);
            return Success(refundHistory, "C?p nh?t tr?ng thái refund history thành công");
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
            return StatusCode(500, new { Message = "Có l?i x?y ra khi c?p nh?t tr?ng thái refund history" });
        }
    }

    /// <summary>
    /// Xóa refund history
    /// </summary>
    /// <param name="id">ID c?a refund history</param>
    /// <returns>K?t qu? xóa</returns>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteRefundHistory(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest("ID refund history không h?p l?");
            }

            var result = await _refundHistoryService.DeleteAsync(id);
            if (!result)
            {
                return NotFound($"Refund history v?i ID {id} không tìm th?y");
            }

            return Success("Xóa refund history thành công");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when deleting refund history");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting refund history with ID: {RefundHistoryId}", id);
            return StatusCode(500, new { Message = "Có l?i x?y ra khi xóa refund history" });
        }
    }

    /// <summary>
    /// X? lý t? ??ng các refund history WAITING -> PENDING
    /// </summary>
    /// <returns>S? l??ng refund histories ???c x? lý</returns>
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
                0 => "Không có refund history nào c?n x? lý",
                1 => "?ã x? lý 1 refund history thành công",
                _ => $"?ã x? lý {processedCount} refund histories thành công"
            };

            return Success(responseData, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing waiting refunds");
            return StatusCode(500, new { Message = "Có l?i x?y ra khi x? lý refund histories" });
        }
    }

    /// <summary>
    /// L?y th?ng kê refund theo tr?ng thái
    /// </summary>
    /// <returns>Th?ng kê refund</returns>
    [HttpGet("statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRefundStatistics()
    {
        try
        {
            var statistics = await _refundHistoryService.GetRefundStatisticsAsync();
            return Success(statistics, "L?y th?ng kê refund thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund statistics");
            return StatusCode(500, new { Message = "Có l?i x?y ra khi l?y th?ng kê refund" });
        }
    }

    /// <summary>
    /// Ki?m tra payment có th? refund không
    /// </summary>
    /// <param name="paymentId">ID c?a payment</param>
    /// <returns>K?t qu? ki?m tra</returns>
    [HttpGet("payment/{paymentId}/can-refund")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CanRefundPayment(Guid paymentId)
    {
        try
        {
            if (paymentId == Guid.Empty)
            {
                return BadRequest("Payment ID không h?p l?");
            }

            var canRefund = await _refundHistoryService.CanRefundPaymentAsync(paymentId);

            var responseData = new
            {
                paymentId = paymentId,
                canRefund = canRefund
            };

            var message = canRefund ? "Payment có th? refund" : "Payment không th? refund";
            return Success(responseData, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if payment can refund: {PaymentId}", paymentId);
            return StatusCode(500, new { Message = "Có l?i x?y ra khi ki?m tra payment" });
        }
    }

    /// <summary>
    /// L?y danh sách refund histories theo user ID ch? v?i status PENDING và COMPLETED
    /// </summary>
    /// <param name="userId">ID c?a user</param>
    /// <returns>Danh sách refund histories có th? x? lý</returns>
    [HttpGet("user/{userId}/processable")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetProcessableRefundHistoriesByUser(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("User ID không h?p l?");
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
                0 => "Không tìm th?y refund history nào ?ang x? lý ho?c ?ã hoàn thành",
                1 => "L?y 1 refund history ?ang x? lý/hoàn thành thành công",
                _ => $"L?y {count} refund histories ?ang x? lý/hoàn thành thành công"
            };

            return Success(responseData, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processable refund histories for user: {UserId}", userId);
            return StatusCode(500, new { Message = "Có l?i x?y ra khi l?y danh sách refund histories có th? x? lý" });
        }
    }
}