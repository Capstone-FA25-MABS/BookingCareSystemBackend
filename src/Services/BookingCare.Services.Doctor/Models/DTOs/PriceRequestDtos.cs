using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Doctor.Models.DTOs;

// Price Request DTOs
public class CreatePriceRequest
{
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Amount must be a valid decimal number with up to 2 decimal places")]
    public decimal Amount { get; set; }
}

public class UpdatePriceRequest
{
    [Required(ErrorMessage = "Price ID is required")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Amount must be a valid decimal number with up to 2 decimal places")]
    public decimal Amount { get; set; }
}

public class PriceQueryRequest
{
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
