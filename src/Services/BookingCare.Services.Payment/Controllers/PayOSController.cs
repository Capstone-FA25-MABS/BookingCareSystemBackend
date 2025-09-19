using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Models.DTOs.PayOS;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Controllers;
using System.Text.Json;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller quản lý tích hợp PayOS
/// </summary>
[Route("api/[controller]")]
public class PayOSController : BaseApiController
{
    private readonly IPayOSService _payOSService;
    private readonly IPaymentService _paymentService;
    private readonly IValidator<PayOSPaymentRequest> _validator;
    private readonly ILogger<PayOSController> _logger;

    public PayOSController(
        IPayOSService payOSService,
        IPaymentService paymentService,
        IValidator<PayOSPaymentRequest> validator,
        ILogger<PayOSController> logger)
    {
        _payOSService = payOSService;
        _paymentService = paymentService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Health check cho PayOS service
    /// </summary>
    /// <returns>Status của PayOS service</returns>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Success(new
        {
            Service = "PayOS Integration",
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        }, "PayOS service is healthy");
    }

    /// <summary>
    /// Tạo payment link PayOS
    /// </summary>
    /// <param name="request">Thông tin thanh toán</param>
    /// <returns>Payment link PayOS</returns>
    [HttpPost("create-payment-link")]
    public async Task<IActionResult> CreatePaymentLink([FromBody] PayOSPaymentRequest request)
    {
        try
        {
            // Validate request data
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Dữ liệu không hợp lệ", errors);
            }

            // Validate payment exists
            var payment = await _paymentService.GetByIdAsync(request.PaymentId);
            if (payment == null)
            {
                return NotFound($"Payment với ID {request.PaymentId} không tìm thấy");
            }

            // Validate amount matches
            if (Math.Abs(payment.Amount - request.Amount) > 0.01m)
            {
                return BadRequest("Số tiền không khớp với payment trong hệ thống");
            }

            // Validate payment status
            if (payment.Status != PaymentStatus.PENDING)
            {
                return BadRequest($"Payment đã được xử lý với trạng thái: {payment.Status}");
            }

            // Create PayOS payment link
            var payOSResponse = await _payOSService.CreatePaymentLinkAsync(request);

            _logger.LogInformation("PayOS payment link created successfully for PaymentId: {PaymentId}, OrderCode: {OrderCode}",
                request.PaymentId, payOSResponse.OrderCode);

            return Success(payOSResponse, "Tạo payment link PayOS thành công");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("PayOS payment creation failed - Invalid argument: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS payment creation failed for PaymentId: {PaymentId}", request.PaymentId);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi tạo payment link PayOS" });
        }
    }

    /// <summary>
    /// Webhook endpoint để nhận thông báo từ PayOS
    /// </summary>
    /// <param name="webhookData">Dữ liệu webhook từ PayOS</param>
    /// <returns>Kết quả xử lý webhook</returns>
    [HttpPost("webhook")]
    public async Task<IActionResult> PayOSWebhook([FromBody] PayOSWebhookData webhookData)
    {
        try
        {
            _logger.LogInformation("Received PayOS webhook for OrderCode: {OrderCode}", webhookData.OrderCode);

            // Verify signature if provided
            if (Request.Headers.ContainsKey("X-PayOS-Signature"))
            {
                var signature = Request.Headers["X-PayOS-Signature"].FirstOrDefault();
                var webhookBody = await new StreamReader(Request.Body).ReadToEndAsync();

                if (!_payOSService.VerifyWebhookSignature(webhookBody, signature))
                {
                    _logger.LogWarning("PayOS webhook signature verification failed for OrderCode: {OrderCode}", webhookData.OrderCode);
                    return BadRequest("Invalid signature");
                }
            }

            // Process webhook
            var result = await _payOSService.ProcessWebhookAsync(webhookData);

            _logger.LogInformation("PayOS webhook processed successfully - PaymentId: {PaymentId}, Success: {Success}",
                result.PaymentId, result.Success);

            return Success(result, "Webhook PayOS xử lý thành công");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("PayOS webhook processing failed - Invalid argument: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS webhook processing failed for OrderCode: {OrderCode}", webhookData.OrderCode);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi xử lý webhook PayOS" });
        }
    }

    /// <summary>
    /// Callback endpoint để nhận user quay về từ PayOS (success/cancel)
    /// </summary>
    /// <returns>Kết quả thanh toán</returns>
    [HttpGet("callback")]
    public async Task<IActionResult> PayOSCallback([FromQuery] string code, [FromQuery] string id, [FromQuery] bool cancel, [FromQuery] string orderCode)
    {
        // Generate request ID để tracking duplicate calls
        var requestId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            _logger.LogInformation("PayOS Callback #{RequestId} - Code: {Code}, Id: {Id}, Cancel: {Cancel}, OrderCode: {OrderCode}",
                requestId, code, id, cancel, orderCode);

            // Validate orderCode parameter
            if (string.IsNullOrEmpty(orderCode))
            {
                _logger.LogWarning("PayOS Callback #{RequestId} - OrderCode is missing", requestId);
                return BadRequest("OrderCode parameter is required");
            }

            if (!long.TryParse(orderCode, out var orderCodeLong))
            {
                _logger.LogWarning("PayOS Callback #{RequestId} - Invalid OrderCode format: {OrderCode}", requestId, orderCode);
                return BadRequest("Invalid OrderCode format");
            }

            // Xử lý callback thông qua PayOSService với tracking
            _logger.LogInformation("PayOS Callback #{RequestId} - Processing callback for OrderCode: {OrderCode}", requestId, orderCodeLong);

            var result = await _payOSService.ProcessCallbackAsync(orderCodeLong, code ?? string.Empty, cancel);

            // Enhanced logging with request tracking
            _logger.LogInformation("PayOS Callback #{RequestId} - Processed successfully - PaymentId: {PaymentId}, Success: {Success}, IsEmptyGuid: {IsEmptyGuid}",
                requestId, result.PaymentId, result.Success, result.PaymentId == Guid.Empty);

            // Tạo response với thông tin chi tiết
            var response = new
            {
                Success = result.Success,
                PaymentId = result.PaymentId,
                OrderCode = result.OrderCode,
                Code = result.ResponseCode,
                Amount = result.Amount,
                Message = result.Message,
                PaymentDate = result.PaymentDate,
                RequestId = requestId, // Để tracking
                ProcessedAt = DateTime.UtcNow,
                IsAlreadyProcessed = result.PaymentId == Guid.Empty // Đủ rồi, không cần IsDuplicateCall và OriginalPaymentId
            };

            return Success(response, result.Success ? "Thanh toán PayOS thành công" : "Thanh toán PayOS thất bại");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("PayOS Callback #{RequestId} - Processing failed - Invalid argument: {Error}", requestId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS Callback #{RequestId} - Processing failed - Code: {Code}, OrderCode: {OrderCode}",
                requestId, code, orderCode);
            return StatusCode(500, new
            {
                Message = "Có lỗi xảy ra khi xử lý callback PayOS",
                RequestId = requestId
            });
        }
    }

    /// <summary>
    /// Lấy thông tin payment từ PayOS
    /// </summary>
    /// <param name="orderCode">Mã đơn hàng PayOS</param>
    /// <returns>Thông tin chi tiết payment</returns>
    [HttpGet("payment-info/{orderCode}")]
    public async Task<IActionResult> GetPaymentInfo(long orderCode)
    {
        try
        {
            var paymentInfo = await _payOSService.GetPaymentInfoAsync(orderCode);

            return Success(paymentInfo, "Lấy thông tin payment PayOS thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PayOS payment info for OrderCode: {OrderCode}", orderCode);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy thông tin payment PayOS" });
        }
    }


    /// <summary>
    /// Manual cleanup các PayOS mapping đã hết hạn
    /// </summary>
    /// <returns>Số lượng mapping đã xóa</returns>
    [HttpPost("cleanup-mappings")]
    public async Task<IActionResult> CleanupMappings()
    {
        try
        {
            _logger.LogInformation("Manual PayOS mapping cleanup requested");

            var deletedCount = await _payOSService.CleanupExpiredMappingsAsync();

            _logger.LogInformation("Manual PayOS mapping cleanup completed - Deleted {DeletedCount} mappings", deletedCount);

            return Success(new
            {
                DeletedCount = deletedCount,
                CleanupTime = DateTime.UtcNow,
                Message = $"Đã xóa {deletedCount} mapping hết hạn"
            }, "Cleanup PayOS mappings thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup PayOS mappings");
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi cleanup PayOS mappings" });
        }
    }

    /// <summary>
    /// Cancel callback endpoint để nhận user hủy thanh toán từ PayOS
    /// </summary>
    /// <returns>Kết quả hủy thanh toán</returns>
    [HttpGet("cancel-callback")]
    public async Task<IActionResult> PayOSCancelCallback([FromQuery] string orderCode)
    {
        try
        {
            _logger.LogInformation("Received PayOS cancel callback - OrderCode: {OrderCode}", orderCode);

            if (!long.TryParse(orderCode, out var orderCodeLong))
            {
                return BadRequest("Invalid order code format");
            }

            // Xử lý cancel callback
            var result = await _payOSService.ProcessCallbackAsync(orderCodeLong, "CANCELLED", true);

            _logger.LogInformation("PayOS cancel callback processed successfully - PaymentId: {PaymentId}",
                result.PaymentId);

            return Success(new
            {
                Success = false,
                PaymentId = result.PaymentId,
                OrderCode = result.OrderCode,
                Message = "Thanh toán đã bị hủy",
                CancelledAt = DateTime.UtcNow
            }, "Thanh toán PayOS đã bị hủy");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("PayOS cancel callback processing failed - Invalid argument: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS cancel callback processing failed - OrderCode: {OrderCode}", orderCode);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi xử lý cancel callback PayOS" });
        }
    }
}