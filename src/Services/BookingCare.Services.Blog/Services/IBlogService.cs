using BookingCare.Services.Blog.Models.DTOs;
using BookingCare.Services.Blog.Models.Entities;

namespace BookingCare.Services.Blog.Services;

public interface IBlogService
{
    Task<PagedResponse<BlogSummaryDto>> GetBlogsAsync(BlogFilterParameters filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlogDetailDto>> GetAllBlogsAsync(CancellationToken cancellationToken = default);
    Task<BlogDetailDto?> GetBlogByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BlogDetailDto> CreateBlogAsync(CreateBlogRequest request, Guid? createdBy = null, CancellationToken cancellationToken = default);
    Task<BlogDetailDto> UpdateBlogAsync(Guid id, UpdateBlogRequest request, CancellationToken cancellationToken = default);
    Task DeleteBlogAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BlogDetailDto> ApproveBlogAsync(Guid id, bool? featured = null, CancellationToken cancellationToken = default);
    Task<BlogDetailDto> RejectBlogAsync(Guid id, CancellationToken cancellationToken = default);
}

