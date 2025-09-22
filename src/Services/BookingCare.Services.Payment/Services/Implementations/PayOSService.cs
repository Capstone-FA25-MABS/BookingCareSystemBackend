using System.Security.Cryptography;
using System.Text;
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

            // CancelUrl cần mang orderCode để client biết user đã hủy đơn nào
            string cancelUrl = $"{_payOSConfig.CancelUrl}?orderCode={orderCode}";

            // Create PaymentData with buyer info if available
            var paymentData = new PaymentData(
                orderCode: orderCode,
                amount: (int)4000,
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

            // Lưu mapping PaymentId -> OrderCode (giữ lại cho cả webhook & callback để tránh trạng thái race)
            await _mappingRepository.CreateMappingAsync(request.PaymentId, orderCode, DateTime.UtcNow);

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

            // Lấy PaymentId từ mapping (mapping được giữ lại dù webhook đã xử lý để callback vẫn biết PaymentId)
            var paymentId = await _mappingRepository.GetPaymentIdByOrderCodeAsync(orderCode);

            if (!paymentId.HasValue)
            {
                LogWarning("PayOS Callback - Cannot find PaymentId for OrderCode: {OrderCode} (mapping absent, possibly cleaned or race)",
                    null, orderCode);

                // Thử lấy payment info từ PayOS để tạo response (không suy ra được PaymentId nữa)
                try
                {
                    var paymentInfo = await _payOS.getPaymentLinkInformation(orderCode);

                    bool callbackSuccess = !cancel && (code == "00" || paymentInfo.status == "PAID");

                    LogInfo("PayOS Callback - Retrieved info from PayOS API - OrderCode: {OrderCode}, Status: {Status}, Amount: {Amount}",
                        null, orderCode, paymentInfo.status, paymentInfo.amount);

                    var response = new PayOSCallbackResponse
                    {
                        PaymentId = Guid.Empty,
                        Success = callbackSuccess,
                        OrderCode = orderCode,
                        Amount = paymentInfo.amount,
                        ResponseCode = code ?? "00",
                        Message = callbackSuccess ? "Thanh toán đã được xử lý thành công (mapping missing)" : "Thanh toán đã được xử lý nhưng thất bại",
                        PaymentDate = callbackSuccess ? DateTime.UtcNow : null,
                        Reference = orderCode.ToString()
                    };

                    return response;
                }
                catch (Exception ex)
                {
                    LogWarning("PayOS Callback - Cannot retrieve payment info from PayOS API for OrderCode: {OrderCode}, Error: {Error}",
                        null, orderCode, ex.Message);

                    var fallbackResponse = new PayOSCallbackResponse
                    {
                        PaymentId = Guid.Empty,
                        Success = false,
                        OrderCode = orderCode,
                        Amount = 0,
                        ResponseCode = code ?? "UNKNOWN",
                        Message = "Giao dịch đã được xử lý trước đó (mapping missing)",
                        PaymentDate = null,
                        Reference = orderCode.ToString()
                    };

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

            // Nếu payment đã được xử lý bởi webhook trước (khác PENDING) -> vẫn trả PaymentId (không xóa mapping ngay)
            if (payment.Status != PaymentStatus.PENDING)
            {
                LogInfo("PayOS Callback - Payment already processed - PaymentId: {PaymentId}, Status: {Status}",
                    null, paymentId.Value, payment.Status);

                var alreadyProcessedResponse = new PayOSCallbackResponse
                {
                    PaymentId = paymentId.Value,
                    Success = payment.Status == PaymentStatus.COMPLETED,
                    OrderCode = orderCode,
                    Amount = payment.Amount,
                    ResponseCode = payment.Status == PaymentStatus.COMPLETED ? "00" : "01",
                    Message = payment.Status == PaymentStatus.COMPLETED ? "Thanh toán đã hoàn thành" : "Thanh toán đã thất bại",
                    PaymentDate = payment.Status == PaymentStatus.COMPLETED ? DateTime.UtcNow : null,
                    Reference = orderCode.ToString()
                };

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
                isSuccess = code == "00";
                newStatus = isSuccess ? PaymentStatus.COMPLETED : PaymentStatus.FAILED;
                message = isSuccess ? "Thanh toán thành công" : $"Thanh toán thất bại - Mã lỗi: {code}";
            }

            await _paymentService.UpdateStatusAsync(new Models.DTOs.Requests.UpdatePaymentStatusRequest
            {
                Id = paymentId.Value,
                Status = newStatus
            });

            // KHÔNG xóa mapping tại đây để webhook/callback có thể tra cứu bất kỳ thứ tự nào.
            // Mapping sẽ được dọn bởi job cleanup dựa trên expiresAt.

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

            return finalResponse;

        }, "ProcessCallbackAsync");
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

            if (success)
            {
                // Có thể giữ mapping để client vẫn resolve được PaymentId sau khi user trở về trang hủy.
                // Không xóa mapping ở đây; cleanup định kỳ sẽ xử lý.
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
        var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        var random = new Random().Next(1000, 9999);

        var orderCode = long.Parse($"{timestamp}{random}");

        if (orderCode.ToString().Length > 18)
        {
            orderCode = long.Parse(orderCode.ToString().Substring(0, 18));
        }

        return orderCode;
    }
}