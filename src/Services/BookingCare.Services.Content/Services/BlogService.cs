using AutoMapper;
using BookingCare.Services.Content.Exceptions;
using BookingCare.Services.Content.Models.DTOs;
using BookingCare.Services.Content.Models.Entities;
using BookingCare.Services.Content.Repositories;
using BookingCare.Services.User.Protos;
using BookingCare.Shared.Common.Exceptions;
using Grpc.Core;

namespace BookingCare.Services.Content.Services;

public class BlogService : IBlogService
{
    private readonly IBlogRepository _blogRepository;
    private readonly IBlogCategoryRepository _categoryRepository;
    private readonly IMapper _mapper;
    private readonly UserService.UserServiceClient _userClient;

    public BlogService(
        IBlogRepository blogRepository,
        IBlogCategoryRepository categoryRepository,
        IMapper mapper,
        UserService.UserServiceClient userClient)
    {
        _blogRepository = blogRepository;
        _categoryRepository = categoryRepository;
        _mapper = mapper;
        _userClient = userClient;
    }

    public async Task<PagedResponse<BlogSummaryDto>> GetBlogsAsync(
        BlogFilterParameters filter,
        CancellationToken cancellationToken = default)
    {
        var sanitizedFilter = SanitizeFilter(filter);
        var (blogs, totalItems) = await _blogRepository.GetBlogsAsync(sanitizedFilter, cancellationToken);
        var summaries = _mapper.Map<IReadOnlyList<BlogSummaryDto>>(blogs);

        // Batch load creator names
        var creatorNames = await GetCreatorNamesBatchAsync(
            blogs.Where(b => b.CreatedBy.HasValue).Select(b => b.CreatedBy!.Value).Distinct().ToList(),
            cancellationToken);

        // Set CreatedByName for all summaries
        var summariesWithCreatorNames = summaries.Select(dto =>
        {
            var blog = blogs.First(b => b.Id == dto.Id);
            var creatorName = blog.CreatedBy.HasValue &&
                              creatorNames.TryGetValue(blog.CreatedBy.Value, out var name)
                ? name
                : null;
            return dto with { CreatedByName = creatorName };
        }).ToList();

        return new PagedResponse<BlogSummaryDto>(summariesWithCreatorNames, totalItems, sanitizedFilter.Page, sanitizedFilter.PageSize);
    }

    public async Task<IReadOnlyList<BlogDetailDto>> GetAllBlogsAsync(
        CancellationToken cancellationToken = default)
    {
        var blogs = await _blogRepository.GetAllBlogsAsync(cancellationToken);
        var blogDtos = _mapper.Map<IReadOnlyList<BlogDetailDto>>(blogs);

        // Batch load creator names
        var creatorNames = await GetCreatorNamesBatchAsync(
            blogs.Where(b => b.CreatedBy.HasValue).Select(b => b.CreatedBy!.Value).Distinct().ToList(),
            cancellationToken);

        // Set RelatedBlogs to empty list and CreatedByName for all blogs
        return blogDtos.Select(dto =>
        {
            var blog = blogs.First(b => b.Id == dto.Id);
            var creatorName = blog.CreatedBy.HasValue &&
                              creatorNames.TryGetValue(blog.CreatedBy.Value, out var name)
                ? name
                : null;
            return dto with
            {
                RelatedBlogs = Array.Empty<BlogSummaryDto>(),
                CreatedByName = creatorName
            };
        }).ToList();
    }

    public async Task<BlogDetailDto?> GetBlogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var blog = await _blogRepository.GetByIdAsync(id, cancellationToken);
        if (blog is null)
        {
            return null;
        }

        var relatedBlogs = await _blogRepository.GetRelatedBlogsAsync(id, limit: 10, cancellationToken);
        return await BuildBlogDetailWithRelationsAsync(blog, relatedBlogs, cancellationToken);
    }

    public async Task<BlogDetailDto> CreateBlogAsync(
        CreateBlogRequest request,
        Guid? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureCategoryExists(request.BlogCategoryId, cancellationToken);

        var entity = _mapper.Map<BlogEntity>(request);
        entity.CreatedBy = createdBy;
        // Auto-approve newly created blogs (temporary): set status active and publish immediately if not specified
        entity.Status = BlogStatus.Active;
        if (!entity.PublishedAt.HasValue)
        {
            entity.PublishedAt = DateTime.UtcNow;
        }
        await _blogRepository.AddAsync(entity, cancellationToken);

        var created = await _blogRepository.GetByIdAsync(entity.Id, cancellationToken)
            ?? throw new BlogServiceException("Không thể tải lại bài viết sau khi tạo.");
        var relatedBlogs = await _blogRepository.GetRelatedBlogsAsync(entity.Id, limit: 10, cancellationToken);

        return await BuildBlogDetailWithRelationsAsync(created, relatedBlogs, cancellationToken);
    }

    public async Task<BlogDetailDto> UpdateBlogAsync(
        Guid id,
        UpdateBlogRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCategoryExists(request.BlogCategoryId, cancellationToken);

        var existing = await _blogRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException("Blog", id);
        }

        existing.BlogCategoryId = request.BlogCategoryId;
        existing.TitleVi = request.TitleVi;
        existing.ContentVi = request.ContentVi;
        existing.TitleEn = request.TitleEn;
        existing.ContentEn = request.ContentEn;
        existing.ThumbnailUrl = request.ThumbnailUrl;
        existing.HeroImageUrl = request.HeroImageUrl;
        existing.Tag = request.Tag;
        existing.Source = request.Source;
        // Auto-approve updates as well (temporary): force status to Active and set PublishedAt if missing
        existing.Status = BlogStatus.Active;
        existing.Featured = request.Featured;
        existing.PublishedAt = request.PublishedAt ?? existing.PublishedAt ?? DateTime.UtcNow;

        await _blogRepository.UpdateAsync(existing, cancellationToken);

        var updated = await _blogRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BlogServiceException("Không thể tải lại bài viết sau khi cập nhật.");
        var relatedBlogs = await _blogRepository.GetRelatedBlogsAsync(id, limit: 10, cancellationToken);

        return await BuildBlogDetailWithRelationsAsync(updated, relatedBlogs, cancellationToken);
    }

    public async Task DeleteBlogAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var existing = await _blogRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException("Blog", id);
        }

        await _blogRepository.DeleteAsync(existing, cancellationToken);
    }

    public async Task<BlogDetailDto> ApproveBlogAsync(
        Guid id,
        bool? featured = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _blogRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException("Blog", id);
        }

        existing.Status = BlogStatus.Active;
        if (!existing.PublishedAt.HasValue)
        {
            existing.PublishedAt = DateTime.UtcNow;
        }

        // Update featured status if provided
        if (featured.HasValue)
        {
            existing.Featured = featured.Value;
        }

        await _blogRepository.UpdateAsync(existing, cancellationToken);

        var updated = await _blogRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BlogServiceException("Không thể tải lại bài viết sau khi duyệt.");
        var relatedBlogs = await _blogRepository.GetRelatedBlogsAsync(id, limit: 10, cancellationToken);

        return await BuildBlogDetailWithRelationsAsync(updated, relatedBlogs, cancellationToken);
    }

    public async Task<BlogDetailDto> RejectBlogAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var existing = await _blogRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException("Blog", id);
        }

        existing.Status = BlogStatus.Rejected;
        await _blogRepository.UpdateAsync(existing, cancellationToken);

        var updated = await _blogRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BlogServiceException("Không thể tải lại bài viết sau khi từ chối.");
        var relatedBlogs = await _blogRepository.GetRelatedBlogsAsync(id, limit: 10, cancellationToken);

        return await BuildBlogDetailWithRelationsAsync(updated, relatedBlogs, cancellationToken);
    }

    private async Task EnsureCategoryExists(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue)
        {
            return;
        }

        var exists = await _categoryRepository.ExistsAsync(categoryId.Value, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("BlogCategory", categoryId.Value);
        }
    }

    private async Task<BlogDetailDto> BuildBlogDetailWithRelationsAsync(
        BlogEntity mainBlog,
        IReadOnlyList<BlogEntity> relatedBlogs,
        CancellationToken cancellationToken)
    {
        var blogDto = _mapper.Map<BlogDetailDto>(mainBlog);
        var relatedBlogsDto = _mapper.Map<IReadOnlyList<BlogSummaryDto>>(relatedBlogs);

        // Batch load creator names for main blog and related blogs
        var allAccountIds = new List<Guid>();
        if (mainBlog.CreatedBy.HasValue)
        {
            allAccountIds.Add(mainBlog.CreatedBy.Value);
        }

        allAccountIds.AddRange(relatedBlogs
            .Where(b => b.CreatedBy.HasValue)
            .Select(b => b.CreatedBy!.Value)
            .Distinct());

        var creatorNames = await GetCreatorNamesBatchAsync(allAccountIds.Distinct().ToList(), cancellationToken);

        // Get creator name for main blog
        var creatorName = mainBlog.CreatedBy.HasValue &&
                          creatorNames.TryGetValue(mainBlog.CreatedBy.Value, out var name)
            ? name
            : null;

        // Set CreatedByName for related blogs
        var relatedBlogsWithCreatorNames = relatedBlogsDto.Select(dto =>
        {
            var relatedBlog = relatedBlogs.First(b => b.Id == dto.Id);
            var relatedCreatorName = relatedBlog.CreatedBy.HasValue &&
                                     creatorNames.TryGetValue(relatedBlog.CreatedBy.Value, out var relatedName)
                ? relatedName
                : null;
            return dto with { CreatedByName = relatedCreatorName };
        }).ToList();

        return blogDto with
        {
            RelatedBlogs = relatedBlogsWithCreatorNames,
            CreatedByName = creatorName
        };
    }

    private async Task<Dictionary<Guid, string>> GetCreatorNamesBatchAsync(
        List<Guid> accountIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, string>();

        if (!accountIds.Any())
        {
            return result;
        }

        try
        {
            var request = new GetUsersByAccountIdsRequest();
            request.AccountIds.AddRange(accountIds.Select(id => id.ToString()));

            var response = await _userClient.GetUsersByAccountIdsAsync(request, cancellationToken: cancellationToken);

            foreach (var user in response.Users)
            {
                if (Guid.TryParse(user.AccountId, out var accountId))
                {
                    var fullName = !string.IsNullOrWhiteSpace(user.FullName)
                        ? user.FullName
                        : string.Empty;
                    result[accountId] = fullName;
                }
            }
        }
        catch (RpcException)
        {
            // If service unavailable, return empty dictionary
        }

        return result;
    }

    private static BlogFilterParameters SanitizeFilter(BlogFilterParameters filter)
    {
        return new BlogFilterParameters
        {
            CategoryId = filter.CategoryId,
            CreatedByAccountId = filter.CreatedByAccountId,
            CreatedByDoctorId = filter.CreatedByDoctorId,
            CreatedByHospitalId = filter.CreatedByHospitalId,
            Tag = filter.Tag,
            Source = filter.Source,
            Status = filter.Status,
            Featured = filter.Featured,
            Keyword = filter.Keyword,
            Page = filter.Page <= 0 ? 1 : filter.Page,
            PageSize = filter.PageSize is < 1 or > 100 ? 10 : filter.PageSize
        };
    }
}


