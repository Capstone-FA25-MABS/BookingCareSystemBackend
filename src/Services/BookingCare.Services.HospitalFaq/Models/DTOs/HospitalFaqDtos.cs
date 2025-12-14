namespace BookingCare.Services.HospitalFaq.Models.DTOs;

/// <summary>
/// Request DTO for creating a new hospital FAQ
/// </summary>
public class CreateHospitalFaqRequest
{
    public Guid HospitalId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Request DTO for updating an existing hospital FAQ
/// </summary>
public class UpdateHospitalFaqRequest
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Response DTO for hospital FAQ
/// </summary>
public class HospitalFaqResponse
{
    public Guid Id { get; set; }
    public Guid HospitalId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request DTO for filtering/querying hospital FAQs
/// </summary>
public class HospitalFaqFilterRequest
{
    public Guid? HospitalId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Paginated response for hospital FAQs
/// </summary>
public class HospitalFaqListResponse
{
    public List<HospitalFaqResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

