using BookingCare.Services.Blog.Models.Entities;

namespace BookingCare.Services.Blog.Models.DTOs;

public record BlogSummaryDto
{
    public Guid Id { get; init; }
    public string TitleVi { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public string? Tag { get; init; }
    public string? Source { get; init; }
    public string? CreatedByName { get; init; }
    public BlogStatus Status { get; init; }
    public bool Featured { get; init; }
    public DateTime? PublishedAt { get; init; }
    public BlogCategoryDto? Category { get; init; }
}

public record BlogDetailDto
{
    public Guid Id { get; init; }
    public string TitleVi { get; init; } = string.Empty;
    public string ContentVi { get; init; } = string.Empty;
    public string? TitleEn { get; init; }
    public string? ContentEn { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? HeroImageUrl { get; init; }
    public string? Tag { get; init; }
    public string? Source { get; init; }
    public BlogStatus Status { get; init; }
    public bool Featured { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public BlogCategoryDto? Category { get; init; }
    public string? CreatedByName { get; init; }
    public IReadOnlyList<BlogSummaryDto> RelatedBlogs { get; init; } = Array.Empty<BlogSummaryDto>();
}

public class CreateBlogRequest
{
    public Guid? BlogCategoryId { get; set; }
    public string TitleVi { get; set; } = string.Empty;
    public string ContentVi { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string? ContentEn { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? Tag { get; set; }
    public string? Source { get; set; }
    public BlogStatus Status { get; set; } = BlogStatus.Pending;
    public bool Featured { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class UpdateBlogRequest : CreateBlogRequest
{
}

public class BlogFilterParameters
{
    public Guid? CategoryId { get; set; }
    public string? Tag { get; set; }
    public string? Source { get; set; }
    public BlogStatus? Status { get; set; }
    public bool? Featured { get; set; }
    public string? Keyword { get; set; }
    public Guid? CreatedByAccountId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public record PagedResponse<T>(IReadOnlyList<T> Items, int TotalItems, int Page, int PageSize);

public class ApproveBlogRequest
{
    public bool? Featured { get; set; }
}

