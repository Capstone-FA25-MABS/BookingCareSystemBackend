using System.Linq;
using AutoMapper;
using BookingCare.Services.Blog.Exceptions;
using BookingCare.Services.Blog.Models.DTOs;
using BookingCare.Services.Blog.Models.Entities;
using BookingCare.Services.Blog.Repositories;
using BookingCare.Services.User.Protos;
using BookingCare.Shared.Common.Exceptions;
using Grpc.Core;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;

namespace BookingCare.Services.Blog.Services;

public class BlogService : IBlogService
{
    private readonly IBlogRepository _blogRepository;
    private readonly IBlogCategoryRepository _categoryRepository;
    private readonly IMapper _mapper;
    private readonly UserService.UserServiceClient _userClient;
    private readonly DoctorService.DoctorServiceClient _doctorClient;
    private readonly HospitalService.HospitalServiceClient _hospitalClient;

    public BlogService(
        IBlogRepository blogRepository,
        IBlogCategoryRepository categoryRepository,
        IMapper mapper,
        UserService.UserServiceClient userClient,
        DoctorService.DoctorServiceClient doctorClient,
        HospitalService.HospitalServiceClient hospitalClient)
    {
        _blogRepository = blogRepository;
        _categoryRepository = categoryRepository;
        _mapper = mapper;
        _userClient = userClient;
        _doctorClient = doctorClient;
        _hospitalClient = hospitalClient;
    }

    public async Task<PagedResponse<BlogSummaryDto>> GetBlogsAsync(BlogFilterParameters filter, CancellationToken cancellationToken = default)
    {
        var sanitizedFilter = SanitizeFilter(filter);
        var (blogs, totalItems) = await _blogRepository.GetBlogsAsync(sanitizedFilter, cancellationToken);
        var summaries = _mapper.Map<IReadOnlyList<BlogSummaryDto>>(blogs);

        // Batch load creator names (account/doctor/hospital)
        var accountIds = blogs.Where(b => b.CreatedBy.HasValue).Select(b => b.CreatedBy!.Value);
        var doctorIds = blogs.Where(b => b.CreatedByDoctorId.HasValue).Select(b => b.CreatedByDoctorId!.Value);
        var hospitalIds = blogs.Where(b => b.CreatedByHospitalId.HasValue).Select(b => b.CreatedByHospitalId!.Value);

        var (accountNames, doctorNames, hospitalNames) = await GetCreatorNamesBatchAsync(
            accountIds,
            doctorIds,
            hospitalIds,
            cancellationToken);

        // Set CreatedByName for all summaries
        var summariesWithCreatorNames = summaries.Select(dto =>
        {
            var blog = blogs.First(b => b.Id == dto.Id);
            var creatorName = ResolveCreatorName(blog, accountNames, doctorNames, hospitalNames);
            return dto with { CreatedByName = creatorName };
        }).ToList();

        return new PagedResponse<BlogSummaryDto>(summariesWithCreatorNames, totalItems, sanitizedFilter.Page, sanitizedFilter.PageSize);
    }

    public async Task<IReadOnlyList<BlogDetailDto>> GetAllBlogsAsync(CancellationToken cancellationToken = default)
    {
        var blogs = await _blogRepository.GetAllBlogsAsync(cancellationToken);
        var blogDtos = _mapper.Map<IReadOnlyList<BlogDetailDto>>(blogs);

        var accountIds = blogs.Where(b => b.CreatedBy.HasValue).Select(b => b.CreatedBy!.Value);
        var doctorIds = blogs.Where(b => b.CreatedByDoctorId.HasValue).Select(b => b.CreatedByDoctorId!.Value);
        var hospitalIds = blogs.Where(b => b.CreatedByHospitalId.HasValue).Select(b => b.CreatedByHospitalId!.Value);

        var (accountNames, doctorNames, hospitalNames) = await GetCreatorNamesBatchAsync(
            accountIds,
            doctorIds,
            hospitalIds,
            cancellationToken);

        // Set RelatedBlogs to empty list and CreatedByName for all blogs
        return blogDtos.Select(dto =>
        {
            var blog = blogs.First(b => b.Id == dto.Id);
            var creatorName = ResolveCreatorName(blog, accountNames, doctorNames, hospitalNames);
            return dto with
            {
                RelatedBlogs = Array.Empty<BlogSummaryDto>(),
                CreatedByName = creatorName
            };
        }).ToList();
    }

    public async Task<BlogDetailDto?> GetBlogByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var blog = await _blogRepository.GetByIdAsync(id, cancellationToken);
        if (blog is null)
        {
            return null;
        }
        var relatedBlogs = await _blogRepository.GetRelatedBlogsAsync(id, limit: 10, cancellationToken);

        return await BuildBlogDetailWithRelationsAsync(blog, relatedBlogs, cancellationToken);
    }

    public async Task<BlogDetailDto> CreateBlogAsync(CreateBlogRequest request, Guid? createdBy = null, CancellationToken cancellationToken = default)
    {
        await EnsureCategoryExists(request.BlogCategoryId, cancellationToken);

        var entity = _mapper.Map<BlogEntity>(request);
        entity.CreatedBy = createdBy;
        entity.CreatedByDoctorId = request.CreatedByDoctorId;
        entity.CreatedByHospitalId = request.CreatedByHospitalId;
        await _blogRepository.AddAsync(entity, cancellationToken);

        var created = await _blogRepository.GetByIdAsync(entity.Id, cancellationToken)
            ?? throw new BlogServiceException("Không thể tải lại bài viết sau khi tạo.");
        var relatedBlogs = await _blogRepository.GetRelatedBlogsAsync(entity.Id, limit: 10, cancellationToken);

        return await BuildBlogDetailWithRelationsAsync(created, relatedBlogs, cancellationToken);
    }

    public async Task<BlogDetailDto> UpdateBlogAsync(Guid id, UpdateBlogRequest request, CancellationToken cancellationToken = default)
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
        existing.Status = request.Status;
        existing.Featured = request.Featured;
        existing.PublishedAt = request.PublishedAt;

        await _blogRepository.UpdateAsync(existing, cancellationToken);

        var updated = await _blogRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BlogServiceException("Không thể tải lại bài viết sau khi cập nhật.");
        var relatedBlogs = await _blogRepository.GetRelatedBlogsAsync(id, limit: 10, cancellationToken);

        return await BuildBlogDetailWithRelationsAsync(updated, relatedBlogs, cancellationToken);
    }

    public async Task DeleteBlogAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _blogRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException("Blog", id);
        }

        await _blogRepository.DeleteAsync(existing, cancellationToken);
    }

    public async Task<BlogDetailDto> ApproveBlogAsync(Guid id, bool? featured = null, CancellationToken cancellationToken = default)
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

    public async Task<BlogDetailDto> RejectBlogAsync(Guid id, CancellationToken cancellationToken = default)
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
        var accountIds = new List<Guid>();
        var doctorIds = new List<Guid>();
        var hospitalIds = new List<Guid>();

        if (mainBlog.CreatedBy.HasValue) accountIds.Add(mainBlog.CreatedBy.Value);
        if (mainBlog.CreatedByDoctorId.HasValue) doctorIds.Add(mainBlog.CreatedByDoctorId.Value);
        if (mainBlog.CreatedByHospitalId.HasValue) hospitalIds.Add(mainBlog.CreatedByHospitalId.Value);

        accountIds.AddRange(relatedBlogs.Where(b => b.CreatedBy.HasValue).Select(b => b.CreatedBy!.Value));
        doctorIds.AddRange(relatedBlogs.Where(b => b.CreatedByDoctorId.HasValue).Select(b => b.CreatedByDoctorId!.Value));
        hospitalIds.AddRange(relatedBlogs.Where(b => b.CreatedByHospitalId.HasValue).Select(b => b.CreatedByHospitalId!.Value));

        var (accountNames, doctorNames, hospitalNames) = await GetCreatorNamesBatchAsync(
            accountIds,
            doctorIds,
            hospitalIds,
            cancellationToken);

        // Get creator name for main blog
        var creatorName = ResolveCreatorName(mainBlog, accountNames, doctorNames, hospitalNames);

        // Set CreatedByName for related blogs
        var relatedBlogsWithCreatorNames = relatedBlogsDto.Select(dto =>
        {
            var relatedBlog = relatedBlogs.First(b => b.Id == dto.Id);
            var relatedCreatorName = ResolveCreatorName(relatedBlog, accountNames, doctorNames, hospitalNames);
            return dto with { CreatedByName = relatedCreatorName };
        }).ToList();

        return blogDto with
        {
            RelatedBlogs = relatedBlogsWithCreatorNames,
            CreatedByName = creatorName
        };
    }

    private static string? ResolveCreatorName(
        BlogEntity blog,
        IReadOnlyDictionary<Guid, string> accountNames,
        IReadOnlyDictionary<Guid, string> doctorNames,
        IReadOnlyDictionary<Guid, string> hospitalNames)
    {
        if (blog.CreatedByDoctorId.HasValue &&
            doctorNames.TryGetValue(blog.CreatedByDoctorId.Value, out var doctorName))
        {
            return doctorName;
        }

        if (blog.CreatedByHospitalId.HasValue &&
            hospitalNames.TryGetValue(blog.CreatedByHospitalId.Value, out var hospitalName))
        {
            return hospitalName;
        }

        if (blog.CreatedBy.HasValue &&
            accountNames.TryGetValue(blog.CreatedBy.Value, out var accountName))
        {
            return accountName;
        }

        return null;
    }

    private async Task<(Dictionary<Guid, string> accountNames, Dictionary<Guid, string> doctorNames, Dictionary<Guid, string> hospitalNames)> GetCreatorNamesBatchAsync(
        IEnumerable<Guid> accountIds,
        IEnumerable<Guid> doctorIds,
        IEnumerable<Guid> hospitalIds,
        CancellationToken cancellationToken = default)
    {
        var accountNames = new Dictionary<Guid, string>();
        var doctorNames = new Dictionary<Guid, string>();
        var hospitalNames = new Dictionary<Guid, string>();

        var accountList = accountIds?.Distinct().ToList() ?? new List<Guid>();
        var doctorList = doctorIds?.Distinct().ToList() ?? new List<Guid>();
        var hospitalList = hospitalIds?.Distinct().ToList() ?? new List<Guid>();

        // Accounts
        if (accountList.Any())
        {
            try
            {
                var request = new GetUsersByAccountIdsRequest();
                request.AccountIds.AddRange(accountList.Select(id => id.ToString()));

                var response = await _userClient.GetUsersByAccountIdsAsync(request, cancellationToken: cancellationToken);

                foreach (var user in response.Users)
                {
                    if (Guid.TryParse(user.AccountId, out var accountId))
                    {
                        var fullName = !string.IsNullOrWhiteSpace(user.FullName)
                            ? user.FullName
                            : string.Empty;
                        accountNames[accountId] = fullName;
                    }
                }
            }
            catch (RpcException)
            {
                // ignore
            }
        }

        // Doctors (basic info)
        if (doctorList.Any())
        {
            try
            {
                var request = new GetDoctorsBasicInfoRequest();
                request.Ids.AddRange(doctorList.Select(id => id.ToString()));
                var response = await _doctorClient.GetDoctorsBasicInfoAsync(request, cancellationToken: cancellationToken);
                foreach (var doc in response.Doctors)
                {
                    if (Guid.TryParse(doc.Id, out var id))
                    {
                        doctorNames[id] = doc.FullName ?? string.Empty;
                    }
                }

                // Also try by account IDs (in case column stores account_id)
                var accountRequest = new GetDoctorsByAccountIdsRequest();
                accountRequest.AccountIds.AddRange(doctorList.Select(id => id.ToString()));
                var accountResponse = await _doctorClient.GetDoctorsByAccountIdsAsync(accountRequest, cancellationToken: cancellationToken);
                foreach (var doc in accountResponse.Doctors)
                {
                    if (Guid.TryParse(doc.AccountId, out var accId))
                    {
                        doctorNames[accId] = doc.FullName ?? string.Empty;
                    }
                }
            }
            catch (RpcException)
            {
                // ignore
            }
        }

        // Hospitals (names only)
        if (hospitalList.Any())
        {
            try
            {
                // Attempt by hospital Ids
                var request = new GetHospitalNamesRequest();
                request.Ids.AddRange(hospitalList.Select(id => id.ToString()));
                var response = await _hospitalClient.GetHospitalNamesAsync(request, cancellationToken: cancellationToken);
                foreach (var hos in response.Hospitals)
                {
                    if (Guid.TryParse(hos.Id, out var id))
                    {
                        hospitalNames[id] = hos.Name ?? string.Empty;
                    }
                }

                // Also try by account Ids (in case column stores account_id)
                var accountRequest = new GetHospitalsByAccountIdsRequest();
                accountRequest.AccountIds.AddRange(hospitalList.Select(id => id.ToString()));
                var accountResponse = await _hospitalClient.GetHospitalsByAccountIdsAsync(accountRequest, cancellationToken: cancellationToken);
                foreach (var hos in accountResponse.Hospitals)
                {
                    if (Guid.TryParse(hos.AccountId, out var accId))
                    {
                        hospitalNames[accId] = hos.FullName ?? hos.Email ?? string.Empty;
                    }
                }
            }
            catch (RpcException)
            {
                // ignore
            }
        }

        return (accountNames, doctorNames, hospitalNames);
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

