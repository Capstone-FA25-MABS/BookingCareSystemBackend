using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Models.DTOs.VNPay;
using BookingCare.Shared.Common.Controllers;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller quản lý tích hợp VNPay
/// </summary>
[Route("api/[controller]")]
public class VNPayController : BaseApiController
{
    private readonly IVNPayService _vnpayService;
    private readonly IPaymentService _paymentService;
    private readonly IValidator<VNPayPaymentRequest> _validator;
    private readonly ILogger<VNPayController> _logger;

    public VNPayController(
        IVNPayService vnpayService,
        IPaymentService paymentService,
        IValidator<VNPayPaymentRequest> validator,
        ILogger<VNPayController> logger)
    {
        _vnpayService = vnpayService;
        _paymentService = paymentService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Health check cho VNPay service
    /// </summary>
    /// <returns>Status của VNPay service</returns>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Success(new
        {
            Service = "VNPay Integration",
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        }, "VNPay service is healthy");
    }

    /// <summary>
    /// Tạo URL thanh toán VNPay
    /// </summary>
    /// <param name="request">Thông tin thanh toán</param>
    /// <returns>URL để redirect đến VNPay</returns>
    [HttpPost("create-payment-url")]
    public async Task<IActionResult> CreatePaymentUrl([FromBody] VNPayPaymentRequest request)
    {
        try
        {
            // Set client IP if not provided (before validation)
            if (string.IsNullOrEmpty(request.ClientIP))
            {
                request.ClientIP = GetClientIP();
            }

            // Validate request data (now ClientIP is populated)
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

            // Create VNPay payment URL
            var response = await _vnpayService.CreatePaymentUrlAsync(request);

            return Success(response, "Tạo URL thanh toán VNPay thành công");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating VNPay payment URL");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VNPay payment URL for PaymentId: {PaymentId}", request.PaymentId);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi tạo URL thanh toán VNPay" });
        }
    }

    /// <summary>
    /// Callback từ VNPay sau khi thanh toán
    /// </summary>
    /// <returns>Kết quả xử lý callback</returns>
    [HttpGet("callback")]
    public async Task<IActionResult> VNPayCallback()
    {
        // Align with PayOS callback structure
        var requestId = Guid.NewGuid().ToString("N")[..8];
        try
        {
            // Get all query parameters
            var rawQueryParams = Request.Query.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.ToString()
            );

            _logger.LogInformation("VNPay Callback #{RequestId} received with {ParamCount} parameters", requestId, rawQueryParams.Count);

            // Process callback
            var callbackResult = await _vnpayService.ProcessCallbackAsync(rawQueryParams);

            // Extract PaymentId from TxnRef (format: PaymentId_Timestamp)
            var paymentIdStr = callbackResult.vnp_TxnRef.Split('_')[0];
            if (!Guid.TryParse(paymentIdStr, out var paymentId))
            {
                _logger.LogError("VNPay Callback #{RequestId} - Invalid PaymentId format in TxnRef: {TxnRef}", requestId, callbackResult.vnp_TxnRef);
                return BadRequest("Invalid transaction reference format");
            }

            // Update payment status based on VNPay result
            var newStatus = callbackResult.IsSuccess ? "COMPLETED" : "FAILED";
            await _paymentService.UpdateStatusAsync(new Models.DTOs.Requests.UpdatePaymentStatusRequest
            {
                Id = paymentId,
                Status = Enum.Parse<Shared.Common.Enums.PaymentStatus>(newStatus)
            });

            var message = GetVNPayResponseMessage(callbackResult.vnp_ResponseCode);

            // Unified response object similar to PayOS callback
            var unified = new
            {
                Success = callbackResult.IsSuccess,
                PaymentId = paymentId,
                OrderCode = callbackResult.vnp_TxnRef, // sử dụng TxnRef như OrderCode tương đương
                Code = callbackResult.vnp_ResponseCode,
                Amount = callbackResult.GetActualAmount,
                Message = message,
                PaymentDate = callbackResult.GetPaymentDateTime(),
                RequestId = requestId,
                ProcessedAt = DateTime.UtcNow,
                IsAlreadyProcessed = false, // VNPay flow hiện chưa đánh dấu duplicate theo cache
            };

            if (callbackResult.IsSuccess)
            {
                _logger.LogInformation("VNPay Callback #{RequestId} - Payment completed successfully for PaymentId: {PaymentId}", requestId, paymentId);
            }
            else
            {
                _logger.LogWarning("VNPay Callback #{RequestId} - Payment failed for PaymentId: {PaymentId}, ResponseCode: {ResponseCode}",
                    requestId, paymentId, callbackResult.vnp_ResponseCode);
            }

            return Success(unified, callbackResult.IsSuccess ? "Thanh toán VNPay thành công" : "Thanh toán VNPay thất bại");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "VNPay Callback #{RequestId} - Signature validation failed", requestId);
            return BadRequest("Chữ ký VNPay không hợp lệ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VNPay Callback #{RequestId} - Error processing callback", requestId);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi xử lý callback VNPay", RequestId = requestId });
        }
    }

    /// <summary>
    /// Query trạng thái giao dịch VNPay
    /// </summary>
    /// <param name="transactionRef">Mã giao dịch</param>
    /// <param name="transactionDate">Ngày giao dịch (yyyyMMdd)</param>
    /// <returns>Thông tin giao dịch</returns>
    [HttpGet("query/{transactionRef}")]
    public async Task<IActionResult> QueryTransaction(string transactionRef, [FromQuery] string transactionDate)
    {
        try
        {
            if (string.IsNullOrEmpty(transactionRef))
            {
                return BadRequest("Transaction reference không được để trống");
            }
            if (string.IsNullOrEmpty(transactionDate))
            {
                return BadRequest("Transaction date không được để trống");
            }
            var result = await _vnpayService.QueryTransactionAsync(transactionRef, transactionDate);
            return Success(result, "Query giao dịch VNPay thành công");
        }
        catch (NotImplementedException)
        {
            return BadRequest("Tính năng query giao dịch VNPay chưa được hỗ trợ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying VNPay transaction: {TxnRef}", transactionRef);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi query giao dịch VNPay" });
        }
    }

    /// <summary>
    /// Lấy IP của client
    /// </summary>
    /// <returns>Client IP address</returns>
    private string GetClientIP()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress;

        // Check for forwarded IP (behind proxy/load balancer)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var forwardedIps = Request.Headers["X-Forwarded-For"].ToString().Split(',');
            if (forwardedIps.Length > 0)
            {
                return forwardedIps[0].Trim();
            }
        }

        return ipAddress?.ToString() ?? "127.0.0.1";
    }

    /// <summary>
    /// Chuyển đổi response code thành message
    /// </summary>
    /// <param name="responseCode">VNPay response code</param>
    /// <returns>Human readable message</returns>
    private string GetVNPayResponseMessage(string responseCode) => responseCode switch
    {
        "00" => "Giao dịch thành công",
        "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
        "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
        "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
        "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
        "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
        "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP). Xin quý khách vui lòng thực hiện lại giao dịch.",
        "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
        "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
        "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
        "75" => "Ngân hàng thanh toán đang bảo trì.",
        "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định. Xin quý khách vui lòng thực hiện lại giao dịch",
        "99" => "Các lỗi khác (lỗi còn lại, không có trong danh sách mã lỗi đã liệt kê)",
        _ => "Lỗi không xác định"
    };
}