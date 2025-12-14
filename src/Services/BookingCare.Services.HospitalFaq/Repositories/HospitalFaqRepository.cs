using BookingCare.Services.HospitalFaq.Data;
using BookingCare.Services.HospitalFaq.Models.DTOs;
using BookingCare.Services.HospitalFaq.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.HospitalFaq.Repositories;

public class HospitalFaqRepository : IHospitalFaqRepository
{
    private readonly HospitalFaqDbContext _dbContext;
    private readonly ILogger<HospitalFaqRepository> _logger;
    private const string ServiceName = "HospitalFaqRepository";

    public HospitalFaqRepository(HospitalFaqDbContext dbContext, ILogger<HospitalFaqRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<HospitalFaqEntity> Faqs, int TotalItems)> GetFaqsAsync(HospitalFaqFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.HospitalFaqs
            .AsNoTracking()
            .AsQueryable();

        if (filter.HospitalId.HasValue)
        {
            query = query.Where(f => f.HospitalId == filter.HospitalId.Value);
        }

        var page = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize is < 1 or > 100 ? 10 : filter.PageSize;

        var totalItems = await query.CountAsync(cancellationToken);

        var faqs = await query
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (faqs, totalItems);
    }

    public async Task<IReadOnlyList<HospitalFaqEntity>> GetFaqsByHospitalIdAsync(Guid hospitalId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.HospitalFaqs
            .Where(f => f.HospitalId == hospitalId)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<HospitalFaqEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.HospitalFaqs
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task AddAsync(HospitalFaqEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.HospitalFaqs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{ServiceName} created FAQ {FaqId} for hospital {HospitalId}", ServiceName, entity.Id, entity.HospitalId);
    }

    public async Task UpdateAsync(HospitalFaqEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.HospitalFaqs.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{ServiceName} updated FAQ {FaqId}", ServiceName, entity.Id);
    }

    public async Task DeleteAsync(HospitalFaqEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.HospitalFaqs.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{ServiceName} deleted FAQ {FaqId}", ServiceName, entity.Id);
    }
}

