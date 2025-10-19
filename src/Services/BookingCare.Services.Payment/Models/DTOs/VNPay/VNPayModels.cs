using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Services.Payment.Models.Interfaces;

namespace BookingCare.Services.Payment.Models.DTOs.VNPay;

/// <summary>
/// Request to create VNPay payment URL
/// </summary>
public class VNPayPaymentRequest
{
    /// <summary>
    /// Payment ID in the system
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Payment amount (VND)
    /// </summary>
    [Required]
    [JsonRequired]
    public decimal Amount { get; set; }

    /// <summary>
    /// Order description
    /// </summary>
    public string OrderDescription { get; set; } = string.Empty;

    /// <summary>
    /// Client IP address
    /// </summary>
    public string ClientIP { get; set; } = string.Empty;

    /// <summary>
    /// Customer information (optional)
    /// </summary>
    public string? CustomerInfo { get; set; }
}

/// <summary>
/// Response containing VNPay payment URL
/// </summary>
public class VNPayPaymentResponse
{
    /// <summary>
    /// URL to redirect the user to VNPay
    /// </summary>
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// Transaction reference from VNPay
    /// </summary>
    public string TransactionRef { get; set; } = string.Empty;

    /// <summary>
    /// Transaction expiration time
    /// </summary>
    public DateTime ExpireTime { get; set; }
}

/// <summary>
/// Callback response from VNPay after payment
/// </summary>
public class VNPayCallbackResponse : IPaymentCallbackResponse
{
    /// <summary>
    /// VNPay transaction reference
    /// </summary>
    public string vnp_TxnRef { get; set; } = string.Empty;

    /// <summary>
    /// Amount (multiplied by 100)
    /// </summary>
    public long vnp_Amount { get; set; }

    /// <summary>
    /// Bank code used for payment
    /// </summary>
    public string vnp_BankCode { get; set; } = string.Empty;

    /// <summary>
    /// Order information
    /// </summary>
    public string vnp_OrderInfo { get; set; } = string.Empty;

    /// <summary>
    /// Transaction result code (00 = success)
    /// </summary>
    public string vnp_ResponseCode { get; set; } = string.Empty;

    /// <summary>
    /// VNPay transaction number
    /// </summary>
    public string vnp_TransactionNo { get; set; } = string.Empty;

    /// <summary>
    /// Transaction status (00 = success)
    /// </summary>
    public string vnp_TransactionStatus { get; set; } = string.Empty;

    /// <summary>
    /// Payment time (yyyyMMddHHmmss)
    /// </summary>
    public string vnp_PayDate { get; set; } = string.Empty;

    /// <summary>
    /// Security hash signature
    /// </summary>
    public string vnp_SecureHash { get; set; } = string.Empty;

    /// <summary>
    /// TMN Code
    /// </summary>
    public string vnp_TmnCode { get; set; } = string.Empty;

    // IPaymentCallbackResponse implementation
    /// <summary>
    /// Response code from VNPay gateway
    /// </summary>
    public string ResponseCode => vnp_ResponseCode;

    /// <summary>
    /// Transaction ID from VNPay
    /// </summary>
    public string? TransactionId => vnp_TransactionNo;

    /// <summary>
    /// Checks whether the transaction is successful
    /// </summary>
    public bool IsSuccess => vnp_ResponseCode == "00" && vnp_TransactionStatus == "00";

    /// <summary>
    /// Get actual amount (divide by 100)
    /// </summary>
    public decimal GetActualAmount => (decimal)vnp_Amount / 100;

    /// <summary>
    /// Parse the payment date/time
    /// </summary>
    public DateTime? GetPaymentDateTime()
    {
        if (string.IsNullOrEmpty(vnp_PayDate) || vnp_PayDate.Length != 14)
            return null;

        if (DateTime.TryParseExact(
                vnp_PayDate,
                "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var result))
            return result;

        return null;
    }
}