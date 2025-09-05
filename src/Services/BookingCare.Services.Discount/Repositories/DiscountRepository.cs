using BookingCare.Services.Discount.Data;
using BookingCare.Services.Discount.Enums;
using BookingCare.Services.Discount.Models.DTOs;
using BookingCare.Services.Discount.Models.Entities;
using BookingCare.Shared.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Discount.Repositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly DiscountDbContext _context;

    public DiscountRepository(DiscountDbContext context)
    {
        _context = context;
    }

    public async Task<DiscountEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Discounts
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DiscountEntity?> GetByCodeAsync(string code)
    {
        return await _context.Discounts
            .FirstOrDefaultAsync(d => d.Code == code);
    }

    public async Task<DiscountEntity> CreateAsync(DiscountEntity discount)
    {
        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync();
        return discount;
    }

    public async Task<DiscountEntity> UpdateAsync(DiscountEntity discount)
    {
        _context.Discounts.Update(discount);
        await _context.SaveChangesAsync();
        return discount;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var discount = await GetByIdAsync(id);
        if (discount == null) return false;

        _context.Discounts.Remove(discount);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Discounts
            .AnyAsync(d => d.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null)
    {
        var query = _context.Discounts.Where(d => d.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(d => d.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<(List<DiscountEntity> Discounts, int TotalCount)> GetDiscountsAsync(DiscountQueryRequest query)
    {
        var queryable = _context.Discounts.AsQueryable();

        // Apply filters
        if (query.ClinicId.HasValue)
        {
            queryable = queryable.Where(d => d.ClinicId == query.ClinicId.Value);
        }

        if (query.SpecialtyId.HasValue)
        {
            queryable = queryable.Where(d => d.SpecialtyId == query.SpecialtyId.Value || d.ApplicableTo == DiscountApplicableTo.ALL);
        }

        if (query.DoctorId.HasValue)
        {
            queryable = queryable.Where(d => d.DoctorId == query.DoctorId.Value || d.ApplicableTo == DiscountApplicableTo.ALL || d.ApplicableTo == DiscountApplicableTo.SPECIALTY);
        }

        if (!string.IsNullOrEmpty(query.Status.ToString()))
        {
            queryable = queryable.Where(d => d.Status == query.Status);
        }

        if (!string.IsNullOrEmpty(query.ApplicableTo.ToString()))
        {
            queryable = queryable.Where(d => d.ApplicableTo == query.ApplicableTo);
        }

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            queryable = queryable.Where(d =>
                d.Code.Contains(query.SearchTerm) ||
                d.Name.Contains(query.SearchTerm) ||
                (d.Description != null && d.Description.Contains(query.SearchTerm)));
        }

        if (query.StartDate.HasValue)
        {
            queryable = queryable.Where(d => d.StartDate >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            queryable = queryable.Where(d => d.EndDate <= query.EndDate.Value);
        }

        var totalCount = await queryable.CountAsync();

        var discounts = await queryable
            .OrderByDescending(d => d.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (discounts, totalCount);
    }

    public async Task<List<DiscountEntity>> GetActiveDiscountsByClinicAsync(Guid clinicId)
    {
        var now = DateTime.UtcNow;
        return await _context.Discounts
            .Where(d => d.ClinicId == clinicId &&
                       d.Status == DiscountStatus.ACTIVE &&
                       d.StartDate <= now &&
                       d.EndDate >= now)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<List<DiscountEntity>> GetApplicableDiscountsAsync(Guid clinicId, Guid? specialtyId = null, Guid? doctorId = null)
    {
        var now = DateTime.UtcNow;
        var query = _context.Discounts
            .Where(d => d.ClinicId == clinicId &&
                       d.Status == DiscountStatus.ACTIVE &&
                       d.StartDate <= now &&
                       d.EndDate >= now &&
                       (d.MaxUses == null || d.UsesCount < d.MaxUses));

        // Apply applicability filters
        query = query.Where(d =>
            d.ApplicableTo == DiscountApplicableTo.ALL ||
            (d.ApplicableTo == DiscountApplicableTo.SPECIALTY && d.SpecialtyId == specialtyId) ||
            (d.ApplicableTo == DiscountApplicableTo.DOCTOR && d.DoctorId == doctorId));

        return await query.OrderBy(d => d.Name).ToListAsync();
    }

    public async Task<DiscountEntity?> GetValidDiscountAsync(string code, Guid clinicId, Guid? specialtyId = null, Guid? doctorId = null)
    {
        var now = DateTime.UtcNow;
        var query = _context.Discounts
            .Where(d => d.Code == code &&
                       d.ClinicId == clinicId &&
                       d.Status == DiscountStatus.ACTIVE &&
                       d.StartDate <= now &&
                       d.EndDate >= now &&
                       (d.MaxUses == null || d.UsesCount < d.MaxUses));

        // Apply applicability filters
        query = query.Where(d =>
            d.ApplicableTo == DiscountApplicableTo.ALL ||
            (d.ApplicableTo == DiscountApplicableTo.SPECIALTY && d.SpecialtyId == specialtyId) ||
            (d.ApplicableTo == DiscountApplicableTo.DOCTOR && d.DoctorId == doctorId));

        return await query.FirstOrDefaultAsync();
    }

    public async Task<bool> IncrementUsageAsync(Guid discountId)
    {
        var discount = await GetByIdAsync(discountId);
        if (discount == null) return false;

        discount.UsesCount++;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DecrementUsageAsync(Guid discountId)
    {
        var discount = await GetByIdAsync(discountId);
        if (discount == null || discount.UsesCount <= 0) return false;

        discount.UsesCount--;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetRemainingUsesAsync(Guid discountId)
    {
        var discount = await GetByIdAsync(discountId);
        if (discount == null || discount.MaxUses == null) return int.MaxValue;

        return Math.Max(0, discount.MaxUses.Value - discount.UsesCount);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, DiscountStatus status)
    {
        var discount = await GetByIdAsync(id);
        if (discount == null) return false;

        discount.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<DiscountEntity>> GetExpiredDiscountsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Discounts
            .Where(d => d.Status == DiscountStatus.ACTIVE && d.EndDate < now)
            .ToListAsync();
    }

    public async Task<int> UpdateExpiredDiscountsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredDiscounts = await _context.Discounts
            .Where(d => d.Status == DiscountStatus.ACTIVE && d.EndDate < now)
            .ToListAsync();

        foreach (var discount in expiredDiscounts)
        {
            discount.Status = DiscountStatus.EXPIRED;
        }

        return await _context.SaveChangesAsync();
    }
}
