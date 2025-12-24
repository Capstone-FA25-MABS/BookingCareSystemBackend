namespace BookingCare.Services.Content.Models.Entities;

/// <summary>
/// Blog post entity migrated from BookingCare.Services.Blog.
/// </summary>
public class BlogEntity : ITimestampedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? BlogCategoryId { get; set; }
    public BlogCategoryEntity? Category { get; set; }

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

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}


