namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO for payment statistics
/// </summary>
public class PaymentStatisticsResponse
{
    /// <summary>
    /// Time series statistics data
    /// </summary>
    public List<PaymentTimeSeriesData> TimeSeries { get; set; } = new();

    /// <summary>
    /// Summary statistics
    /// </summary>
    public PaymentSummaryStatistics Summary { get; set; } = new();

    /// <summary>
    /// Breakdown by payment method
    /// </summary>
    public List<PaymentMethodStatistics> PaymentMethodBreakdown { get; set; } = new();

    /// <summary>
    /// Breakdown by status
    /// </summary>
    public List<PaymentStatusStatistics> StatusBreakdown { get; set; } = new();

    /// <summary>
    /// Breakdown by transaction type
    /// </summary>
    public List<TransactionTypeStatistics> TransactionTypeBreakdown { get; set; } = new();

    /// <summary>
    /// Period used for statistics
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Date range for the statistics
    /// </summary>
    public string DateRange { get; set; } = string.Empty;
}

/// <summary>
/// Time series statistics data
/// </summary>
public class PaymentTimeSeriesData
{
    /// <summary>
    /// Time label (e.g. "2024-01", "2024-Q1", "2024-01-15")
    /// </summary>
    public string TimeLabel { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the period
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End date of the period
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Total number of payments
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of completed payments
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// Amount from completed payments
    /// </summary>
    public decimal CompletedAmount { get; set; }

    /// <summary>
    /// Number of pending payments
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// Number of failed payments
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Number of refunded payments
    /// </summary>
    public int RefundedCount { get; set; }

    /// <summary>
    /// Average amount per payment
    /// </summary>
    public decimal AverageAmount { get; set; }
}

/// <summary>
/// Summary statistics
/// </summary>
public class PaymentSummaryStatistics
{
    /// <summary>
    /// Total number of payments
    /// </summary>
    public int TotalPayments { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Total amount from completed payments
    /// </summary>
    public decimal TotalCompletedAmount { get; set; }

    /// <summary>
    /// Success rate (%)
    /// </summary>
    public decimal SuccessRate { get; set; }

    /// <summary>
    /// Average payment amount
    /// </summary>
    public decimal AveragePaymentAmount { get; set; }

    /// <summary>
    /// Maximum payment amount
    /// </summary>
    public decimal MaxPaymentAmount { get; set; }

    /// <summary>
    /// Minimum payment amount
    /// </summary>
    public decimal MinPaymentAmount { get; set; }

    /// <summary>
    /// Average payments per day
    /// </summary>
    public decimal AveragePaymentsPerDay { get; set; }

    /// <summary>
    /// Growth rate compared to previous period (%)
    /// </summary>
    public decimal GrowthRate { get; set; }
}

/// <summary>
/// Statistics by payment method
/// </summary>
public class PaymentMethodStatistics
{
    /// <summary>
    /// Payment method ID
    /// </summary>
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Payment method name
    /// </summary>
    public string PaymentMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Count of payments
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Percentage of total (%)
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Average amount
    /// </summary>
    public decimal AverageAmount { get; set; }
}

/// <summary>
/// Statistics by payment status
/// </summary>
public class PaymentStatusStatistics
{
    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Count
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Percentage (%)
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Statistics by transaction type
/// </summary>
public class TransactionTypeStatistics
{
    /// <summary>
    /// Transaction type
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    /// Count
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Percentage (%)
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Average amount
    /// </summary>
    public decimal AverageAmount { get; set; }
}