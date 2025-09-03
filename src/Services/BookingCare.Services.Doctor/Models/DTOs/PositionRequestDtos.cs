using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Doctor.Models.DTOs;

// Position Request DTOs
public class CreatePositionRequest
{
    [Required(ErrorMessage = "Position name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Position name must be between 2 and 255 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_()]+$", ErrorMessage = "Position name can only contain letters, numbers, spaces, hyphens, underscores, and parentheses")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
}

public class UpdatePositionRequest
{
    [Required(ErrorMessage = "Position ID is required")]
    public Guid Id { get; set; }

    [StringLength(255, MinimumLength = 2, ErrorMessage = "Position name must be between 2 and 255 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_()]+$", ErrorMessage = "Position name can only contain letters, numbers, spaces, hyphens, underscores, and parentheses")]
    public string? Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
}

public class PositionQueryRequest
{
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
