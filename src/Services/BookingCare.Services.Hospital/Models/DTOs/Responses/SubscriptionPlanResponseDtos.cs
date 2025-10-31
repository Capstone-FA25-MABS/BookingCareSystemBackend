using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Responses;

public class SubscriptionPlanResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string BillingCycle { get; set; } = string.Empty;
    public int? MaxDoctors { get; set; } // null = unlimited
    public int? MaxSpecialties { get; set; } // null = unlimited
    public int? MaxAppointments { get; set; } // null = unlimited
    public string? Features { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SubscriptionPlanListResponse
{
    public List<SubscriptionPlanResponse> SubscriptionPlans { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class SubscriptionPlanDetailResponse : SubscriptionPlanResponse
{
    public List<HospitalSubscriptionResponse>? HospitalSubscriptions { get; set; }
    public int ActiveSubscriptionsCount { get; set; }
}
