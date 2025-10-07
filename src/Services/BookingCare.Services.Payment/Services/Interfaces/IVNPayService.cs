using BookingCare.Services.Payment.Models.DTOs.VNPay;
using BookingCare.Services.Payment.Models.Configurations;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Interface for VNPay Service
/// </summary>
public interface IVNPayService
{
    /// <summary>
    /// Create VNPay payment URL
    /// </summary>
    /// <param name="request">Payment information</param>
    /// <returns>URL to redirect to VNPay</returns>
    Task<VNPayPaymentResponse> CreatePaymentUrlAsync(VNPayPaymentRequest request);

    /// <summary>
    /// Handle callback from VNPay
    /// </summary>
    /// <param name="queryParams">Query parameters from VNPay callback</param>
    /// <returns>Callback handling result</returns>
    Task<VNPayCallbackResponse> ProcessCallbackAsync(Dictionary<string, string> queryParams);

    /// <summary>
    /// Validate signature from VNPay
    /// </summary>
    /// <param name="queryParams">Parameters from VNPay</param>
    /// <param name="secureHash">Signature to validate</param>
    /// <returns>True if signature is valid</returns>
    bool ValidateSignature(Dictionary<string, string> queryParams, string secureHash);

    /// <summary>
    /// Query transaction status from VNPay
    /// </summary>
    /// <param name="transactionRef">Transaction reference</param>
    /// <param name="transactionDate">Transaction date (yyyyMMdd)</param>
    /// <returns>Transaction information</returns>
    Task<object> QueryTransactionAsync(string transactionRef, string transactionDate);
}