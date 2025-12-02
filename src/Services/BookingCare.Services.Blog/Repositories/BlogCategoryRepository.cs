using BookingCare.Services.Blog.Data;
using BookingCare.Services.Blog.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Blog.Repositories;

public class BlogCategoryRepository : IBlogCategoryRepository
{
    private readonly BlogDbContext _dbContext;
    private readonly ILogger<BlogCategoryRepository> _logger;
    private const string ServiceName = "BlogCategoryRepository";

    public BlogCategoryRepository(BlogDbContext dbContext, ILogger<BlogCategoryRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BlogCategoryEntity>> GetAllAsync(bool includeChildren, CancellationToken cancellationToken = default)
    {
        IQueryable<BlogCategoryEntity> query = _dbContext.BlogCategories.AsNoTracking();

        if (includeChildren)
        {
            query = query.Include(c => c.Children);
        }

        return await query
            .OrderBy(c => c.CategoryName)
            .ToListAsync(cancellationToken);
    }

    public async Task<BlogCategoryEntity?> GetByIdAsync(Guid id, bool includeChildren = false, CancellationToken cancellationToken = default)
    {
        IQueryable<BlogCategoryEntity> query = _dbContext.BlogCategories;

        if (includeChildren)
        {
            query = query.Include(c => c.Children);
        }
        query = query.Include(c => c.Blogs);
        return await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.BlogCategories.AnyAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(BlogCategoryEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.BlogCategories.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{ServiceName} created category {CategoryName} ({CategoryId})", ServiceName, entity.CategoryName, entity.Id);
    }

    public async Task UpdateAsync(BlogCategoryEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.BlogCategories.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{ServiceName} updated category {CategoryId}", ServiceName, entity.Id);
    }

    public async Task DeleteAsync(BlogCategoryEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.BlogCategories.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{ServiceName} deleted category {CategoryId}", ServiceName, entity.Id);
    }
}

