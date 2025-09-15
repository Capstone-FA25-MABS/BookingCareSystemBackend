namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO cho th?ng kê payments
/// </summary>
public class PaymentStatisticsResponse
{
    /// <summary>
    /// D? li?u th?ng kê theo th?i gian
    /// </summary>
    public List<PaymentTimeSeriesData> TimeSeries { get; set; } = new();

    /// <summary>
    /// Th?ng kê t?ng quan
    /// </summary>
    public PaymentSummaryStatistics Summary { get; set; } = new();

    /// <summary>
    /// Th?ng kê theo ph??ng th?c thanh toán
    /// </summary>
    public List<PaymentMethodStatistics> PaymentMethodBreakdown { get; set; } = new();

    /// <summary>
    /// Th?ng kê theo tr?ng thái
    /// </summary>
    public List<PaymentStatusStatistics> StatusBreakdown { get; set; } = new();

    /// <summary>
    /// Th?ng kê theo lo?i giao d?ch
    /// </summary>
    public List<TransactionTypeStatistics> TransactionTypeBreakdown { get; set; } = new();

    /// <summary>
    /// Chu k? th?ng kê ???c s? d?ng
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Kho?ng th?i gian th?ng kê
    /// </summary>
    public string DateRange { get; set; } = string.Empty;
}

/// <summary>
/// D? li?u th?ng kê theo chu?i th?i gian
/// </summary>
public class PaymentTimeSeriesData
{
    /// <summary>
    /// Nhãn th?i gian (ví d?: "2024-01", "2024-Q1", "2024-01-15")
    /// </summary>
    public string TimeLabel { get; set; } = string.Empty;

    /// <summary>
    /// Ngày b?t ??u c?a chu k?
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Ngày k?t thúc c?a chu k?
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// T?ng s? l??ng payments
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// T?ng s? ti?n
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// S? l??ng payments thành công
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// S? ti?n t? payments thành công
    /// </summary>
    public decimal CompletedAmount { get; set; }

    /// <summary>
    /// S? l??ng payments ?ang pending
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// S? l??ng payments th?t b?i
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// S? l??ng payments hoàn ti?n
    /// </summary>
    public int RefundedCount { get; set; }

    /// <summary>
    /// Giá tr? trung bình m?i payment
    /// </summary>
    public decimal AverageAmount { get; set; }
}

/// <summary>
/// Th?ng kê t?ng quan
/// </summary>
public class PaymentSummaryStatistics
{
    /// <summary>
    /// T?ng s? l??ng payments
    /// </summary>
    public int TotalPayments { get; set; }

    /// <summary>
    /// T?ng s? ti?n
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// T?ng s? ti?n thành công
    /// </summary>
    public decimal TotalCompletedAmount { get; set; }

    /// <summary>
    /// T? l? thành công (%)
    /// </summary>
    public decimal SuccessRate { get; set; }

    /// <summary>
    /// Giá tr? trung bình m?i payment
    /// </summary>
    public decimal AveragePaymentAmount { get; set; }

    /// <summary>
    /// Payment cao nh?t
    /// </summary>
    public decimal MaxPaymentAmount { get; set; }

    /// <summary>
    /// Payment th?p nh?t
    /// </summary>
    public decimal MinPaymentAmount { get; set; }

    /// <summary>
    /// S? l??ng payments m?i ngày (trung bình)
    /// </summary>
    public decimal AveragePaymentsPerDay { get; set; }

    /// <summary>
    /// T?ng tr??ng so v?i k? tr??c (%)
    /// </summary>
    public decimal GrowthRate { get; set; }
}

/// <summary>
/// Th?ng kê theo ph??ng th?c thanh toán
/// </summary>
public class PaymentMethodStatistics
{
    /// <summary>
    /// ID ph??ng th?c thanh toán
    /// </summary>
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Tên ph??ng th?c thanh toán
    /// </summary>
    public string PaymentMethodName { get; set; } = string.Empty;

    /// <summary>
    /// S? l??ng payments
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// T?ng s? ti?n
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// T? l? so v?i t?ng (%)
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Giá tr? trung bình
    /// </summary>
    public decimal AverageAmount { get; set; }
}

/// <summary>
/// Th?ng kê theo tr?ng thái payment
/// </summary>
public class PaymentStatusStatistics
{
    /// <summary>
    /// Tr?ng thái
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// S? l??ng
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// T?ng s? ti?n
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// T? l? (%)
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Th?ng kê theo lo?i giao d?ch
/// </summary>
public class TransactionTypeStatistics
{
    /// <summary>
    /// Lo?i giao d?ch
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    /// S? l??ng
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// T?ng s? ti?n
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// T? l? (%)
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Giá tr? trung bình
    /// </summary>
    public decimal AverageAmount { get; set; }
}