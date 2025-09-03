using BookingCare.Services.Doctor.Data;
using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Shared.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Doctor.Repositories;

public class PriceRepository : IPriceRepository
{
    private readonly DoctorDbContext _context;

    public PriceRepository(DoctorDbContext context)
    {
        _context = context;
    }

    #region Price CRUD Operations

    public async Task<PriceEntity?> GetPriceByIdAsync(Guid id)
    {
        return await _context.Prices
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PriceEntity> CreatePriceAsync(PriceEntity price)
    {
        _context.Prices.Add(price);
        await _context.SaveChangesAsync();
        return price;
    }

    public async Task<PriceEntity> UpdatePriceAsync(PriceEntity price)
    {
        _context.Prices.Update(price);
        await _context.SaveChangesAsync();
        return price;
    }

    public async Task<bool> DeletePriceAsync(Guid id)
    {
        var price = await GetPriceByIdAsync(id);
        if (price == null) return false;

        _context.Prices.Remove(price);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PriceExistsAsync(Guid id)
    {
        return await _context.Prices
            .AnyAsync(p => p.Id == id);
    }

    #endregion

    #region Price Query Operations

    public async Task<(List<PriceEntity> Prices, int TotalCount)> GetPricesAsync(PriceQueryRequest query)
    {
        var queryable = _context.Prices.AsQueryable();

        // Apply amount filters
        if (query.MinAmount.HasValue)
        {
            queryable = queryable.Where(p => p.Amount >= query.MinAmount.Value);
        }

        if (query.MaxAmount.HasValue)
        {
            queryable = queryable.Where(p => p.Amount <= query.MaxAmount.Value);
        }

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var prices = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (prices, totalCount);
    }

    public async Task<List<PriceEntity>> GetAllPricesAsync()
    {
        return await _context.Prices.ToListAsync();
    }

    #endregion

    #region PriceRule Operations

    public async Task<PriceRuleEntity?> GetActivePriceRuleAsync(
        int? minExperience = null,
        string? position = null)
    {
        var query = _context.PriceRules.Where(r => r.Status == Status.ACTIVE);
        if (minExperience.HasValue)
            query = query.Where(r => !r.MinExperience.HasValue || minExperience >= r.MinExperience);
        if (!string.IsNullOrEmpty(position))
            query = query.Where(r => r.Position == position);
        return await query.FirstOrDefaultAsync();
    }

    #endregion
}
