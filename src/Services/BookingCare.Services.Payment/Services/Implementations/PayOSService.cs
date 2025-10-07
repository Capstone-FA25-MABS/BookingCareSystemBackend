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
/// Implementation of PayOS Service
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
    /// Create PayOS payment link
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
                throw new ArgumentException("Amount must be greater than 0");

            // Check if payment exists
            var payment = await _paymentService.GetByIdAsync(request.PaymentId);
            if (payment == null)
                throw new ArgumentException($"Payment with ID {request.PaymentId} not found");

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
                // Default item if no items provided
                items.Add(new ItemData(request.Description, 1, (int)request.Amount));
            }

            // Set expiration time
            var expireAt = DateTime.Now.AddMinutes(_payOSConfig.TimeoutInMinutes);
            var expiredAt = ((DateTimeOffset)expireAt).ToUnixTimeSeconds();

            // CancelUrl should include orderCode so client knows which order was cancelled
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

            // Save mapping PaymentId -> OrderCode (for both webhook & callback to avoid race conditions)
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
    /// Handle callback from PayOS (when user returns from PayOS)
    /// </summary>
    public async Task<PayOSCallbackResponse> ProcessCallbackAsync(long orderCode, string code, bool cancel)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Processing PayOS callback - OrderCode: {OrderCode}, Code: {Code}, Cancel: {Cancel}",
                null, orderCode, code, cancel);

            // Get PaymentId from mapping (mapping is kept even if webhook processed so callback can still resolve PaymentId)
            var paymentId = await _mappingRepository.GetPaymentIdByOrderCodeAsync(orderCode);

            if (!paymentId.HasValue)
            {
                LogWarning("PayOS Callback - Cannot find PaymentId for OrderCode: {OrderCode} (mapping absent, possibly cleaned or race)",
                    null, orderCode);

                // Try to get payment info from PayOS to create response (cannot resolve PaymentId anymore)
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
                        Message = callbackSuccess ? "Payment processed successfully (mapping missing)" : "Payment processed but failed",
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
                        Message = "Transaction was processed previously (mapping missing)",
                        PaymentDate = null,
                        Reference = orderCode.ToString()
                    };

                    return fallbackResponse;
                }
            }

            // Get payment info from system
            var payment = await _paymentService.GetByIdAsync(paymentId.Value);
            if (payment == null)
            {
                LogWarning("PayOS Callback - Payment not found in system for PaymentId: {PaymentId}", null, paymentId.Value);
                throw new ArgumentException($"Payment with ID {paymentId.Value} not found");
            }

            // If payment was already processed by webhook (not PENDING) -> still return PaymentId (do not delete mapping immediately)
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
                    Message = payment.Status == PaymentStatus.COMPLETED ? "Payment completed" : "Payment failed",
                    PaymentDate = payment.Status == PaymentStatus.COMPLETED ? DateTime.UtcNow : null,
                    Reference = orderCode.ToString()
                };

                return alreadyProcessedResponse;
            }

            // Process new payment
            PaymentStatus newStatus;
            bool isSuccess;
            string message;

            if (cancel)
            {
                newStatus = PaymentStatus.FAILED;
                isSuccess = false;
                message = "Payment was cancelled by user";
            }
            else
            {
                isSuccess = code == "00";
                newStatus = isSuccess ? PaymentStatus.COMPLETED : PaymentStatus.FAILED;
                message = isSuccess ? "Payment successful" : $"Payment failed - Error code: {code}";
            }

            await _paymentService.UpdateStatusAsync(new Models.DTOs.Requests.UpdatePaymentStatusRequest
            {
                Id = paymentId.Value,
                Status = newStatus
            });

            // DO NOT delete mapping here so webhook/callback can resolve any order.
            // Mapping will be cleaned up by scheduled job based on expiresAt.

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
    /// Cleanup expired mappings
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
    /// Get payment info from PayOS
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
    /// Cancel PayOS payment link
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
                // Mapping can be kept so client can still resolve PaymentId after user returns to cancel page.
                // Do not delete mapping here; periodic cleanup will handle it.
            }

            LogInfo("PayOS payment link cancellation result - OrderCode: {OrderCode}, Success: {Success}",
                null, orderCode, success);

            return success;

        }, "CancelPaymentLinkAsync");
    }

    /// <summary>
    /// Generate unique order code for PayOS
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