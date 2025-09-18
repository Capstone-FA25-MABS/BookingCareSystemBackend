namespace BookingCare.Services.Payment.Models.DTOs.PayOS;

/// <summary>
/// Request ?? t?o payment link PayOS
/// </summary>
public class PayOSPaymentRequest
{
    /// <summary>
    /// ID payment trong h? th?ng
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// S? ti?n thanh toán (VND)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Mô t? ??n hàng
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Thông tin ng??i mua (tùy ch?n)
    /// </summary>
    public PayOSBuyerInfo? BuyerInfo { get; set; }

    /// <summary>
    /// Thông tin s?n ph?m/d?ch v? (tùy ch?n)
    /// </summary>
    public List<PayOSItemInfo>? Items { get; set; }
}

/// <summary>
/// Response ch?a payment link PayOS
/// </summary>
public class PayOSPaymentResponse
{
    /// <summary>
    /// Payment link ?? redirect user ??n PayOS
    /// </summary>
    public string CheckoutUrl { get; set; } = string.Empty;

    /// <summary>
    /// Order code t? PayOS
    /// </summary>
    public long OrderCode { get; set; }

    /// <summary>
    /// QR code ?? thanh toán
    /// </summary>
    public string QrCode { get; set; } = string.Empty;

    /// <summary>
    /// Th?i gian h?t h?n giao d?ch
    /// </summary>
    public DateTime ExpireAt { get; set; }
}

/// <summary>
/// Webhook data t? PayOS
/// </summary>
public class PayOSWebhookData
{
    /// <summary>
    /// Order code
    /// </summary>
    public long OrderCode { get; set; }

    /// <summary>
    /// S? ti?n
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Mô t?
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Account number
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Reference
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// Transaction date time
    /// </summary>
    public DateTime TransactionDateTime { get; set; }

    /// <summary>
    /// Currency (VND)
    /// </summary>
    public string Currency { get; set; } = "VND";

    /// <summary>
    /// Payment link id
    /// </summary>
    public string PaymentLinkId { get; set; } = string.Empty;

    /// <summary>
    /// Code (00 = success)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Desc
    /// </summary>
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// Counter account bank id (null for QR payments)
    /// </summary>
    public string? CounterAccountBankId { get; set; }

    /// <summary>
    /// Counter account bank name (null for QR payments)
    /// </summary>
    public string? CounterAccountBankName { get; set; }

    /// <summary>
    /// Counter account name (null for QR payments)
    /// </summary>
    public string? CounterAccountName { get; set; }

    /// <summary>
    /// Counter account number (null for QR payments)
    /// </summary>
    public string? CounterAccountNumber { get; set; }

    /// <summary>
    /// Virtual account name (null for QR payments)
    /// </summary>
    public string? VirtualAccountName { get; set; }

    /// <summary>
    /// Virtual account number (null for QR payments)
    /// </summary>
    public string? VirtualAccountNumber { get; set; }
}

/// <summary>
/// Thông tin ng??i mua
/// </summary>
public class PayOSBuyerInfo
{
    /// <summary>
    /// Tên ng??i mua
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email ng??i mua
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// S? ?i?n tho?i ng??i mua
    /// </summary>
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// Thông tin s?n ph?m/d?ch v?
/// </summary>
public class PayOSItemInfo
{
    /// <summary>
    /// Tên s?n ph?m/d?ch v?
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// S? l??ng
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Giá
    /// </summary>
    public int Price { get; set; }
}

/// <summary>
/// Response callback t? PayOS
/// </summary>
public class PayOSCallbackResponse
{
    /// <summary>
    /// ID payment trong h? th?ng
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Giao d?ch có thành công không
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Order code t? PayOS
    /// </summary>
    public long OrderCode { get; set; }

    /// <summary>
    /// S? ti?n ?ã thanh toán
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Mã ph?n h?i
    /// </summary>
    public string ResponseCode { get; set; } = string.Empty;

    /// <summary>
    /// Thông ?i?p
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Th?i gian thanh toán
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Reference t? PayOS
    /// </summary>
    public string Reference { get; set; } = string.Empty;
}