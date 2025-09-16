using BookingCare.Services.Payment.Models.DTOs.VNPay;
using BookingCare.Services.Payment.Models.Configurations;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Interface cho VNPay Service
/// </summary>
public interface IVNPayService
{
    /// <summary>
    /// Tạo URL thanh toán VNPay
    /// </summary>
    /// <param name="request">Thông tin thanh toán</param>
    /// <returns>URL để redirect đến VNPay</returns>
    Task<VNPayPaymentResponse> CreatePaymentUrlAsync(VNPayPaymentRequest request);

    /// <summary>
    /// Xử lý callback từ VNPay
    /// </summary>
    /// <param name="queryParams">Query parameters từ VNPay callback</param>
    /// <returns>Kết quả xử lý callback</returns>
    Task<VNPayCallbackResponse> ProcessCallbackAsync(Dictionary<string, string> queryParams);

    /// <summary>
    /// Validate chữ ký từ VNPay
    /// </summary>
    /// <param name="queryParams">Parameters từ VNPay</param>
    /// <param name="secureHash">Chữ ký cần validate</param>
    /// <returns>True nếu chữ ký hợp lệ</returns>
    bool ValidateSignature(Dictionary<string, string> queryParams, string secureHash);

    /// <summary>
    /// Query trạng thái giao dịch từ VNPay
    /// </summary>
    /// <param name="transactionRef">Mã giao dịch</param>
    /// <param name="transactionDate">Ngày giao dịch (yyyyMMdd)</param>
    /// <returns>Thông tin giao dịch</returns>
    Task<object> QueryTransactionAsync(string transactionRef, string transactionDate);
}