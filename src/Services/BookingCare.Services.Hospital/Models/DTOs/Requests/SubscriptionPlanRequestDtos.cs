using System.ComponentModel.DataAnnotations;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

public class CreateSubscriptionPlanRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [MaxLength(20)]
    public string BillingCycle { get; set; } = "MONTHLY";

    [Range(0, int.MaxValue)]
    public int MaxDoctors { get; set; } = 0;

    [Range(0, int.MaxValue)]
    public int MaxSpecialties { get; set; } = 0;

    public string? Features { get; set; }
}

public class UpdateSubscriptionPlanRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Price { get; set; }

    [MaxLength(20)]
    public string? BillingCycle { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxDoctors { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxSpecialties { get; set; }

    public string? Features { get; set; }

    public Status? Status { get; set; }
}

public class SubscriptionPlanFilterRequest
{
    public string? Name { get; set; }
    public Status? Status { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? BillingCycle { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
}
