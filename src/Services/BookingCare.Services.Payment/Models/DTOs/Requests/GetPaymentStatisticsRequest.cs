namespace BookingCare.Services.Payment.Models.DTOs.Requests;

// <summary>
// Request DTO for payment statistics
// </summary>
public class GetPaymentStatisticsRequest
{
    /// <summary>
    /// Start date for statistics (default: 6 months ago)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date for statistics (default: current month)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Statistics period (default: Monthly)
    /// </summary>
    public StatisticsPeriod Period { get; set; } = StatisticsPeriod.Monthly;

    /// <summary>
    /// Hospital ID (optional - if null get all hospitals)
    /// </summary>
    public Guid? HospitalId { get; set; }

    /// <summary>
    /// Patient ID (optional - if null get all patients)
    /// </summary>
    public Guid? PatientId { get; set; }

    /// <summary>
    /// Transaction type (optional - if null get all)
    /// </summary>
    public string? TransactionType { get; set; }

    /// <summary>
    /// Payment status (optional - if null get all)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Get FromDate with default value (6 months ago from start of month)
    /// </summary>
    public DateTime GetFromDate()
    {
        if (FromDate.HasValue) return FromDate.Value.Date;

        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        return new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Local);
    }

    /// <summary>
    /// Get ToDate with default value (end of current month)
    /// </summary>
    public DateTime GetToDate()
    {
        if (ToDate.HasValue) return ToDate.Value.Date;

        var now = DateTime.Now;
        return new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 0, 0, 0, DateTimeKind.Local);
    }
}

/// <summary>
/// Enum for statistics period
/// </summary>
public enum StatisticsPeriod
{
    /// <summary>
    /// Daily statistics
    /// </summary>
    Daily,

    /// <summary>
    /// Weekly statistics
    /// </summary>
    Weekly,

    /// <summary>
    /// Monthly statistics
    /// </summary>
    Monthly,

    /// <summary>
    /// Quarterly statistics
    /// </summary>
    Quarterly,

    /// <summary>
    /// Yearly statistics
    /// </summary>
    Yearly
}