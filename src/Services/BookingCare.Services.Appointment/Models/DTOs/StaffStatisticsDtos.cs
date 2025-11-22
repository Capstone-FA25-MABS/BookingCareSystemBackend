using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Appointment.Models.DTOs;

public class StaffHospitalStatisticsRequest
{
    [Required]
    public Guid HospitalId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public StatisticsPeriod Period { get; set; } = StatisticsPeriod.Weekly;

    public DateTime GetFromDate()
    {
        if (FromDate.HasValue)
        {
            return DateTime.SpecifyKind(FromDate.Value.Date, DateTimeKind.Utc);
        }

        var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-30);
        return DateTime.SpecifyKind(thirtyDaysAgo, DateTimeKind.Utc);
    }

    public DateTime GetToDate()
    {
        if (ToDate.HasValue)
        {
            return DateTime.SpecifyKind(ToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        }

        var today = DateTime.UtcNow.Date;
        return DateTime.SpecifyKind(today.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
    }
}

public class StaffHospitalStatisticsResponse
{
    public Guid HospitalId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public StatisticsPeriod Period { get; set; }
    public HospitalAppointmentOverview Overview { get; set; } = new();
    public List<AppointmentTrendPoint> AppointmentTrend { get; set; } = new();
    public List<NewPatientTrendPoint> NewPatientTrend { get; set; } = new();

    public static StaffHospitalStatisticsResponse CreateEmpty(Guid hospitalId, DateTime fromDate, DateTime toDate, StatisticsPeriod period)
    {
        return new StaffHospitalStatisticsResponse
        {
            HospitalId = hospitalId,
            FromDate = fromDate,
            ToDate = toDate,
            Period = period
        };
    }
}

public class HospitalAppointmentOverview
{
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int ConfirmedAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int RescheduledAppointments { get; set; }
    public int NewPatients { get; set; }
    public decimal NoShowRate { get; set; }
    public decimal RescheduleRate { get; set; }
}

public class AppointmentTrendPoint
{
    public string Label { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int RescheduledAppointments { get; set; }
}

public class NewPatientTrendPoint
{
    public string Label { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int NewPatients { get; set; }
}

public enum StatisticsPeriod
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Yearly
}


