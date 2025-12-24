using BookingCare.Services.Doctor.Data;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Shared.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Doctor.Repositories.Implementations;

public class LanguageRepository : ILanguageRepository
{
    private readonly DoctorDbContext _context;

    public LanguageRepository(DoctorDbContext context)
    {
        _context = context;
    }

    #region Language CRUD Operations

    public async Task<LanguageEntity?> GetLanguageByIdAsync(Guid id)
    {
        return await _context.Languages
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<LanguageEntity?> GetLanguageByNameAsync(string name)
    {
        return await _context.Languages
            .FirstOrDefaultAsync(l => l.Name == name);
    }

    public async Task<LanguageEntity> CreateLanguageAsync(LanguageEntity language)
    {
        _context.Languages.Add(language);
        await _context.SaveChangesAsync();
        return language;
    }

    public async Task<LanguageEntity> UpdateLanguageAsync(LanguageEntity language)
    {
        _context.Languages.Update(language);
        await _context.SaveChangesAsync();
        return language;
    }

    public async Task<bool> DeleteLanguageAsync(Guid id)
    {
        var language = await GetLanguageByIdAsync(id);
        if (language == null) return false;

        // Soft delete - chỉ thay đổi status thành INACTIVE
        language.Status = Status.INACTIVE;
        language.UpdatedAt = DateTime.UtcNow;
        _context.Languages.Update(language);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LanguageExistsAsync(Guid id)
    {
        return await _context.Languages
            .AnyAsync(l => l.Id == id);
    }

    public async Task<bool> LanguageNameExistsAsync(string name, Guid? excludeId = null)
    {
        var query = _context.Languages.Where(l => l.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(l => l.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    #endregion

    #region Language Query Operations

    public async Task<(List<LanguageEntity> Languages, int TotalCount)> GetLanguagesAsync(LanguageQueryRequest query)
    {
        var queryable = _context.Languages.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            queryable = queryable.Where(l => l.Name.ToLower().Contains(searchTerm));
        }

        // Apply status filter
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(l => l.Status == query.Status.Value);
        }

        // Apply sorting
        queryable = ApplySorting(queryable, query);

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var languages = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (languages, totalCount);
    }

    public async Task<List<LanguageEntity>> GetAllLanguagesAsync()
    {
        return await _context.Languages.ToListAsync();
    }

    public async Task<List<LanguageEntity>> GetLanguagesByIdsAsync(List<Guid> ids)
    {
        return await _context.Languages
            .Where(l => ids.Contains(l.Id))
            .ToListAsync();
    }

    public async Task<List<LanguageEntity>> GetActiveLanguagesSimpleAsync()
    {
        return await _context.Languages
            .Where(l => l.Status == Status.ACTIVE)
            .Select(l => new LanguageEntity
            {
                Id = l.Id,
                Name = l.Name,
                Status = l.Status
            })
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    #endregion

    #region Private Helper Methods

    private IQueryable<LanguageEntity> ApplySorting(IQueryable<LanguageEntity> queryable, LanguageQueryRequest query)
    {
        if (string.IsNullOrEmpty(query.SortBy))
        {
            // Default sort by CreatedAt descending (newest first)
            return queryable.OrderByDescending(l => l.CreatedAt);
        }

        return query.SortBy.ToLower() switch
        {
            "name" => query.SortOrder?.ToLower() == "desc"
                ? queryable.OrderByDescending(l => l.Name)
                : queryable.OrderBy(l => l.Name),
            "createdat" => query.SortOrder?.ToLower() == "desc"
                ? queryable.OrderByDescending(l => l.CreatedAt)
                : queryable.OrderBy(l => l.CreatedAt),
            "updatedat" => query.SortOrder?.ToLower() == "desc"
                ? queryable.OrderByDescending(l => l.UpdatedAt)
                : queryable.OrderBy(l => l.UpdatedAt),
            "status" => query.SortOrder?.ToLower() == "desc"
                ? queryable.OrderByDescending(l => l.Status)
                : queryable.OrderBy(l => l.Status),
            _ => queryable.OrderByDescending(l => l.CreatedAt) // Default fallback
        };
    }

    #endregion
}
