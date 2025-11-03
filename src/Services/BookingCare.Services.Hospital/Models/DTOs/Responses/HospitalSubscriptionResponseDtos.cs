using BookingCare.Services.Hospital.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Responses;

public class HospitalSubscriptionResponse
{
    public Guid HospitalSubscriptionId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid SubscriptionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Use HospitalSimpleResponse instead of HospitalResponse to avoid circular reference
    public HospitalSimpleResponse? Hospital { get; set; }
    public SubscriptionPlanResponse? SubscriptionPlan { get; set; }
    public bool IsActive => Status == SubscriptionStatus.ACTIVE && DateTime.Now >= StartDate && DateTime.Now <= EndDate;
    public int DaysRemaining => (EndDate - DateTime.Now).Days;
}

public class HospitalSubscriptionListResponse
{
    public List<HospitalSubscriptionResponse> HospitalSubscriptions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
