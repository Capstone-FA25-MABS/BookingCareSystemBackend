namespace BookingCare.Services.Payment.Models.DTOs.PayOS;

/// <summary>
/// Request to create a PayOS payment link
/// </summary>
public class PayOSPaymentRequest
{
    /// <summary>
    /// Payment ID in the system
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Payment amount (VND)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Order description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Buyer information (optional)
    /// </summary>
    public PayOSBuyerInfo? BuyerInfo { get; set; }

    /// <summary>
    /// Product/service items information (optional)
    /// </summary>
    public List<PayOSItemInfo>? Items { get; set; }
}

/// <summary>
/// Response containing PayOS payment link
/// </summary>
public class PayOSPaymentResponse
{
    /// <summary>
    /// Checkout URL to redirect the user to PayOS
    /// </summary>
    public string CheckoutUrl { get; set; } = string.Empty;

    /// <summary>
    /// Order code from PayOS
    /// </summary>
    public long OrderCode { get; set; }

    /// <summary>
    /// QR code for payment
    /// </summary>
    public string QrCode { get; set; } = string.Empty;

    /// <summary>
    /// Transaction expiration time
    /// </summary>
    public DateTime ExpireAt { get; set; }
}

/// <summary>
/// Webhook data from PayOS
/// </summary>
public class PayOSWebhookData
{
    /// <summary>
    /// Order code
    /// </summary>
    public long OrderCode { get; set; }

    /// <summary>
    /// Amount
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Description
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
    /// Description message
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
/// Buyer information
/// </summary>
public class PayOSBuyerInfo
{
    /// <summary>
    /// Buyer name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Buyer email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Buyer phone number
    /// </summary>
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// Product/service item information
/// </summary>
public class PayOSItemInfo
{
    /// <summary>
    /// Item name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Quantity
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Price
    /// </summary>
    public int Price { get; set; }
}

/// <summary>
/// Callback response from PayOS
/// </summary>
public class PayOSCallbackResponse
{
    /// <summary>
    /// Payment ID in the system
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Whether the transaction succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Order code from PayOS
    /// </summary>
    public long OrderCode { get; set; }

    /// <summary>
    /// Amount paid
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Response code
    /// </summary>
    public string ResponseCode { get; set; } = string.Empty;

    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Payment date time
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Reference from PayOS
    /// </summary>
    public string Reference { get; set; } = string.Empty;
}