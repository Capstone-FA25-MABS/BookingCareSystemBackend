namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO cho thống kê payments
/// </summary>
public class GetPaymentStatisticsRequest
{
    /// <summary>
    /// Ngày bắt đầu thống kê (mặc định: 6 tháng trước)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Ngày kết thúc thống kê (mặc định: tháng hiện tại)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Loại thống kê (mặc định: Monthly)
    /// </summary>
    public StatisticsPeriod Period { get; set; } = StatisticsPeriod.Monthly;

    /// <summary>
    /// Clinic ID (tùy chọn - nếu null thì lấy tất cả clinics)
    /// </summary>
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// Patient ID (tùy chọn - nếu null thì lấy tất cả patients)
    /// </summary>
    public Guid? PatientId { get; set; }

    /// <summary>
    /// Loại giao dịch (tùy chọn - nếu null thì lấy tất cả)
    /// </summary>
    public string? TransactionType { get; set; }

    /// <summary>
    /// Trạng thái payment (tùy chọn - nếu null thì lấy tất cả)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Lấy FromDate với default value (6 tháng trước từ đầu tháng)
    /// </summary>
    public DateTime GetFromDate()
    {
        if (FromDate.HasValue) return FromDate.Value.Date;

        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        return new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);
    }

    /// <summary>
    /// Lấy ToDate với default value (cuối tháng hiện tại)
    /// </summary>
    public DateTime GetToDate()
    {
        if (ToDate.HasValue) return ToDate.Value.Date;

        var now = DateTime.Now;
        return new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
    }
}

/// <summary>
/// Enum cho chu kỳ thống kê
/// </summary>
public enum StatisticsPeriod
{
    /// <summary>
    /// Thống kê theo ngày
    /// </summary>
    Daily,

    /// <summary>
    /// Thống kê theo tuần
    /// </summary>
    Weekly,

    /// <summary>
    /// Thống kê theo tháng
    /// </summary>
    Monthly,

    /// <summary>
    /// Thống kê theo quý
    /// </summary>
    Quarterly,

    /// <summary>
    /// Thống kê theo năm
    /// </summary>
    Yearly
}