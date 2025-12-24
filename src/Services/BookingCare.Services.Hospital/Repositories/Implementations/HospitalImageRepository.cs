using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Hospital.Repositories.Implementations;

public class HospitalImageRepository : IHospitalImageRepository
{
    private readonly HospitalDbContext _context;

    public HospitalImageRepository(HospitalDbContext context)
    {
        _context = context;
    }

    public async Task<HospitalImageEntity?> GetByIdAsync(Guid id)
    {
        return await _context.HospitalImages
            .FirstOrDefaultAsync(img => img.Id == id);
    }

    public async Task<List<HospitalImageEntity>> GetAllAsync()
    {
        return await _context.HospitalImages
            .OrderBy(img => img.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<HospitalImageEntity> images, int totalCount)> GetFilteredAsync(HospitalImageFilterRequest filter)
    {
        var query = _context.HospitalImages.AsQueryable();

        if (filter.HospitalId.HasValue)
        {
            query = query.Where(img => img.HospitalId == filter.HospitalId.Value);
        }

        var totalCount = await query.CountAsync();

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(filter.SortBy))
        {
            query = filter.SortBy.ToLower() switch
            {
                "createdat" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(img => img.CreatedAt)
                    : query.OrderBy(img => img.CreatedAt),
                _ => query.OrderBy(img => img.CreatedAt)
            };
        }
        else
        {
            query = query.OrderBy(img => img.CreatedAt);
        }

        // Apply pagination
        var images = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (images, totalCount);
    }

    public async Task<HospitalImageEntity> CreateAsync(HospitalImageEntity image)
    {
        _context.HospitalImages.Add(image);
        await _context.SaveChangesAsync();
        return image;
    }

    public async Task<HospitalImageEntity> UpdateAsync(HospitalImageEntity image)
    {
        _context.HospitalImages.Update(image);
        await _context.SaveChangesAsync();
        return image;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var image = await GetByIdAsync(id);
        if (image == null)
        {
            return false;
        }

        _context.HospitalImages.Remove(image);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.HospitalImages.AnyAsync(img => img.Id == id);
    }

    public async Task<List<HospitalImageEntity>> GetByHospitalIdAsync(Guid hospitalId)
    {
        return await _context.HospitalImages
            .Where(img => img.HospitalId == hospitalId)
            .OrderBy(img => img.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteByHospitalIdAsync(Guid hospitalId)
    {
        var images = await GetByHospitalIdAsync(hospitalId);
        if (images.Count == 0)
        {
            return false;
        }

        _context.HospitalImages.RemoveRange(images);
        await _context.SaveChangesAsync();
        return true;
    }
}

