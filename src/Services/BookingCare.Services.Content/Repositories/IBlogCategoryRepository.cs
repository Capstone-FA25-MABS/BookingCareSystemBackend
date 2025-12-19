using BookingCare.Services.Content.Models.Entities;

namespace BookingCare.Services.Content.Repositories;

public interface IBlogCategoryRepository
{
    Task<IReadOnlyList<BlogCategoryEntity>> GetAllAsync(
        bool includeChildren,
        CancellationToken cancellationToken = default);

    Task<BlogCategoryEntity?> GetByIdAsync(
        Guid id,
        bool includeChildren = false,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(BlogCategoryEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(BlogCategoryEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(BlogCategoryEntity entity, CancellationToken cancellationToken = default);
}


