using BookingCare.Services.Payment.Models.DTOs.PayOS;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Interface cho PayOS Service
/// </summary>
public interface IPayOSService
{
    /// <summary>
    /// T?o payment link PayOS
    /// </summary>
    /// <param name="request">Thông tin thanh toán</param>
    /// <returns>Payment link và thông tin thanh toán</returns>
    Task<PayOSPaymentResponse> CreatePaymentLinkAsync(PayOSPaymentRequest request);

    /// <summary>
    /// X? lý webhook t? PayOS
    /// </summary>
    /// <param name="webhookData">D? li?u webhook t? PayOS</param>
    /// <returns>K?t qu? x? lý webhook</returns>
    Task<PayOSCallbackResponse> ProcessWebhookAsync(PayOSWebhookData webhookData);

    /// <summary>
    /// Xử lý callback từ PayOS (khi user quay về từ PayOS)
    /// </summary>
    /// <param name="orderCode">Order code từ PayOS</param>
    /// <param name="code">Mã phản hồi từ PayOS</param>
    /// <param name="cancel">Có bị hủy không</param>
    /// <returns>Kết quả xử lý callback</returns>
    Task<PayOSCallbackResponse> ProcessCallbackAsync(long orderCode, string code, bool cancel);

    /// <summary>
    /// Xác nh?n webhook signature t? PayOS
    /// </summary>
    /// <param name="webhookData">D? li?u webhook</param>
    /// <param name="signature">Signature ?? xác th?c</param>
    /// <returns>True n?u signature h?p l?</returns>
    bool VerifyWebhookSignature(string webhookData, string signature);

    /// <summary>
    /// L?y thông tin payment t? PayOS
    /// </summary>
    /// <param name="orderCode">Mã ??n hàng PayOS</param>
    /// <returns>Thông tin chi ti?t payment</returns>
    Task<object> GetPaymentInfoAsync(long orderCode);

    /// <summary>
    /// H?y payment link PayOS
    /// </summary>
    /// <param name="orderCode">Mã ??n hàng PayOS</param>
    /// <param name="cancellationReason">Lý do h?y</param>
    /// <returns>K?t qu? h?y payment</returns>
    Task<bool> CancelPaymentLinkAsync(long orderCode, string cancellationReason = "");

    /// <summary>
    /// Cleanup các mapping đã hết hạn
    /// </summary>
    /// <returns>Số lượng mapping đã xóa</returns>
    Task<int> CleanupExpiredMappingsAsync();
}