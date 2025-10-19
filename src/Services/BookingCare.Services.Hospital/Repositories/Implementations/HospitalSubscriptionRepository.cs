using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Hospital.Repositories.Implementations;

public class HospitalSubscriptionRepository : IHospitalSubscriptionRepository
{
    private readonly HospitalDbContext _context;

    public HospitalSubscriptionRepository(HospitalDbContext context)
    {
        _context = context;
    }

    public async Task<HospitalSubscriptionEntity?> GetByIdAsync(Guid id)
    {
        return await _context.HospitalSubscriptions
            .Include(hs => hs.Hospital)
            .Include(hs => hs.SubscriptionPlan)
            .FirstOrDefaultAsync(hs => hs.HospitalSubscriptionId == id);
    }

    public async Task<List<HospitalSubscriptionEntity>> GetAllAsync()
    {
        return await _context.HospitalSubscriptions
            .Include(hs => hs.Hospital)
            .Include(hs => hs.SubscriptionPlan)
            .OrderByDescending(hs => hs.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<HospitalSubscriptionEntity> subscriptions, int totalCount)> GetFilteredAsync(HospitalSubscriptionFilterRequest filter)
    {
        var query = _context.HospitalSubscriptions
            .Include(hs => hs.Hospital)
            .Include(hs => hs.SubscriptionPlan)
            .AsQueryable();

        query = ApplyFilters(query, filter);
        var totalCount = await query.CountAsync();
        query = ApplySorting(query, filter);
        var subscriptions = await ApplyPagination(query, filter).ToListAsync();

        return (subscriptions, totalCount);
    }

    private IQueryable<HospitalSubscriptionEntity> ApplyFilters(IQueryable<HospitalSubscriptionEntity> query, HospitalSubscriptionFilterRequest filter)
    {
        if (filter.HospitalId.HasValue)
            query = query.Where(hs => hs.HospitalId == filter.HospitalId.Value);

        if (filter.SubscriptionId.HasValue)
            query = query.Where(hs => hs.SubscriptionId == filter.SubscriptionId.Value);

        if (filter.Status.HasValue)
            query = query.Where(hs => hs.Status == filter.Status.Value);

        if (filter.StartDateFrom.HasValue)
            query = query.Where(hs => hs.StartDate >= filter.StartDateFrom.Value);

        if (filter.StartDateTo.HasValue)
            query = query.Where(hs => hs.StartDate <= filter.StartDateTo.Value);

        if (filter.EndDateFrom.HasValue)
            query = query.Where(hs => hs.EndDate >= filter.EndDateFrom.Value);

        if (filter.EndDateTo.HasValue)
            query = query.Where(hs => hs.EndDate <= filter.EndDateTo.Value);

        return query;
    }

    private IQueryable<HospitalSubscriptionEntity> ApplySorting(IQueryable<HospitalSubscriptionEntity> query, HospitalSubscriptionFilterRequest filter)
    {
        if (string.IsNullOrEmpty(filter.SortBy))
            return query.OrderByDescending(hs => hs.CreatedAt);

        return filter.SortBy.ToLower() switch
        {
            "startdate" => filter.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(hs => hs.StartDate)
                : query.OrderBy(hs => hs.StartDate),
            "enddate" => filter.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(hs => hs.EndDate)
                : query.OrderBy(hs => hs.EndDate),
            "createdat" => filter.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(hs => hs.CreatedAt)
                : query.OrderBy(hs => hs.CreatedAt),
            _ => query.OrderByDescending(hs => hs.CreatedAt)
        };
    }

    private IQueryable<HospitalSubscriptionEntity> ApplyPagination(IQueryable<HospitalSubscriptionEntity> query, HospitalSubscriptionFilterRequest filter)
    {
        return query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize);
    }

    public async Task<HospitalSubscriptionEntity> CreateAsync(HospitalSubscriptionEntity subscription)
    {
        _context.HospitalSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Reload with includes
        return await GetByIdAsync(subscription.HospitalSubscriptionId) ?? subscription;
    }

    public async Task<HospitalSubscriptionEntity> UpdateAsync(HospitalSubscriptionEntity subscription)
    {
        _context.HospitalSubscriptions.Update(subscription);
        await _context.SaveChangesAsync();

        // Reload with includes
        return await GetByIdAsync(subscription.HospitalSubscriptionId) ?? subscription;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var subscription = await _context.HospitalSubscriptions.FindAsync(id);
        if (subscription == null)
            return false;

        _context.HospitalSubscriptions.Remove(subscription);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.HospitalSubscriptions.AnyAsync(hs => hs.HospitalSubscriptionId == id);
    }

    public async Task<List<HospitalSubscriptionEntity>> GetByHospitalIdAsync(Guid hospitalId)
    {
        return await _context.HospitalSubscriptions
            .Include(hs => hs.Hospital)
            .Include(hs => hs.SubscriptionPlan)
            .Where(hs => hs.HospitalId == hospitalId)
            .OrderByDescending(hs => hs.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<HospitalSubscriptionEntity>> GetBySubscriptionIdAsync(Guid subscriptionId)
    {
        return await _context.HospitalSubscriptions
            .Include(hs => hs.Hospital)
            .Include(hs => hs.SubscriptionPlan)
            .Where(hs => hs.SubscriptionId == subscriptionId)
            .OrderByDescending(hs => hs.CreatedAt)
            .ToListAsync();
    }

    public async Task<HospitalSubscriptionEntity?> GetActiveByHospitalIdAsync(Guid hospitalId)
    {
        var now = DateTime.Now;
        return await _context.HospitalSubscriptions
            .Include(hs => hs.Hospital)
            .Include(hs => hs.SubscriptionPlan)
            .Where(hs => hs.HospitalId == hospitalId)
            .Where(hs => hs.Status == SubscriptionStatus.ACTIVE || hs.Status == SubscriptionStatus.TRIAL)
            .Where(hs => hs.StartDate <= now && hs.EndDate >= now)
            .OrderByDescending(hs => hs.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<HospitalSubscriptionEntity>> GetExpiringSoonAsync(int days = 30)
    {
        var now = DateTime.Now;
        var targetDate = now.AddDays(days);

        return await _context.HospitalSubscriptions
            .Include(hs => hs.Hospital)
            .Include(hs => hs.SubscriptionPlan)
            .Where(hs => hs.Status == SubscriptionStatus.ACTIVE || hs.Status == SubscriptionStatus.TRIAL)
            .Where(hs => hs.EndDate >= now && hs.EndDate <= targetDate)
            .OrderBy(hs => hs.EndDate)
            .ToListAsync();
    }
}
