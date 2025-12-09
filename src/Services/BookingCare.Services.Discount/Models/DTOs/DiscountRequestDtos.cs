using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Services.Discount.Enums;
using BookingCare.Services.Discount.Infrastructure.JsonConverters;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Discount.Models.DTOs;

// Request DTOs
public class CreateDiscountRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [JsonRequired]
    public Guid HospitalId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    [JsonConverter(typeof(FlexibleEnumConverter<DiscountType>))]
    public DiscountType DiscountType { get; set; } = DiscountType.PERCENTAGE;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "MaxUses must be greater than 0")]
    public int? MaxUses { get; set; }

    public Status Status { get; set; } = Status.ACTIVE;
}

public class UpdateDiscountRequest
{
    [StringLength(50, MinimumLength = 3)]
    public string? Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal? Amount { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "MaxUses must be greater than 0")]
    public int? MaxUses { get; set; }

    public string? Status { get; set; }
}

public class ValidateDiscountRequest
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [JsonRequired]
    public Guid HospitalId { get; set; }

    public Guid? SpecialtyId { get; set; }

    public Guid? DoctorId { get; set; }

    [Required]
    [JsonRequired]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal TotalAmount { get; set; }
}

public class UseDiscountRequest
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [JsonRequired]
    public Guid HospitalId { get; set; }

    [Required]
    [JsonRequired]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal TotalAmount { get; set; }
}

public class RevertDiscountUsageRequest
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [JsonRequired]
    public Guid HospitalId { get; set; }
}

public class CalculateDiscountRequest
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [JsonRequired]
    [Range(0.01, double.MaxValue, ErrorMessage = "Original amount must be greater than 0")]
    public decimal OriginalAmount { get; set; }

    [Required]
    [JsonRequired]
    public Guid HospitalId { get; set; }
}
