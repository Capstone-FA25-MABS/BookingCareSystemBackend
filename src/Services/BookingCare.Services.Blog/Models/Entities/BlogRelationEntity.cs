namespace BookingCare.Services.Blog.Models.Entities;

public class BlogRelationEntity
{
    public long BlogId { get; set; }
    public BlogEntity Blog { get; set; } = null!;

    public long RelatedBlogId { get; set; }
    public BlogEntity RelatedBlog { get; set; } = null!;

    public BlogRelationType RelationType { get; set; } = BlogRelationType.Related;
}

