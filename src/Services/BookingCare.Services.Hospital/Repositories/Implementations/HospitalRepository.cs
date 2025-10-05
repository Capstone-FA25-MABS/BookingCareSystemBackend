using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Enums;
using BookingCare.Shared.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Hospital.Repositories.Implementations;

public class HospitalRepository : IHospitalRepository
{
    private readonly HospitalDbContext _context;

    public HospitalRepository(HospitalDbContext context)
    {
        _context = context;
    }

    public async Task<HospitalEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Hospitals
            .Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalImages)
            .Include(h => h.HospitalSubscriptions.Where(s => s.Status == SubscriptionStatus.ACTIVE))
                .ThenInclude(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<HospitalEntity?> GetByEmailAsync(string email)
    {
        return await _context.Hospitals
            .Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalImages)
            .FirstOrDefaultAsync(h => h.Email == email);
    }

    public async Task<List<HospitalEntity>> GetAllAsync()
    {
        return await _context.Hospitals
            .Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalImages)
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<(List<HospitalEntity> hospitals, int totalCount)> GetFilteredAsync(HospitalFilterRequest filter)
    {
        var query = _context.Hospitals
            .Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalImages)
            .AsQueryable();

        query = ApplyFilters(query, filter);
        var totalCount = await query.CountAsync();
        query = ApplySorting(query, filter);
        var hospitals = await ApplyPagination(query, filter).ToListAsync();

        return (hospitals, totalCount);
    }

    private IQueryable<HospitalEntity> ApplyFilters(IQueryable<HospitalEntity> query, HospitalFilterRequest filter)
    {
        if (!string.IsNullOrEmpty(filter.Name))
            query = query.Where(h => h.Name.Contains(filter.Name));

        if (!string.IsNullOrEmpty(filter.Email))
            query = query.Where(h => h.Email.Contains(filter.Email));

        if (filter.Status.HasValue)
            query = query.Where(h => h.Status == filter.Status.Value);

        if (filter.SpecialtyIds != null && filter.SpecialtyIds.Any())
            query = query.Where(h => h.HospitalSpecialties.Any(hs => filter.SpecialtyIds.Contains(hs.SpecialtyId)));

        return query;
    }

    private IQueryable<HospitalEntity> ApplySorting(IQueryable<HospitalEntity> query, HospitalFilterRequest filter)
    {
        if (string.IsNullOrEmpty(filter.SortBy))
            return query.OrderBy(h => h.Name);

        return filter.SortBy.ToLower() switch
        {
            "name" => filter.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(h => h.Name)
                : query.OrderBy(h => h.Name),
            "email" => filter.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(h => h.Email)
                : query.OrderBy(h => h.Email),
            "createdat" => filter.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(h => h.CreatedAt)
                : query.OrderBy(h => h.CreatedAt),
            _ => query.OrderBy(h => h.Name)
        };
    }

    private IQueryable<HospitalEntity> ApplyPagination(IQueryable<HospitalEntity> query, HospitalFilterRequest filter)
    {
        return query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize);
    }

    public async Task<HospitalEntity> CreateAsync(HospitalEntity hospital)
    {
        _context.Hospitals.Add(hospital);
        await _context.SaveChangesAsync();
        return hospital;
    }

    public async Task<HospitalEntity> UpdateAsync(HospitalEntity hospital)
    {
        _context.Hospitals.Update(hospital);
        await _context.SaveChangesAsync();
        return hospital;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var hospital = await _context.Hospitals.FindAsync(id);
        if (hospital == null)
            return false;

        _context.Hospitals.Remove(hospital);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Hospitals.AnyAsync(h => h.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null)
    {
        var query = _context.Hospitals.Where(h => h.Email == email);
        if (excludeId.HasValue)
        {
            query = query.Where(h => h.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<List<HospitalEntity>> GetBySpecialtyAsync(Guid specialtyId)
    {
        return await _context.Hospitals
            .Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalImages)
            .Where(h => h.HospitalSpecialties.Any(hs => hs.SpecialtyId == specialtyId))
            .ToListAsync();
    }

    public async Task<List<HospitalEntity>> GetByAccountIdAsync(Guid accountId)
    {
        return await _context.Hospitals
            .Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalImages)
            .Where(h => h.AccountId == accountId)
            .ToListAsync();
    }

    #region Optimized Methods for gRPC Performance

    public async Task<HospitalEntity?> GetHospitalBasicInfoByIdAsync(Guid id)
    {
        return await _context.Hospitals
            .Where(h => h.Id == id)
            .Select(h => new HospitalEntity
            {
                Id = h.Id,
                Name = h.Name,
                Address = h.Address,
                Phone = h.Phone,
                Email = h.Email,
                AvatarUrl = h.AvatarUrl
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<HospitalEntity>> GetHospitalsBasicInfoByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        return await _context.Hospitals
            .Where(h => idList.Contains(h.Id))
            .Select(h => new HospitalEntity
            {
                Id = h.Id,
                Name = h.Name,
                Address = h.Address,
                Phone = h.Phone,
                Email = h.Email,
                AvatarUrl = h.AvatarUrl
            })
            .ToListAsync();
    }

    #endregion
}
