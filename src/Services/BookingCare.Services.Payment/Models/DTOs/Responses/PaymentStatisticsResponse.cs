namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO cho thống kê payments
/// </summary>
public class PaymentStatisticsResponse
{
    /// <summary>
    /// Dữ liệu thống kê theo thời gian
    /// </summary>
    public List<PaymentTimeSeriesData> TimeSeries { get; set; } = new();

    /// <summary>
    /// Thống kê tổng quan
    /// </summary>
    public PaymentSummaryStatistics Summary { get; set; } = new();

    /// <summary>
    /// Thống kê theo phương thức thanh toán
    /// </summary>
    public List<PaymentMethodStatistics> PaymentMethodBreakdown { get; set; } = new();

    /// <summary>
    /// Thống kê theo trạng thái
    /// </summary>
    public List<PaymentStatusStatistics> StatusBreakdown { get; set; } = new();

    /// <summary>
    /// Thống kê theo loại giao dịch
    /// </summary>
    public List<TransactionTypeStatistics> TransactionTypeBreakdown { get; set; } = new();

    /// <summary>
    /// Chu kỳ thống kê được sử dụng
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Khoảng thời gian thống kê
    /// </summary>
    public string DateRange { get; set; } = string.Empty;
}

/// <summary>
/// Dữ liệu thống kê theo chuỗi thời gian
/// </summary>
public class PaymentTimeSeriesData
{
    /// <summary>
    /// Nhãn thời gian (ví dụ: "2024-01", "2024-Q1", "2024-01-15")
    /// </summary>
    public string TimeLabel { get; set; } = string.Empty;

    /// <summary>
    /// Ngày bắt đầu của chu kỳ
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Ngày kết thúc của chu kỳ
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Tổng số lượng payments
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Tổng số tiền
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Số lượng payments thành công
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// Số tiền từ payments thành công
    /// </summary>
    public decimal CompletedAmount { get; set; }

    /// <summary>
    /// Số lượng payments đang pending
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// Số lượng payments thất bại
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Số lượng payments hoàn tiền
    /// </summary>
    public int RefundedCount { get; set; }

    /// <summary>
    /// Giá trị trung bình mỗi payment
    /// </summary>
    public decimal AverageAmount { get; set; }
}

/// <summary>
/// Thống kê tổng quan
/// </summary>
public class PaymentSummaryStatistics
{
    /// <summary>
    /// Tổng số lượng payments
    /// </summary>
    public int TotalPayments { get; set; }

    /// <summary>
    /// Tổng số tiền
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Tổng số tiền thành công
    /// </summary>
    public decimal TotalCompletedAmount { get; set; }

    /// <summary>
    /// Tỉ lệ thành công (%)
    /// </summary>
    public decimal SuccessRate { get; set; }

    /// <summary>
    /// Giá trị trung bình mỗi payment
    /// </summary>
    public decimal AveragePaymentAmount { get; set; }

    /// <summary>
    /// Payment cao nhất
    /// </summary>
    public decimal MaxPaymentAmount { get; set; }

    /// <summary>
    /// Payment thấp nhất
    /// </summary>
    public decimal MinPaymentAmount { get; set; }

    /// <summary>
    /// Số lượng payments mỗi ngày (trung bình)
    /// </summary>
    public decimal AveragePaymentsPerDay { get; set; }

    /// <summary>
    /// Tăng trưởng so với kỳ trước (%)
    /// </summary>
    public decimal GrowthRate { get; set; }
}

/// <summary>
/// Thống kê theo phương thức thanh toán
/// </summary>
public class PaymentMethodStatistics
{
    /// <summary>
    /// ID phương thức thanh toán
    /// </summary>
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Tên phương thức thanh toán
    /// </summary>
    public string PaymentMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng payments
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Tổng số tiền
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Tỉ lệ so với tổng (%)
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Giá trị trung bình
    /// </summary>
    public decimal AverageAmount { get; set; }
}

/// <summary>
/// Thống kê theo trạng thái payment
/// </summary>
public class PaymentStatusStatistics
{
    /// <summary>
    /// Trạng thái
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Tổng số tiền
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Tỉ lệ (%)
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Thống kê theo loại giao dịch
/// </summary>
public class TransactionTypeStatistics
{
    /// <summary>
    /// Loại giao dịch
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Tổng số tiền
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Tỉ lệ (%)
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Giá trị trung bình
    /// </summary>
    public decimal AverageAmount { get; set; }
}