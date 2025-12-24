namespace BookingCare.Services.Hospital.Models.DTOs.Responses;

public class SubscriptionUsageResponse
{
    public Guid HospitalId { get; set; }
    public bool HasActiveSubscription { get; set; }
    public string? Message { get; set; }

    // Subscription Plan Info
    public Guid? SubscriptionPlanId { get; set; }
    public string? SubscriptionPlanName { get; set; }

    // Limits (null = unlimited)
    public int? MaxDoctors { get; set; }
    public int? MaxSpecialties { get; set; }
    public int? MaxAppointments { get; set; }
    public int? MaxServices { get; set; }

    // Current Usage
    public int CurrentDoctorCount { get; set; }
    public int CurrentSpecialtyCount { get; set; }
    public int CurrentAppointmentCount { get; set; }
    public int CurrentServiceCount { get; set; }

    // Usage Percentages
    public decimal DoctorUsagePercentage { get; set; }
    public decimal SpecialtyUsagePercentage { get; set; }
    public decimal AppointmentUsagePercentage { get; set; }
    public decimal ServiceUsagePercentage { get; set; }

    // Limit Status
    public bool IsDoctorLimitExceeded { get; set; }
    public bool IsSpecialtyLimitExceeded { get; set; }
    public bool IsAppointmentLimitExceeded { get; set; }
    public bool IsServiceLimitExceeded { get; set; }

    // Subscription Info
    public DateTime? SubscriptionEndDate { get; set; }
    public int DaysUntilExpiry { get; set; }

    // Additional Info
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

public class SubscriptionUsageAlertResponse
{
    public Guid HospitalId { get; set; }
    public bool HasAlerts { get; set; }
    public List<string> Alerts { get; set; } = new();
    public SubscriptionUsageResponse? Usage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.Now;
}

public class SubscriptionUsageReportResponse
{
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<SubscriptionUsageResponse> HospitalUsages { get; set; } = new();
    public int TotalHospitals { get; set; }
    public int HospitalsWithActiveSubscriptions { get; set; }
    public int HospitalsWithAlerts { get; set; }
    public decimal AverageDoctorUsagePercentage { get; set; }
    public decimal AverageSpecialtyUsagePercentage { get; set; }
}
