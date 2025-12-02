namespace BookingCare.Services.Blog.Models.Entities;

public class BlogCategoryEntity : ITimestampedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public CategoryStatus Status { get; set; } = CategoryStatus.Active;
    public Guid? ParentId { get; set; }
    public BlogCategoryEntity? Parent { get; set; }
    public ICollection<BlogCategoryEntity> Children { get; set; } = new List<BlogCategoryEntity>();
    public ICollection<BlogEntity> Blogs { get; set; } = new List<BlogEntity>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

