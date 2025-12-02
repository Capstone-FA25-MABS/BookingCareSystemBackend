using BookingCare.Services.Blog.Models.DTOs;

namespace BookingCare.Services.Blog.Services;

public interface IBlogCategoryService
{
    Task<IReadOnlyList<BlogCategoryDto>> GetCategoriesAsync(bool includeChildren, CancellationToken cancellationToken = default);
    Task<BlogCategoryDto?> GetCategoryByIdAsync(Guid id, bool includeChildren, CancellationToken cancellationToken = default);
    Task<BlogCategoryDto> CreateCategoryAsync(CreateBlogCategoryRequest request, CancellationToken cancellationToken = default);
    Task<BlogCategoryDto> UpdateCategoryAsync(Guid id, UpdateBlogCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}

