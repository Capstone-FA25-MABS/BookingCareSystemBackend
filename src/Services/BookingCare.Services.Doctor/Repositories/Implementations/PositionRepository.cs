using BookingCare.Services.Doctor.Data;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Doctor.Repositories.Implementations;

public class PositionRepository : IPositionRepository
{
    private readonly DoctorDbContext _context;

    public PositionRepository(DoctorDbContext context)
    {
        _context = context;
    }

    #region Position CRUD Operations

    public async Task<PositionEntity?> GetPositionByIdAsync(Guid id)
    {
        return await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PositionEntity?> GetPositionByNameAsync(string name)
    {
        return await _context.Positions
            .FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<PositionEntity> CreatePositionAsync(PositionEntity position)
    {
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();
        return position;
    }

    public async Task<PositionEntity> UpdatePositionAsync(PositionEntity position)
    {
        _context.Positions.Update(position);
        await _context.SaveChangesAsync();
        return position;
    }

    public async Task<bool> DeletePositionAsync(Guid id)
    {
        var position = await GetPositionByIdAsync(id);
        if (position == null) return false;

        _context.Positions.Remove(position);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PositionExistsAsync(Guid id)
    {
        return await _context.Positions
            .AnyAsync(p => p.Id == id);
    }

    public async Task<bool> PositionNameExistsAsync(string name, Guid? excludeId = null)
    {
        var query = _context.Positions.Where(p => p.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    #endregion

    #region Position Query Operations

    public async Task<(List<PositionEntity> Positions, int TotalCount)> GetPositionsAsync(PositionQueryRequest query)
    {
        var queryable = _context.Positions.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            queryable = queryable.Where(p => p.Name.ToLower().Contains(searchTerm));
        }

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var positions = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (positions, totalCount);
    }

    public async Task<List<PositionEntity>> GetAllPositionsAsync()
    {
        return await _context.Positions.ToListAsync();
    }

    #endregion
}
