using System.ComponentModel.DataAnnotations;

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
    [Range(1, long.MaxValue, ErrorMessage = "ClinicId must be greater than 0")]
    public long ClinicId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "SpecialtyId must be greater than 0")]
    public long? SpecialtyId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "DoctorId must be greater than 0")]
    public long? DoctorId { get; set; }

    [Required]
    public string ApplicableTo { get; set; } = "ALL";

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    public string DiscountType { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "MaxUses must be greater than 0")]
    public int? MaxUses { get; set; }

    public string Status { get; set; } = "ACTIVE";
}

public class UpdateDiscountRequest
{
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Id must be greater than 0")]
    public long Id { get; set; }

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
    [Range(1, long.MaxValue, ErrorMessage = "ClinicId must be greater than 0")]
    public long ClinicId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "SpecialtyId must be greater than 0")]
    public long? SpecialtyId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "DoctorId must be greater than 0")]
    public long? DoctorId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal TotalAmount { get; set; }
}

public class UseDiscountRequest
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "ClinicId must be greater than 0")]
    public long ClinicId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "SpecialtyId must be greater than 0")]
    public long? SpecialtyId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "DoctorId must be greater than 0")]
    public long? DoctorId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal TotalAmount { get; set; }
}
