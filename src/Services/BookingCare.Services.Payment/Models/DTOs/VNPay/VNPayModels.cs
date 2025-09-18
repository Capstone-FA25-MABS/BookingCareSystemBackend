namespace BookingCare.Services.Payment.Models.DTOs.VNPay;

/// <summary>
/// Request để tạo URL thanh toán VNPay
/// </summary>
public class VNPayPaymentRequest
{
    /// <summary>
    /// ID payment trong hệ thống 
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Số tiền thanh toán (VND)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Thông tin đơn hàng/mô tả
    /// </summary>
    public string OrderDescription { get; set; } = string.Empty;

    /// <summary>
    /// IP của client
    /// </summary>
    public string ClientIP { get; set; } = string.Empty;

    /// <summary>
    /// Thông tin khách hàng (tùy chọn)
    /// </summary>
    public string? CustomerInfo { get; set; }
}

/// <summary>
/// Response chứa URL thanh toán VNPay
/// </summary>
public class VNPayPaymentResponse
{
    /// <summary>
    /// URL để redirect user đến VNPay
    /// </summary>
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// Transaction reference từ VNPay
    /// </summary>
    public string TransactionRef { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian hết hạn giao dịch
    /// </summary>
    public DateTime ExpireTime { get; set; }
}

/// <summary>
/// Callback response từ VNPay sau khi thanh toán
/// </summary>
public class VNPayCallbackResponse
{
    /// <summary>
    /// Mã giao dịch của VNPay
    /// </summary>
    public string vnp_TxnRef { get; set; } = string.Empty;

    /// <summary>
    /// Số tiền (đã nhân với 100)
    /// </summary>
    public long vnp_Amount { get; set; }

    /// <summary>
    /// Mã ngân hàng thanh toán
    /// </summary>
    public string vnp_BankCode { get; set; } = string.Empty;

    /// <summary>
    /// Thông tin đơn hàng
    /// </summary>
    public string vnp_OrderInfo { get; set; } = string.Empty;

    /// <summary>
    /// Kết quả giao dịch (00: thành công)
    /// </summary>
    public string vnp_ResponseCode { get; set; } = string.Empty;

    /// <summary>
    /// Mã giao dịch tại VNPay
    /// </summary>
    public string vnp_TransactionNo { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái giao dịch (00: thành công)
    /// </summary>
    public string vnp_TransactionStatus { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian thanh toán (yyyyMMddHHmmss)
    /// </summary>
    public string vnp_PayDate { get; set; } = string.Empty;

    /// <summary>
    /// Chữ ký bảo mật
    /// </summary>
    public string vnp_SecureHash { get; set; } = string.Empty;

    /// <summary>
    /// TMN Code
    /// </summary>
    public string vnp_TmnCode { get; set; } = string.Empty;

    /// <summary>
    /// Check xem giao dịch có thành công không
    /// </summary>
    public bool IsSuccess => vnp_ResponseCode == "00" && vnp_TransactionStatus == "00";

    /// <summary>
    /// Lấy số tiền thực tế (chia cho 100)
    /// </summary>
    public decimal GetActualAmount => (decimal)vnp_Amount / 100;

    /// <summary>
    /// Parse thời gian thanh toán
    /// </summary>
    public DateTime? GetPaymentDateTime()
    {
        if (string.IsNullOrEmpty(vnp_PayDate) || vnp_PayDate.Length != 14)
            return null;

        if (DateTime.TryParseExact(vnp_PayDate, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var result))
            return result;

        return null;
    }
}