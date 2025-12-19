using BookingCare.Services.Content.Models.DTOs;
using BookingCare.Services.Content.Models.Entities;

namespace BookingCare.Services.Content.Repositories;

public interface IBlogRepository
{
    Task<(IReadOnlyList<BlogEntity> Blogs, int TotalItems)> GetBlogsAsync(
        BlogFilterParameters filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlogEntity>> GetAllBlogsAsync(
        CancellationToken cancellationToken = default);

    Task<BlogEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(BlogEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(BlogEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(BlogEntity entity, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlogEntity>> GetRelatedBlogsAsync(
        Guid blogId,
        int limit = 10,
        CancellationToken cancellationToken = default);
}


