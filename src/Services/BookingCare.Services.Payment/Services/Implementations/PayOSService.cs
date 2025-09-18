using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Models.DTOs.PayOS;
using BookingCare.Services.Payment.Models.Configurations;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Payment.Services.Implementations;

/// <summary>
/// Implementation của PayOS Service
/// </summary>
public class PayOSService : BaseService, IPayOSService
{
    private readonly PayOSConfiguration _payOSConfig;
    private readonly PayOS _payOS;
    private readonly IPaymentService _paymentService;
    private readonly IPayOSPaymentMappingRepository _mappingRepository;

    // In-memory cache để tránh duplicate processing (TTL: 5 minutes)
    private readonly ConcurrentDictionary<long, (DateTime ProcessedAt, PayOSCallbackResponse Response)> _processedCallbacks
        = new ConcurrentDictionary<long, (DateTime, PayOSCallbackResponse)>();

    public PayOSService(
        IOptions<PayOSConfiguration> payOSConfig,
        IPaymentService paymentService,
        IPayOSPaymentMappingRepository mappingRepository,
        ILogger<PayOSService> logger) : base(logger)
    {
        _payOSConfig = payOSConfig.Value;
        _paymentService = paymentService;
        _mappingRepository = mappingRepository;

        ValidateConfiguration();

        // Initialize PayOS client
        _payOS = new PayOS(_payOSConfig.ClientId, _payOSConfig.ApiKey, _payOSConfig.ChecksumKey);
    }

    /// <summary>
    /// Validate PayOS configuration
    /// </summary>
    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(_payOSConfig.ClientId))
            throw new InvalidOperationException("PayOS ClientId is not configured");

        if (string.IsNullOrEmpty(_payOSConfig.ApiKey))
            throw new InvalidOperationException("PayOS ApiKey is not configured");

        if (string.IsNullOrEmpty(_payOSConfig.ChecksumKey))
            throw new InvalidOperationException("PayOS ChecksumKey is not configured");

        if (string.IsNullOrEmpty(_payOSConfig.ReturnUrl))
            throw new InvalidOperationException("PayOS ReturnUrl is not configured");

        LogInfo("PayOS Configuration validated successfully - ClientId: {ClientId}", null, _payOSConfig.ClientId);
    }

    /// <summary>
    /// Tạo payment link PayOS
    /// </summary>
    public async Task<PayOSPaymentResponse> CreatePaymentLinkAsync(PayOSPaymentRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating PayOS payment link for PaymentId: {PaymentId}", null, request.PaymentId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.PaymentId, nameof(request.PaymentId));

            if (request.Amount <= 0)
                throw new ArgumentException("Amount phải lớn hơn 0");

            // Kiểm tra payment tồn tại
            var payment = await _paymentService.GetByIdAsync(request.PaymentId);
            if (payment == null)
                throw new ArgumentException($"Payment với ID {request.PaymentId} không tìm thấy");

            // Generate unique order code (timestamp + random)
            var orderCode = GenerateOrderCode();

            // Prepare items list
            var items = new List<ItemData>();

            if (request.Items?.Any() == true)
            {
                items.AddRange(request.Items.Select(item => new ItemData(item.Name, item.Quantity, item.Price)));
            }
            else
            {
                // Default item nếu không có items
                items.Add(new ItemData(request.Description, 1, (int)request.Amount));
            }

            // Set expiration time
            var expireAt = DateTime.Now.AddMinutes(_payOSConfig.TimeoutInMinutes);
            var expiredAt = ((DateTimeOffset)expireAt).ToUnixTimeSeconds();

            // Sử dụng CancelUrl từ configuration và thêm orderCode
            string cancelUrl = $"{_payOSConfig.CancelUrl}?orderCode={orderCode}";

            // Create PaymentData with buyer info if available
            var paymentData = new PaymentData(
                orderCode: orderCode,
                amount: (int)request.Amount,
                description: request.Description,
                items: items,
                returnUrl: _payOSConfig.ReturnUrl,
                cancelUrl: cancelUrl
            )
            {
                buyerName = request.BuyerInfo?.Name,
                buyerEmail = request.BuyerInfo?.Email,
                buyerPhone = request.BuyerInfo?.Phone,
                expiredAt = (int)expiredAt
            };

            // Create payment link
            var createResult = await _payOS.createPaymentLink(paymentData);

            // Lưu mapping PaymentId -> OrderCode
            await _mappingRepository.CreateMappingAsync(request.PaymentId, orderCode, expireAt);

            LogInfo("PayOS payment link created successfully - OrderCode: {OrderCode}, CheckoutUrl: {CheckoutUrl}",
                null, createResult.orderCode, createResult.checkoutUrl);

            return new PayOSPaymentResponse
            {
                CheckoutUrl = createResult.checkoutUrl,
                OrderCode = createResult.orderCode,
                QrCode = createResult.qrCode,
                ExpireAt = expireAt
            };
        }, "CreatePaymentLinkAsync");
    }

    /// <summary>
    /// Xử lý callback từ PayOS (khi user quay về từ PayOS)
    /// </summary>
    public async Task<PayOSCallbackResponse> ProcessCallbackAsync(long orderCode, string code, bool cancel)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Processing PayOS callback - OrderCode: {OrderCode}, Code: {Code}, Cancel: {Cancel}",
                null, orderCode, code, cancel);

            // Kiểm tra cache để tránh duplicate processing
            if (_processedCallbacks.TryGetValue(orderCode, out var cachedResult))
            {
                var timeSinceProcessed = DateTime.UtcNow - cachedResult.ProcessedAt;
                if (timeSinceProcessed.TotalMinutes < 5) // Cache 5 phút
                {
                    LogInfo("PayOS Callback - Returning cached result for OrderCode: {OrderCode} (processed {Minutes} minutes ago)",
                        null, orderCode, timeSinceProcessed.TotalMinutes);
                    return cachedResult.Response;
                }
                else
                {
                    // Xóa cache cũ
                    _processedCallbacks.TryRemove(orderCode, out _);
                }
            }

            // Cleanup cache cũ (chỉ chạy 10% thời gian để không ảnh hưởng performance)
            if (Random.Shared.Next(1, 11) == 1) // 10% chance
            {
                CleanupExpiredCache();
            }

            // Lấy PaymentId từ mapping
            var paymentId = await _mappingRepository.GetPaymentIdByOrderCodeAsync(orderCode);

            if (!paymentId.HasValue)
            {
                LogWarning("PayOS Callback - Cannot find PaymentId for OrderCode: {OrderCode} (likely already processed by webhook or duplicate call)",
                    null, orderCode);

                // Thử lấy payment info từ PayOS để tạo response
                try
                {
                    var paymentInfo = await _payOS.getPaymentLinkInformation(orderCode);

                    // Tạo response dựa trên thông tin từ PayOS
                    bool callbackSuccess = !cancel && (code == "00" || paymentInfo.status == "PAID");

                    LogInfo("PayOS Callback - Retrieved info from PayOS API - OrderCode: {OrderCode}, Status: {Status}, Amount: {Amount}",
                        null, orderCode, paymentInfo.status, paymentInfo.amount);

                    var response = new PayOSCallbackResponse
                    {
                        PaymentId = Guid.Empty, // Vẫn để Empty vì không tìm được
                        Success = callbackSuccess,
                        OrderCode = orderCode,
                        Amount = paymentInfo.amount,
                        ResponseCode = code ?? "00",
                        Message = callbackSuccess ? "Thanh toán đã được xử lý thành công (duplicate call)" : "Thanh toán đã được xử lý nhưng thất bại",
                        PaymentDate = callbackSuccess ? DateTime.UtcNow : null,
                        Reference = orderCode.ToString()
                    };

                    // Cache kết quả
                    _processedCallbacks.TryAdd(orderCode, (DateTime.UtcNow, response));
                    return response;
                }
                catch (Exception ex)
                {
                    LogWarning("PayOS Callback - Cannot retrieve payment info from PayOS API for OrderCode: {OrderCode}, Error: {Error}",
                        null, orderCode, ex.Message);

                    // Nếu không lấy được info từ PayOS, trả về response generic
                    var fallbackResponse = new PayOSCallbackResponse
                    {
                        PaymentId = Guid.Empty,
                        Success = false,
                        OrderCode = orderCode,
                        Amount = 0,
                        ResponseCode = code ?? "UNKNOWN",
                        Message = "Giao dịch đã được xử lý trước đó (duplicate call)",
                        PaymentDate = null,
                        Reference = orderCode.ToString()
                    };

                    // Cache kết quả fallback
                    _processedCallbacks.TryAdd(orderCode, (DateTime.UtcNow, fallbackResponse));
                    return fallbackResponse;
                }
            }

            // Lấy thông tin payment từ hệ thống
            var payment = await _paymentService.GetByIdAsync(paymentId.Value);
            if (payment == null)
            {
                LogWarning("PayOS Callback - Payment not found in system for PaymentId: {PaymentId}", null, paymentId.Value);
                throw new ArgumentException($"Payment với ID {paymentId.Value} không tìm thấy");
            }

            // Kiểm tra xem payment đã được xử lý chưa
            if (payment.Status != PaymentStatus.PENDING)
            {
                LogInfo("PayOS Callback - Payment already processed - PaymentId: {PaymentId}, Status: {Status}",
                    null, paymentId.Value, payment.Status);

                // Xóa mapping nếu còn và return response
                await _mappingRepository.DeleteMappingAsync(orderCode);

                var alreadyProcessedResponse = new PayOSCallbackResponse
                {
                    PaymentId = paymentId.Value,
                    Success = payment.Status == PaymentStatus.COMPLETED,
                    OrderCode = orderCode,
                    Amount = payment.Amount,
                    ResponseCode = payment.Status == PaymentStatus.COMPLETED ? "00" : "01",
                    Message = payment.Status == PaymentStatus.COMPLETED ? "Thanh toán đã hoàn thành (already processed)" : "Thanh toán đã thất bại",
                    PaymentDate = payment.Status == PaymentStatus.COMPLETED ? DateTime.UtcNow : null,
                    Reference = orderCode.ToString()
                };

                // Cache kết quả
                _processedCallbacks.TryAdd(orderCode, (DateTime.UtcNow, alreadyProcessedResponse));
                return alreadyProcessedResponse;
            }

            // Xử lý payment mới
            PaymentStatus newStatus;
            bool isSuccess;
            string message;

            if (cancel)
            {
                newStatus = PaymentStatus.FAILED;
                isSuccess = false;
                message = "Thanh toán đã bị hủy bởi người dùng";
            }
            else
            {
                // Kiểm tra mã phản hồi từ PayOS
                isSuccess = code == "00";
                newStatus = isSuccess ? PaymentStatus.COMPLETED : PaymentStatus.FAILED;
                message = isSuccess ? "Thanh toán thành công" : $"Thanh toán thất bại - Mã lỗi: {code}";
            }

            // Cập nhật trạng thái payment và xóa mapping
            await _paymentService.UpdateStatusAsync(new Models.DTOs.Requests.UpdatePaymentStatusRequest
            {
                Id = paymentId.Value,
                Status = newStatus
            });

            await _mappingRepository.DeleteMappingAsync(orderCode);

            LogInfo("PayOS Callback - Successfully processed - PaymentId: {PaymentId}, Status: {Status}",
                null, paymentId.Value, newStatus);

            var finalResponse = new PayOSCallbackResponse
            {
                PaymentId = paymentId.Value,
                Success = isSuccess,
                OrderCode = orderCode,
                Amount = payment.Amount,
                ResponseCode = code,
                Message = message,
                PaymentDate = isSuccess ? DateTime.UtcNow : null,
                Reference = orderCode.ToString()
            };

            // Cache kết quả thành công
            _processedCallbacks.TryAdd(orderCode, (DateTime.UtcNow, finalResponse));
            return finalResponse;

        }, "ProcessCallbackAsync");
    }

    /// <summary>
    /// Cleanup cache entries cũ hơn 10 phút
    /// </summary>
    private void CleanupExpiredCache()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        var expiredKeys = _processedCallbacks
            .Where(kvp => kvp.Value.ProcessedAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _processedCallbacks.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            LogDebug("Cleaned up {Count} expired cache entries", null, expiredKeys.Count);
        }
    }

    /// <summary>
    /// Xử lý webhook từ PayOS
    /// </summary>
    public async Task<PayOSCallbackResponse> ProcessWebhookAsync(PayOSWebhookData webhookData)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Processing PayOS webhook for OrderCode: {OrderCode}", null, webhookData.OrderCode);

            // Lấy PaymentId từ mapping
            var paymentId = await _mappingRepository.GetPaymentIdByOrderCodeAsync(webhookData.OrderCode);
            if (!paymentId.HasValue)
            {
                LogWarning("Cannot find PaymentId for OrderCode: {OrderCode}", null, webhookData.OrderCode);
                throw new ArgumentException($"Cannot find PaymentId for OrderCode: {webhookData.OrderCode}");
            }

            // Lấy thông tin payment
            var payment = await _paymentService.GetByIdAsync(paymentId.Value);
            if (payment == null)
            {
                LogWarning("Payment not found for PaymentId: {PaymentId}", null, paymentId.Value);
                throw new ArgumentException($"Payment với ID {paymentId.Value} không tìm thấy");
            }

            // Xác định trạng thái thanh toán
            var isSuccess = webhookData.Code == "00";
            var paymentStatus = isSuccess ? PaymentStatus.COMPLETED : PaymentStatus.FAILED;

            // Cập nhật trạng thái payment
            await _paymentService.UpdateStatusAsync(new Models.DTOs.Requests.UpdatePaymentStatusRequest
            {
                Id = paymentId.Value,
                Status = paymentStatus
            });

            // Xóa mapping sau khi đã xử lý xong để tiết kiệm dung lượng
            await _mappingRepository.DeleteMappingAsync(webhookData.OrderCode);

            LogInfo("PayOS webhook processed successfully - PaymentId: {PaymentId}, Status: {Status}",
                null, paymentId.Value, paymentStatus);

            return new PayOSCallbackResponse
            {
                PaymentId = paymentId.Value,
                Success = isSuccess,
                OrderCode = webhookData.OrderCode,
                Amount = webhookData.Amount,
                ResponseCode = webhookData.Code,
                Message = webhookData.Desc,
                PaymentDate = isSuccess ? webhookData.TransactionDateTime : null,
                Reference = webhookData.Reference
            };

        }, "ProcessWebhookAsync");
    }

    /// <summary>
    /// Cleanup các mapping đã hết hạn
    /// </summary>
    public async Task<int> CleanupExpiredMappingsAsync()
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Starting cleanup of expired PayOS mappings", null);

            var deletedCount = await _mappingRepository.CleanupExpiredMappingsAsync();

            LogInfo("Cleaned up {DeletedCount} expired PayOS mappings", null, deletedCount);
            return deletedCount;

        }, "CleanupExpiredMappingsAsync");
    }

    /// <summary>
    /// Xác nhận webhook signature từ PayOS
    /// </summary>
    public bool VerifyWebhookSignature(string webhookData, string signature)
    {
        try
        {
            LogInfo("Verifying PayOS webhook signature", null);

            // PayOS sử dụng HMAC-SHA256 để tạo signature
            var key = Encoding.UTF8.GetBytes(_payOSConfig.ChecksumKey);
            var data = Encoding.UTF8.GetBytes(webhookData);

            using (var hmac = new HMACSHA256(key))
            {
                var computedSignature = Convert.ToHexString(hmac.ComputeHash(data)).ToLower();
                var isValid = computedSignature.Equals(signature.ToLower(), StringComparison.OrdinalIgnoreCase);

                LogInfo("PayOS webhook signature verification result: {IsValid}", null, isValid);
                return isValid;
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error verifying PayOS webhook signature: {Error}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Lấy thông tin payment từ PayOS
    /// </summary>
    public async Task<object> GetPaymentInfoAsync(long orderCode)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting PayOS payment info for OrderCode: {OrderCode}", null, orderCode);

            var paymentInfo = await _payOS.getPaymentLinkInformation(orderCode);

            LogInfo("PayOS payment info retrieved successfully - OrderCode: {OrderCode}, Status: {Status}",
                null, orderCode, paymentInfo.status);

            return paymentInfo;

        }, "GetPaymentInfoAsync");
    }

    /// <summary>
    /// Hủy payment link PayOS
    /// </summary>
    public async Task<bool> CancelPaymentLinkAsync(long orderCode, string cancellationReason = "")
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Cancelling PayOS payment link - OrderCode: {OrderCode}, Reason: {Reason}",
                null, orderCode, cancellationReason);

            // Use the correct PayOS API method for canceling payment link
            var cancelResult = await _payOS.cancelPaymentLink(orderCode, string.IsNullOrEmpty(cancellationReason) ? "Cancelled by user" : cancellationReason);

            var success = cancelResult != null;

            // Nếu hủy thành công, xóa mapping
            if (success)
            {
                await _mappingRepository.DeleteMappingAsync(orderCode);
            }

            LogInfo("PayOS payment link cancellation result - OrderCode: {OrderCode}, Success: {Success}",
                null, orderCode, success);

            return success;

        }, "CancelPaymentLinkAsync");
    }

    /// <summary>
    /// Generate unique order code cho PayOS
    /// </summary>
    private long GenerateOrderCode()
    {
        // PayOS yêu cầu order code là số nguyên dương, không quá 18 digits
        var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        var random = new Random().Next(1000, 9999);

        var orderCode = long.Parse($"{timestamp}{random}");

        // Đảm bảo không vượt quá giới hạn của PayOS (max 18 digits)
        if (orderCode.ToString().Length > 18)
        {
            orderCode = long.Parse(orderCode.ToString().Substring(0, 18));
        }

        return orderCode;
    }
}