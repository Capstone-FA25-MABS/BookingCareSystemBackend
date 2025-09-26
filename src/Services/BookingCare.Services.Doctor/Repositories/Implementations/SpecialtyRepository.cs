using BookingCare.Services.Doctor.Data;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Shared.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Doctor.Repositories.Implementations;

public class SpecialtyRepository : ISpecialtyRepository
{
    private readonly DoctorDbContext _context;

    public SpecialtyRepository(DoctorDbContext context)
    {
        _context = context;
    }

    #region Specialty CRUD Operations

    public async Task<SpecialtyEntity?> GetSpecialtyByIdAsync(Guid id)
    {
        return await _context.Specialties
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SpecialtyEntity?> GetSpecialtyByNameAsync(string name)
    {
        return await _context.Specialties
            .FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<SpecialtyEntity> CreateSpecialtyAsync(SpecialtyEntity specialty)
    {
        _context.Specialties.Add(specialty);
        await _context.SaveChangesAsync();
        return specialty;
    }

    public async Task<SpecialtyEntity> UpdateSpecialtyAsync(SpecialtyEntity specialty)
    {
        _context.Specialties.Update(specialty);
        await _context.SaveChangesAsync();
        return specialty;
    }

    public async Task<bool> DeleteSpecialtyAsync(Guid id)
    {
        var specialty = await GetSpecialtyByIdAsync(id);
        if (specialty == null) return false;

        // Soft delete - chỉ thay đổi status thành INACTIVE
        specialty.Status = Status.INACTIVE;
        specialty.UpdatedAt = DateTime.UtcNow;
        _context.Specialties.Update(specialty);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SpecialtyExistsAsync(Guid id)
    {
        return await _context.Specialties
            .AnyAsync(s => s.Id == id);
    }

    public async Task<bool> SpecialtyNameExistsAsync(string name, Guid? excludeId = null)
    {
        var query = _context.Specialties.Where(s => s.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    #endregion

    #region Specialty Query Operations

    public async Task<(List<SpecialtyEntity> Specialties, int TotalCount)> GetSpecialtiesAsync(SpecialtyQueryRequest query)
    {
        var queryable = _context.Specialties.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            queryable = queryable.Where(s => s.Name.ToLower().Contains(searchTerm));
        }

        // Apply status filter
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(s => s.Status == query.Status.Value);
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(query.SortBy))
        {
            var isDescending = query.SortOrder?.ToLower() == "desc";
            queryable = query.SortBy.ToLower() switch
            {
                "name" => isDescending ? queryable.OrderByDescending(s => s.Name) : queryable.OrderBy(s => s.Name),
                "status" => isDescending ? queryable.OrderByDescending(s => s.Status) : queryable.OrderBy(s => s.Status),
                "createdat" => isDescending ? queryable.OrderByDescending(s => s.CreatedAt) : queryable.OrderBy(s => s.CreatedAt),
                "updatedat" => isDescending ? queryable.OrderByDescending(s => s.UpdatedAt) : queryable.OrderBy(s => s.UpdatedAt),
                _ => queryable.OrderBy(s => s.Name)
            };
        }
        else
        {
            queryable = queryable.OrderBy(s => s.Name);
        }

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var specialties = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (specialties, totalCount);
    }

    public async Task<List<SpecialtyEntity>> GetAllSpecialtiesAsync()
    {
        return await _context.Specialties
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<SpecialtyEntity>> GetActiveSpecialtiesAsync()
    {
        return await _context.Specialties
            .Where(s => s.Status == Status.ACTIVE)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<SpecialtyEntity>> GetSpecialtiesByIdsAsync(List<Guid> ids)
    {
        return await _context.Specialties
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();
    }

    #endregion
}
