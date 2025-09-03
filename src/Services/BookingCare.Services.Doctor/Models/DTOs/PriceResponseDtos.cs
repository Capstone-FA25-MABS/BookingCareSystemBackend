namespace BookingCare.Services.Doctor.Models.DTOs;

// Price Response DTOs
public class PriceResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class PriceListResponse
{
    public List<PriceResponse> Prices { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
