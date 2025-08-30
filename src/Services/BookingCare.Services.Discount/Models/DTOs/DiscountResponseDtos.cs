using BookingCare.Services.Discount.Enums;

namespace BookingCare.Services.Discount.Models.DTOs;

// Response DTOs
public class DiscountResponse
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long ClinicId { get; set; }
    public long? SpecialtyId { get; set; }
    public long? DoctorId { get; set; }
    public DiscountApplicableTo ApplicableTo { get; set; }
    public decimal Amount { get; set; }
    public DiscountType DiscountType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? MaxUses { get; set; }
    public int UsesCount { get; set; }
    public DiscountStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DiscountValidationResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public DiscountResponse? Discount { get; set; }
}

public class DiscountUsageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public long DiscountId { get; set; }
    public int RemainingUses { get; set; }
}

public class DiscountListResponse
{
    public List<DiscountResponse> Discounts { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

// Query DTOs
public class DiscountQueryRequest
{
    public long? ClinicId { get; set; }
    public long? SpecialtyId { get; set; }
    public long? DoctorId { get; set; }
    public DiscountStatus? Status { get; set; }
    public DiscountApplicableTo? ApplicableTo { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
