using BookingCare.Shared.Common.Enums;
using System.Globalization;

namespace BookingCare.Shared.Common.Helpers;

/// <summary>
/// Helper class for statistics period calculations
/// </summary>
public static class StatisticsPeriodHelper
{
    /// <summary>
    /// Gets the period bounds (start, end, label) for a given date and period type
    /// </summary>
    /// <param name="date">The date to calculate period bounds for</param>
    /// <param name="period">The statistics period type</param>
    /// <returns>A tuple containing start date, end date, and label</returns>
    public static (DateTime start, DateTime end, string label) GetPeriodBounds(DateTime date, StatisticsPeriod period)
    {
        return period switch
        {
            StatisticsPeriod.Daily => (date, date, date.ToString("yyyy-MM-dd")),
            StatisticsPeriod.Weekly => GetWeeklyPeriod(date),
            StatisticsPeriod.Monthly => GetMonthlyPeriod(date),
            StatisticsPeriod.Quarterly => GetQuarterlyPeriod(date),
            StatisticsPeriod.Yearly => GetYearlyPeriod(date),
            _ => (date, date, date.ToString("yyyy-MM-dd"))
        };
    }

    /// <summary>
    /// Gets the next period date based on the current date and period type
    /// </summary>
    /// <param name="current">The current date</param>
    /// <param name="period">The statistics period type</param>
    /// <returns>The next period date</returns>
    public static DateTime GetNextPeriod(DateTime current, StatisticsPeriod period)
    {
        return period switch
        {
            StatisticsPeriod.Daily => current.AddDays(1),
            StatisticsPeriod.Weekly => current.AddDays(7),
            StatisticsPeriod.Monthly => current.AddMonths(1),
            StatisticsPeriod.Quarterly => current.AddMonths(3),
            StatisticsPeriod.Yearly => current.AddYears(1),
            _ => current.AddDays(1)
        };
    }

    private static (DateTime start, DateTime end, string label) GetWeeklyPeriod(DateTime date)
    {
        var startOfWeek = date.AddDays(-(int)date.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);
        return (startOfWeek, endOfWeek, $"W{GetWeekOfYear(startOfWeek)}-{startOfWeek.Year}");
    }

    private static (DateTime start, DateTime end, string label) GetMonthlyPeriod(DateTime date)
    {
        var startOfMonth = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        return (startOfMonth, endOfMonth, date.ToString("yyyy-MM"));
    }

    private static (DateTime start, DateTime end, string label) GetQuarterlyPeriod(DateTime date)
    {
        var quarter = (date.Month - 1) / 3 + 1;
        var startOfQuarter = new DateTime(date.Year, (quarter - 1) * 3 + 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfQuarter = startOfQuarter.AddMonths(3).AddDays(-1);
        return (startOfQuarter, endOfQuarter, $"{date.Year}-Q{quarter}");
    }

    private static (DateTime start, DateTime end, string label) GetYearlyPeriod(DateTime date)
    {
        var startOfYear = new DateTime(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfYear = new DateTime(date.Year, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        return (startOfYear, endOfYear, date.Year.ToString());
    }

    private static int GetWeekOfYear(DateTime date)
    {
        var culture = CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(
            date,
            culture.DateTimeFormat.CalendarWeekRule,
            culture.DateTimeFormat.FirstDayOfWeek);
    }
}

