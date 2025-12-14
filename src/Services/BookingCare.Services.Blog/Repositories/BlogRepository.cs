using BookingCare.Services.Blog.Data;
using BookingCare.Services.Blog.Models.DTOs;
using BookingCare.Services.Blog.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Blog.Repositories;

public class BlogRepository : IBlogRepository
{
    private readonly BlogDbContext _dbContext;
    private readonly ILogger<BlogRepository> _logger;
    private const string ServiceName = "BlogRepository";

    public BlogRepository(BlogDbContext dbContext, ILogger<BlogRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<BlogEntity> Blogs, int TotalItems)> GetBlogsAsync(BlogFilterParameters filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Blogs
            .Include(b => b.Category)
            .AsNoTracking()
            .AsQueryable();

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(b => b.BlogCategoryId == filter.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Tag))
        {
            query = query.Where(b => b.Tag != null && b.Tag.Contains(filter.Tag));
        }

        if (!string.IsNullOrWhiteSpace(filter.Source))
        {
            query = query.Where(b => b.Source != null && b.Source.Contains(filter.Source));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(b => b.Status == filter.Status.Value);
        }

        if (filter.Featured.HasValue)
        {
            query = query.Where(b => b.Featured == filter.Featured.Value);
        }

        if (filter.CreatedByAccountId.HasValue)
        {
            query = query.Where(b => b.CreatedBy == filter.CreatedByAccountId.Value);
        }

        if (filter.CreatedByDoctorId.HasValue)
        {
            query = query.Where(b => b.CreatedByDoctorId == filter.CreatedByDoctorId.Value);
        }

        if (filter.CreatedByHospitalId.HasValue)
        {
            query = query.Where(b => b.CreatedByHospitalId == filter.CreatedByHospitalId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            query = query.Where(b =>
                b.TitleVi.Contains(filter.Keyword) ||
                (b.TitleEn != null && b.TitleEn.Contains(filter.Keyword)) ||
                b.ContentVi.Contains(filter.Keyword));
        }

        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 10 : filter.PageSize;

        var totalItems = await query.CountAsync(cancellationToken);

        var blogs = await query
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (blogs, totalItems);
    }

    public async Task<IReadOnlyList<BlogEntity>> GetAllBlogsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Blogs
            .Include(b => b.Category)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<BlogEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Blogs
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task AddAsync(BlogEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Blogs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{ServiceName} created blog {BlogId}", ServiceName, entity.Id);
    }

    public async Task UpdateAsync(BlogEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Blogs.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{ServiceName} updated blog {BlogId}", ServiceName, entity.Id);
    }

    public async Task DeleteAsync(BlogEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Blogs.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{ServiceName} deleted blog {BlogId}", ServiceName, entity.Id);
    }

    public async Task<IReadOnlyList<BlogEntity>> GetRelatedBlogsAsync(Guid blogId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var blog = await _dbContext.Blogs
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == blogId, cancellationToken);

        if (blog == null || !blog.BlogCategoryId.HasValue)
        {
            return new List<BlogEntity>();
        }

        return await _dbContext.Blogs
            .Include(b => b.Category)
            .Where(b => b.Id != blogId
                && b.BlogCategoryId == blog.BlogCategoryId
                && b.Status == BlogStatus.Active)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}

