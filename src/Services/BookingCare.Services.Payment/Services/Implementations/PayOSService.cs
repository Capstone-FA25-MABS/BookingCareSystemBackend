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

            // Get PaymentId from mapping
            var paymentId = await _mappingRepository.GetPaymentIdByOrderCodeAsync(orderCode);

            // Handle case when mapping is not found
            if (!paymentId.HasValue)
            {
                return await HandleMissingMappingAsync(orderCode, code, cancel);
            }

            // Get payment info from system
            var payment = await GetPaymentByIdAsync(paymentId.Value);

            // Handle case when payment was already processed
            if (payment.Status != PaymentStatus.PENDING)
            {
                return CreateAlreadyProcessedResponse(paymentId.Value, payment, orderCode);
            }

            // Process new payment
            return await ProcessNewPaymentAsync(paymentId.Value, payment, orderCode, code, cancel);

        }, "ProcessCallbackAsync");
    }

    /// <summary>
    /// Handle callback when mapping is missing
    /// </summary>
    private async Task<PayOSCallbackResponse> HandleMissingMappingAsync(long orderCode, string code, bool cancel)
    {
        LogWarning("PayOS Callback - Cannot find PaymentId for OrderCode: {OrderCode} (mapping absent)", null, orderCode);

        try
        {
            var paymentInfo = await _payOS.getPaymentLinkInformation(orderCode);
            return CreateFallbackResponse(orderCode, code, cancel, paymentInfo);
        }
        catch (Exception ex)
        {
            LogWarning("PayOS Callback - Cannot retrieve payment info from PayOS API for OrderCode: {OrderCode}, Error: {Error}",
                null, orderCode, ex.Message);
            return CreateErrorFallbackResponse(orderCode, code);
        }
    }

    /// <summary>
    /// Create fallback response when PayOS API is accessible but mapping is missing
    /// </summary>
    private PayOSCallbackResponse CreateFallbackResponse(long orderCode, string code, bool cancel, dynamic paymentInfo)
    {
        bool callbackSuccess = !cancel && (code == "00" || paymentInfo.status == "PAID");

        LogInfo("PayOS Callback - Retrieved info from PayOS API - OrderCode: {OrderCode}, Status: {Status}, Amount: {Amount}",
            null, orderCode, paymentInfo.status, paymentInfo.amount);

        return new PayOSCallbackResponse
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
    }

    /// <summary>
    /// Create error fallback response when both mapping and PayOS API are unavailable
    /// </summary>
    private PayOSCallbackResponse CreateErrorFallbackResponse(long orderCode, string code)
    {
        return new PayOSCallbackResponse
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
    }

    /// <summary>
    /// Get payment by ID with validation
    /// </summary>
    private async Task<Models.DTOs.Responses.PaymentResponse> GetPaymentByIdAsync(Guid paymentId)
    {
        var payment = await _paymentService.GetByIdAsync(paymentId);
        if (payment == null)
        {
            LogWarning("PayOS Callback - Payment not found in system for PaymentId: {PaymentId}", null, paymentId);
            throw new ArgumentException($"Payment with ID {paymentId} not found");
        }
        return payment;
    }

    /// <summary>
    /// Create response for already processed payments
    /// </summary>
    private PayOSCallbackResponse CreateAlreadyProcessedResponse(Guid paymentId, Models.DTOs.Responses.PaymentResponse payment, long orderCode)
    {
        LogInfo("PayOS Callback - Payment already processed - PaymentId: {PaymentId}, Status: {Status}",
            null, paymentId, payment.Status);

        return new PayOSCallbackResponse
        {
            PaymentId = paymentId,
            Success = payment.Status == PaymentStatus.COMPLETED,
            OrderCode = orderCode,
            Amount = payment.Amount,
            ResponseCode = payment.Status == PaymentStatus.COMPLETED ? "00" : "01",
            Message = payment.Status == PaymentStatus.COMPLETED ? "Payment completed" : "Payment failed",
            PaymentDate = payment.Status == PaymentStatus.COMPLETED ? DateTime.UtcNow : null,
            Reference = orderCode.ToString()
        };
    }

    /// <summary>
    /// Process new payment callback
    /// </summary>
    private async Task<PayOSCallbackResponse> ProcessNewPaymentAsync(Guid paymentId, Models.DTOs.Responses.PaymentResponse payment, long orderCode, string code, bool cancel)
    {
        // Determine payment status and success
        var (newStatus, isSuccess, message) = DeterminePaymentOutcome(code, cancel);

        // Update payment status
        await _paymentService.UpdateStatusAsync(new Models.DTOs.Requests.UpdatePaymentStatusRequest
        {
            Id = paymentId,
            Status = newStatus
        });

        LogInfo("PayOS Callback - Successfully processed - PaymentId: {PaymentId}, Status: {Status}",
            null, paymentId, newStatus);

        return new PayOSCallbackResponse
        {
            PaymentId = paymentId,
            Success = isSuccess,
            OrderCode = orderCode,
            Amount = payment.Amount,
            ResponseCode = code,
            Message = message,
            PaymentDate = isSuccess ? DateTime.UtcNow : null,
            Reference = orderCode.ToString()
        };
    }

    /// <summary>
    /// Determine payment outcome based on code and cancel flag
    /// </summary>
    private (PaymentStatus Status, bool IsSuccess, string Message) DeterminePaymentOutcome(string code, bool cancel)
    {
        if (cancel)
        {
            return (PaymentStatus.FAILED, false, "Payment was cancelled by user");
        }

        bool isSuccess = code == "00";
        var status = isSuccess ? PaymentStatus.COMPLETED : PaymentStatus.FAILED;
        var message = isSuccess ? "Payment successful" : $"Payment failed - Error code: {code}";

        return (status, isSuccess, message);
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
        // Generate a cryptographically secure random number
        using var rng = RandomNumberGenerator.Create();
        byte[] randomNumber = new byte[4];
        rng.GetBytes(randomNumber);
        var random = BitConverter.ToUInt32(randomNumber) % 9000 + 1000; // Get a random number between 1000 and 9999

        var orderCode = long.Parse($"{timestamp}{random}");

        if (orderCode.ToString().Length > 18)
        {
            orderCode = long.Parse(orderCode.ToString().Substring(0, 18));
        }

        return orderCode;
    }
}