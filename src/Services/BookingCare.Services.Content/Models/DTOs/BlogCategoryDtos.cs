using BookingCare.Services.Content.Models.Entities;

namespace BookingCare.Services.Content.Models.DTOs;

public record BlogCategoryDto(
    Guid Id,
    string CategoryName,
    string? Description,
    string? ImageUrl,
    CategoryStatus Status,
    Guid? ParentId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<BlogCategoryDto>? Children = null);

public class CreateBlogCategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public CategoryStatus Status { get; set; } = CategoryStatus.Active;
    public Guid? ParentId { get; set; }
}

public class UpdateBlogCategoryRequest : CreateBlogCategoryRequest
{
}


