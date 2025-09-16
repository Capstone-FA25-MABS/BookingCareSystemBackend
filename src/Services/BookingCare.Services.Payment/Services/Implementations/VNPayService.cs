using System.Security.Cryptography;
using System.Text;
using System.Net;
using Microsoft.Extensions.Options;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Models.DTOs.VNPay;
using BookingCare.Services.Payment.Models.Configurations;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Payment.Services.Implementations;

/// <summary>
/// Implementation của VNPay Service
/// </summary>
public class VNPayService : BaseService, IVNPayService
{
    private readonly VNPayConfiguration _vnpayConfig;

    public VNPayService(
        IOptions<VNPayConfiguration> vnpayConfig,
        ILogger<VNPayService> logger) : base(logger)
    {
        _vnpayConfig = vnpayConfig.Value;
        ValidateConfiguration();
    }

    /// <summary>
    /// Validate VNPay configuration
    /// </summary>
    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(_vnpayConfig.TmnCode))
            throw new InvalidOperationException("VNPay TmnCode is not configured");

        if (string.IsNullOrEmpty(_vnpayConfig.HashSecret))
            throw new InvalidOperationException("VNPay HashSecret is not configured");

        if (string.IsNullOrEmpty(_vnpayConfig.PaymentUrl))
            throw new InvalidOperationException("VNPay PaymentUrl is not configured");

        if (string.IsNullOrEmpty(_vnpayConfig.ReturnUrl))
            throw new InvalidOperationException("VNPay ReturnUrl is not configured");

        LogInfo("VNPay Configuration validated successfully - TmnCode: {TmnCode}", null, _vnpayConfig.TmnCode);
    }

    /// <summary>
    /// Tạo URL thanh toán VNPay
    /// </summary>
    public async Task<VNPayPaymentResponse> CreatePaymentUrlAsync(VNPayPaymentRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating VNPay payment URL for PaymentId: {PaymentId}", null, request.PaymentId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.PaymentId, nameof(request.PaymentId));

            if (request.Amount <= 0)
                throw new ArgumentException("Amount phải lớn hơn 0");

            // Generate transaction data
            var vietnamTime = DateTime.Now;
            var txnRef = $"{request.PaymentId}_{vietnamTime:yyyyMMddHHmmss}";
            var createDate = vietnamTime.ToString("yyyyMMddHHmmss");
            var expireDate = vietnamTime.AddMinutes(_vnpayConfig.TimeoutInMinutes).ToString("yyyyMMddHHmmss");

            // Prepare VNPay parameters
            var vnpParams = new SortedList<string, string>
            {
                {"vnp_Amount", ((long)(request.Amount * 100)).ToString()},
                {"vnp_Command", _vnpayConfig.Command},
                {"vnp_CreateDate", createDate},
                {"vnp_CurrCode", _vnpayConfig.CurrCode},
                {"vnp_ExpireDate", expireDate},
                {"vnp_IpAddr", request.ClientIP},
                {"vnp_Locale", _vnpayConfig.Locale},
                {"vnp_OrderInfo", CleanOrderInfo(request.OrderDescription)},
                {"vnp_OrderType", "other"},
                {"vnp_ReturnUrl", _vnpayConfig.ReturnUrl},
                {"vnp_TmnCode", _vnpayConfig.TmnCode},
                {"vnp_TxnRef", txnRef},
                {"vnp_Version", _vnpayConfig.Version}
            };

            // Add customer info if provided
            if (!string.IsNullOrEmpty(request.CustomerInfo))
            {
                var cleanCustomerInfo = CleanCustomerInfo(request.CustomerInfo);
                if (!string.IsNullOrEmpty(cleanCustomerInfo))
                {
                    vnpParams.Add("vnp_Bill_FirstName", cleanCustomerInfo);
                }
            }

            // Create payment URL
            var paymentUrl = CreateRequestUrl(_vnpayConfig.PaymentUrl, vnpParams, _vnpayConfig.HashSecret);

            var response = new VNPayPaymentResponse
            {
                PaymentUrl = paymentUrl,
                TransactionRef = txnRef,
                ExpireTime = vietnamTime.AddMinutes(_vnpayConfig.TimeoutInMinutes)
            };

            LogInfo("VNPay payment URL created successfully for PaymentId: {PaymentId}",
                null, request.PaymentId);

            return response;
        }, "CreateVNPayPaymentUrl");
    }

    /// <summary>
    /// Xử lý callback từ VNPay
    /// </summary>
    public async Task<VNPayCallbackResponse> ProcessCallbackAsync(Dictionary<string, string> queryParams)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Processing VNPay callback with {ParamCount} parameters", null, queryParams.Count);

            // Validation
            ValidateRequired(queryParams, nameof(queryParams));

            if (!queryParams.ContainsKey("vnp_SecureHash"))
                throw new ArgumentException("Missing vnp_SecureHash in callback");

            // Extract and validate signature
            var receivedHash = queryParams["vnp_SecureHash"];
            var paramsForValidation = queryParams
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            if (!ValidateSignature(paramsForValidation, receivedHash))
            {
                LogError(new UnauthorizedAccessException("VNPay callback signature validation failed"),
                    "Invalid VNPay signature for TxnRef: {TxnRef}", null,
                    queryParams.GetValueOrDefault("vnp_TxnRef", "Unknown"));
                throw new UnauthorizedAccessException("Invalid VNPay signature");
            }

            // Create response object
            var response = new VNPayCallbackResponse
            {
                vnp_TxnRef = queryParams.GetValueOrDefault("vnp_TxnRef", ""),
                vnp_Amount = long.TryParse(queryParams.GetValueOrDefault("vnp_Amount", "0"), out var amount) ? amount : 0,
                vnp_BankCode = queryParams.GetValueOrDefault("vnp_BankCode", ""),
                vnp_OrderInfo = queryParams.GetValueOrDefault("vnp_OrderInfo", ""),
                vnp_ResponseCode = queryParams.GetValueOrDefault("vnp_ResponseCode", ""),
                vnp_TransactionNo = queryParams.GetValueOrDefault("vnp_TransactionNo", ""),
                vnp_TransactionStatus = queryParams.GetValueOrDefault("vnp_TransactionStatus", ""),
                vnp_PayDate = queryParams.GetValueOrDefault("vnp_PayDate", ""),
                vnp_SecureHash = receivedHash,
                vnp_TmnCode = queryParams.GetValueOrDefault("vnp_TmnCode", "")
            };

            LogInfo("VNPay callback processed successfully - TxnRef: {TxnRef}, Status: {Status}",
                null, response.vnp_TxnRef, response.vnp_ResponseCode);

            return response;
        }, "ProcessVNPayCallback");
    }

    /// <summary>
    /// Validate chữ ký từ VNPay
    /// </summary>
    public bool ValidateSignature(Dictionary<string, string> queryParams, string inputHash)
    {
        try
        {
            if (string.IsNullOrEmpty(inputHash))
            {
                LogWarning("VNPay signature validation failed: inputHash is null or empty", null);
                return false;
            }

            // Create sorted parameters
            var sortedParams = new SortedList<string, string>();
            foreach (var kv in queryParams)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                    sortedParams[kv.Key] = kv.Value;
            }

            // Create raw data string
            var rawData = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));

            // Compute hash
            var computedHash = HmacSHA512(_vnpayConfig.HashSecret, rawData);

            // Compare hashes (case-insensitive)
            var isValid = inputHash.Equals(computedHash, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                LogWarning("VNPay signature mismatch - Expected: {Expected}, Got: {Actual}",
                    null, computedHash, inputHash);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            LogError(ex, "Exception occurred during VNPay signature validation", null);
            return false;
        }
    }

    /// <summary>
    /// Query trạng thái giao dịch từ VNPay (tính năng nâng cao)
    /// </summary>
    public async Task<object> QueryTransactionAsync(string transactionRef, string transactionDate)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Querying VNPay transaction: {TxnRef} for date {Date}", null, transactionRef, transactionDate);

            // TODO: Implement VNPay Query API call
            await Task.Delay(1); // Placeholder

            return new
            {
                Message = "VNPay Query Transaction API chưa được implement",
                TransactionRef = transactionRef,
                TransactionDate = transactionDate
            };
        }, "QueryVNPayTransaction");
    }

    /// <summary>
    /// Tạo URL request với signature
    /// </summary>
    private string CreateRequestUrl(string baseUrl, SortedList<string, string> requestData, string hashSecret)
    {
        var query = new StringBuilder();
        foreach (var kv in requestData)
        {
            query.Append($"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}&");
        }

        var rawData = query.ToString().TrimEnd('&');
        var secureHash = HmacSHA512(hashSecret, rawData);

        return $"{baseUrl}?{rawData}&vnp_SecureHash={secureHash}";
    }

    /// <summary>
    /// HMAC-SHA512 hash computation
    /// </summary>
    private string HmacSHA512(string key, string inputData)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Clean OrderInfo để tuân thủ VNPay format
    /// </summary>
    private string CleanOrderInfo(string orderInfo)
    {
        if (string.IsNullOrEmpty(orderInfo))
            return "Thanh toan don hang";

        // VNPay chỉ chấp nhận: a-z, A-Z, 0-9, space, dot, dash, underscore
        var cleaned = System.Text.RegularExpressions.Regex.Replace(orderInfo, @"[^a-zA-Z0-9\s\.\-_]", "");

        if (cleaned.Length > 255)
            cleaned = cleaned.Substring(0, 255);

        return string.IsNullOrWhiteSpace(cleaned) ? "Thanh toan don hang" : cleaned.Trim();
    }

    /// <summary>
    /// Clean customer info để tuân thủ VNPay format
    /// </summary>
    private string CleanCustomerInfo(string customerInfo)
    {
        if (string.IsNullOrEmpty(customerInfo))
            return "";

        // Chỉ giữ lại chữ cái và số
        var cleaned = System.Text.RegularExpressions.Regex.Replace(customerInfo, @"[^a-zA-Z0-9\s]", "");

        if (cleaned.Length > 50)
            cleaned = cleaned.Substring(0, 50);

        return cleaned.Trim();
    }
}