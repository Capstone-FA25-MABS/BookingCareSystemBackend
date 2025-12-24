using BookingCare.Services.Hospital.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

/// <summary>
/// Query parameters for filtering and paginating hospital registrations
/// </summary>
public class HospitalRegistrationQueryParameters
{
    public string? SearchTerm { get; set; }
    public RegistrationStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "createdAt";
    public string SortOrder { get; set; } = "DESC";
}

