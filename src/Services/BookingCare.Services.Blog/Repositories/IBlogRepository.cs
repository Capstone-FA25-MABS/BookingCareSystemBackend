using BookingCare.Services.Blog.Models.DTOs;
using BookingCare.Services.Blog.Models.Entities;

namespace BookingCare.Services.Blog.Repositories;

public interface IBlogRepository
{
    Task<(IReadOnlyList<BlogEntity> Blogs, int TotalItems)> GetBlogsAsync(BlogFilterParameters filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlogEntity>> GetAllBlogsAsync(CancellationToken cancellationToken = default);
    Task<BlogEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(BlogEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(BlogEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(BlogEntity entity, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlogEntity>> GetRelatedBlogsAsync(Guid blogId, int limit = 10, CancellationToken cancellationToken = default);
}

