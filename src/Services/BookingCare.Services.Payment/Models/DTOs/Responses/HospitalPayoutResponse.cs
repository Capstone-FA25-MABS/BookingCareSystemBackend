using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO for hospital payout summary
/// </summary>
public class HospitalPayoutResponse
{
    public Guid Id { get; set; }
    public Guid HospitalId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public Guid BankAccountId { get; set; }
    public BankAccountInfo BankAccount { get; set; } = null!;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public int AppointmentCount { get; set; }
    public PayoutStatus Status { get; set; }
    public Guid? ProcessedByAdminId { get; set; }
    public string? ProcessedByAdminName { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Bank account information for payout
/// </summary>
public class BankAccountInfo
{
    public Guid Id { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}

/// <summary>
/// Response for payout statistics
/// </summary>
public class PayoutStatisticsResponse
{
    public int TotalPendingPayouts { get; set; }
    public decimal TotalPendingAmount { get; set; }
    public int TotalCompletedPayouts { get; set; }
    public decimal TotalCompletedAmount { get; set; }
    public int TotalHospitals { get; set; }
}

/// <summary>
/// Response for payout details with appointment breakdown
/// </summary>
public class PayoutDetailsResponse
{
    public HospitalPayoutResponse Payout { get; set; } = null!;
    public List<PayoutAppointmentDetail> Appointments { get; set; } = new();
}

/// <summary>
/// Individual appointment detail in payout
/// </summary>
public class PayoutAppointmentDetail
{
    public Guid AppointmentId { get; set; }
    public Guid PaymentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentCompletedAt { get; set; }
}
