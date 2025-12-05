using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Appointment.Models.DTOs;

/// <summary>
/// Request DTO for appointment revenue statistics
/// Staff dashboard shows revenue from COMPLETED appointments only
/// </summary>
public class GetAppointmentStatisticsRequest
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
    /// Hospital ID (required for Staff role - staff can only see their hospital's statistics)
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Doctor ID (optional - if provided, show statistics for specific doctor)
    /// </summary>
    public Guid? DoctorId { get; set; }

    /// <summary>
    /// Specialty ID (optional - if provided, show statistics for specific specialty)
    /// </summary>
    public Guid? SpecialtyId { get; set; }

    /// <summary>
    /// Get FromDate with default value (6 months ago from start of month)
    /// </summary>
    public DateTime GetFromDate()
    {
        if (FromDate.HasValue)
            return FromDate.Value.Date;

        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        return new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Local);
    }

    /// <summary>
    /// Get ToDate with default value (end of current month)
    /// </summary>
    public DateTime GetToDate()
    {
        if (ToDate.HasValue)
            return ToDate.Value.Date;

        var now = DateTime.Now;
        return new DateTime(
            now.Year,
            now.Month,
            DateTime.DaysInMonth(now.Year, now.Month),
            23,
            59,
            59,
            DateTimeKind.Local
        );
    }
}

/// <summary>
/// Response DTO for appointment revenue statistics
/// </summary>
public class AppointmentStatisticsResponse
{
    /// <summary>
    /// Time series statistics data
    /// </summary>
    public List<AppointmentTimeSeriesData> TimeSeries { get; set; } = new();

    /// <summary>
    /// Summary statistics
    /// </summary>
    public AppointmentSummaryStatistics Summary { get; set; } = new();

    /// <summary>
    /// Breakdown by doctor (top 10)
    /// </summary>
    public List<DoctorStatistics> TopDoctors { get; set; } = new();

    /// <summary>
    /// Breakdown by specialty
    /// </summary>
    public List<SpecialtyStatistics> SpecialtyBreakdown { get; set; } = new();

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
/// Time series data for appointments
/// </summary>
public class AppointmentTimeSeriesData
{
    /// <summary>
    /// Time label (e.g. "2024-01", "2024-Q1", "Week 15/2024")
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
    /// Total number of completed appointments
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total revenue from completed appointments (sum of Amount field)
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Average revenue per appointment
    /// </summary>
    public decimal AverageRevenue { get; set; }
}

/// <summary>
/// Summary statistics for appointments
/// </summary>
public class AppointmentSummaryStatistics
{
    /// <summary>
    /// Total number of completed appointments
    /// </summary>
    public int TotalCompletedAppointments { get; set; }

    /// <summary>
    /// Total revenue from completed appointments
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Average revenue per appointment
    /// </summary>
    public decimal AverageRevenuePerAppointment { get; set; }

    /// <summary>
    /// Maximum appointment amount
    /// </summary>
    public decimal MaxAppointmentAmount { get; set; }

    /// <summary>
    /// Minimum appointment amount
    /// </summary>
    public decimal MinAppointmentAmount { get; set; }

    /// <summary>
    /// Average appointments per day
    /// </summary>
    public decimal AverageAppointmentsPerDay { get; set; }

    /// <summary>
    /// Growth rate compared to previous period (%)
    /// </summary>
    public decimal GrowthRate { get; set; }
}

/// <summary>
/// Statistics by doctor
/// </summary>
public class DoctorStatistics
{
    /// <summary>
    /// Doctor ID
    /// </summary>
    public Guid DoctorId { get; set; }

    /// <summary>
    /// Doctor name
    /// </summary>
    public string DoctorName { get; set; } = string.Empty;

    /// <summary>
    /// Specialty name
    /// </summary>
    public string SpecialtyName { get; set; } = string.Empty;

    /// <summary>
    /// Number of completed appointments
    /// </summary>
    public int CompletedAppointments { get; set; }

    /// <summary>
    /// Total revenue generated
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Average revenue per appointment
    /// </summary>
    public decimal AverageRevenue { get; set; }

    /// <summary>
    /// Percentage of total hospital revenue (%)
    /// </summary>
    public decimal PercentageOfTotal { get; set; }
}

/// <summary>
/// Statistics by specialty
/// </summary>
public class SpecialtyStatistics
{
    /// <summary>
    /// Specialty ID
    /// </summary>
    public Guid SpecialtyId { get; set; }

    /// <summary>
    /// Specialty name
    /// </summary>
    public string SpecialtyName { get; set; } = string.Empty;

    /// <summary>
    /// Number of completed appointments
    /// </summary>
    public int CompletedAppointments { get; set; }

    /// <summary>
    /// Total revenue
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Average revenue per appointment
    /// </summary>
    public decimal AverageRevenue { get; set; }

    /// <summary>
    /// Percentage of total hospital revenue (%)
    /// </summary>
    public decimal PercentageOfTotal { get; set; }
}
