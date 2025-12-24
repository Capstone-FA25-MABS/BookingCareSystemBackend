using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Services.Hospital.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

public class CreateHospitalSubscriptionRequest
{
    [Required]
    [JsonRequired]
    public Guid HospitalId { get; set; }

    [Required]
    [JsonRequired]
    public Guid SubscriptionId { get; set; }

    [Required]
    [JsonRequired]
    public DateTime StartDate { get; set; }

    [Required]
    [JsonRequired]
    public DateTime EndDate { get; set; }
}

public class UpdateHospitalSubscriptionRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public SubscriptionStatus? Status { get; set; }
}

public class HospitalSubscriptionFilterRequest
{
    public Guid? HospitalId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public SubscriptionStatus? Status { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public DateTime? EndDateFrom { get; set; }
    public DateTime? EndDateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
}

public class UpgradeHospitalSubscriptionRequest
{
    [Required]
    [JsonRequired]
    public Guid NewSubscriptionPlanId { get; set; }
}