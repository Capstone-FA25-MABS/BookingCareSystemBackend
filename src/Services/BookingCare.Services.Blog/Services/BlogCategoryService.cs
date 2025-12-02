using AutoMapper;
using BookingCare.Services.Blog.Exceptions;
using BookingCare.Services.Blog.Models.DTOs;
using BookingCare.Services.Blog.Models.Entities;
using BookingCare.Services.Blog.Repositories;

namespace BookingCare.Services.Blog.Services;

public class BlogCategoryService : IBlogCategoryService
{
    private readonly IBlogCategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public BlogCategoryService(IBlogCategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<BlogCategoryDto>> GetCategoriesAsync(bool includeChildren, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(includeChildren, cancellationToken);
        return _mapper.Map<IReadOnlyList<BlogCategoryDto>>(categories);
    }

    public async Task<BlogCategoryDto?> GetCategoryByIdAsync(Guid id, bool includeChildren, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, includeChildren, cancellationToken);
        return category is null ? null : _mapper.Map<BlogCategoryDto>(category);
    }

    public async Task<BlogCategoryDto> CreateCategoryAsync(CreateBlogCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureParentExistsIfNeeded(request.ParentId, cancellationToken);

        var entity = _mapper.Map<BlogCategoryEntity>(request);
        await _categoryRepository.AddAsync(entity, cancellationToken);
        return _mapper.Map<BlogCategoryDto>(entity);
    }

    public async Task<BlogCategoryDto> UpdateCategoryAsync(Guid id, UpdateBlogCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ParentId.HasValue && request.ParentId.Value == id)
        {
            throw new BlogServiceException("Danh mục không thể là cha của chính nó.");
        }

        await EnsureParentExistsIfNeeded(request.ParentId, cancellationToken);

        var existingCategory = await _categoryRepository.GetByIdAsync(id, false, cancellationToken);
        if (existingCategory is null)
        {
            throw new BlogServiceException($"Không tìm thấy danh mục với id {id}");
        }

        existingCategory.CategoryName = request.CategoryName;
        existingCategory.Description = request.Description;
        existingCategory.ImageUrl = request.ImageUrl;
        existingCategory.Status = request.Status;
        existingCategory.ParentId = request.ParentId;

        await _categoryRepository.UpdateAsync(existingCategory, cancellationToken);
        return _mapper.Map<BlogCategoryDto>(existingCategory);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existingCategory = await _categoryRepository.GetByIdAsync(id, true, cancellationToken);
        if (existingCategory is null)
        {
            throw new BlogServiceException($"Không tìm thấy danh mục với id {id}");
        }

        if (existingCategory.Children.Any())
        {
            throw new BlogServiceException("Không thể xóa danh mục vẫn còn danh mục con.");
        }

        if (existingCategory.Blogs.Any())
        {
            throw new BlogServiceException("Không thể xóa danh mục đang chứa bài viết.");
        }

        await _categoryRepository.DeleteAsync(existingCategory, cancellationToken);
    }

    private async Task EnsureParentExistsIfNeeded(Guid? parentId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return;
        }

        var exists = await _categoryRepository.ExistsAsync(parentId.Value, cancellationToken);
        if (!exists)
        {
            throw new BlogServiceException($"Danh mục cha với id {parentId.Value} không tồn tại.");
        }
    }
}

