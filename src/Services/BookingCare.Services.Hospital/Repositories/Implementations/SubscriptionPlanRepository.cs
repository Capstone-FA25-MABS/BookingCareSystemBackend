using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Shared.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Hospital.Repositories.Implementations;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly HospitalDbContext _context;

    public SubscriptionPlanRepository(HospitalDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlanEntity?> GetByIdAsync(Guid id)
    {
        return await _context.SubscriptionPlans
            .Include(s => s.HospitalSubscriptions)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SubscriptionPlanEntity?> GetByNameAsync(string name)
    {
        return await _context.SubscriptionPlans
            .FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<List<SubscriptionPlanEntity>> GetAllAsync()
    {
        return await _context.SubscriptionPlans
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<(List<SubscriptionPlanEntity> plans, int totalCount)> GetFilteredAsync(SubscriptionPlanFilterRequest filter)
    {
        var query = _context.SubscriptionPlans.AsQueryable();

        query = ApplyFilters(query, filter);
        var totalCount = await query.CountAsync();
        query = ApplySorting(query, filter);
        var plans = await ApplyPagination(query, filter).ToListAsync();

        return (plans, totalCount);
    }

    private IQueryable<SubscriptionPlanEntity> ApplyFilters(IQueryable<SubscriptionPlanEntity> query, SubscriptionPlanFilterRequest filter)
    {
        if (!string.IsNullOrEmpty(filter.Name))
            query = query.Where(s => s.Name.Contains(filter.Name));

        if (filter.Status.HasValue)
            query = query.Where(s => s.Status == filter.Status.Value);

        if (filter.MinPrice.HasValue)
            query = query.Where(s => s.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(s => s.Price <= filter.MaxPrice.Value);

        if (!string.IsNullOrEmpty(filter.BillingCycle))
            query = query.Where(s => s.BillingCycle == filter.BillingCycle);

        return query;
    }

    private IQueryable<SubscriptionPlanEntity> ApplySorting(IQueryable<SubscriptionPlanEntity> query, SubscriptionPlanFilterRequest filter)
    {
        if (string.IsNullOrEmpty(filter.SortBy))
            return query.OrderBy(s => s.Name);

        return filter.SortBy.ToLower() switch
        {
            "name" => filter.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(s => s.Name)
                : query.OrderBy(s => s.Name),
            "price" => filter.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(s => s.Price)
                : query.OrderBy(s => s.Price),
            "createdat" => filter.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(s => s.CreatedAt)
                : query.OrderBy(s => s.CreatedAt),
            _ => query.OrderBy(s => s.Name)
        };
    }

    private IQueryable<SubscriptionPlanEntity> ApplyPagination(IQueryable<SubscriptionPlanEntity> query, SubscriptionPlanFilterRequest filter)
    {
        return query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize);
    }

    public async Task<SubscriptionPlanEntity> CreateAsync(SubscriptionPlanEntity plan)
    {
        _context.SubscriptionPlans.Add(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task<SubscriptionPlanEntity> UpdateAsync(SubscriptionPlanEntity plan)
    {
        _context.SubscriptionPlans.Update(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan == null)
            return false;

        _context.SubscriptionPlans.Remove(plan);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.SubscriptionPlans.AnyAsync(s => s.Id == id);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        var query = _context.SubscriptionPlans.Where(s => s.Name == name);
        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<List<SubscriptionPlanEntity>> GetActiveAsync()
    {
        return await _context.SubscriptionPlans
            .Where(s => s.Status == Status.ACTIVE)
            .OrderBy(s => s.Price)
            .ToListAsync();
    }
}
