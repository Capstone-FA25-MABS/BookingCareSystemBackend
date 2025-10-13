namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO for paged list of payments
/// </summary>
public class GetPaymentsPagedRequest
{
    /// <summary>
    /// Page number (starts from 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size (max 100)
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Search keyword (optional)
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Sort by field (CreatedAt, Amount, Status)
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Sort order (asc or desc)
    /// </summary>
    public string SortOrder { get; set; } = "desc";
}