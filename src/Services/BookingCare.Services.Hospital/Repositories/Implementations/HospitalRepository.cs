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

        // Note: Status filtering is now handled by Auth service, not in database query

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

    public async Task<List<HospitalEntity>> GetActiveHospitalsSimpleAsync()
    {
        // Note: Status filtering is now handled by Auth service
        // This method returns all hospitals, status will be enriched later
        return await _context.Hospitals
            .Select(h => new HospitalEntity
            {
                Id = h.Id,
                Name = h.Name,
                AvatarUrl = h.AvatarUrl
            })
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<(List<HospitalEntity> hospitals, int totalCount)> GetOptimizedHospitalListAsync(HospitalListOptimizedFilterRequest filter)
    {
        var query = _context.Hospitals.AsQueryable();

        // 1) Search filter
        query = ApplyOptimizedSearchFilter(query, filter.Search);

        // 2) Specialty filter (parse + debug + apply)
        query = await ApplyOptimizedSpecialtyFilterAsync(query, filter.SpecialtyIds);

        // 3) Count before pagination
        var totalCount = await query.CountAsync();
        Console.WriteLine($"Total hospitals after database filtering: {totalCount}");

        // 4) Sorting + 5) Projection + Pagination
        var hospitals = await ProjectSortAndPaginateAsync(query, filter);

        Console.WriteLine($"Returning {hospitals.Count} hospitals from repository");
        return (hospitals, totalCount);
    }

    private static IQueryable<HospitalEntity> ApplyOptimizedSearchFilter(IQueryable<HospitalEntity> query, string? search)
    {
        if (string.IsNullOrEmpty(search))
            return query;

        Console.WriteLine($"Applying search filter: '{search}'");
        return query.Where(h => h.Name.Contains(search) || h.Address.Contains(search));
    }

    private async Task<IQueryable<HospitalEntity>> ApplyOptimizedSpecialtyFilterAsync(IQueryable<HospitalEntity> query, string[]? specialtyIds)
    {
        if (specialtyIds == null || specialtyIds.Length == 0)
            return query;

        Console.WriteLine($"Applying specialty filter with {specialtyIds.Length} specialty IDs: {string.Join(", ", specialtyIds)}");

        var specialtyGuids = ParseSpecialtyGuids(specialtyIds);
        if (!specialtyGuids.Any())
            return query;

        await LogSpecialtyDebugInfoAsync(specialtyGuids);
        return query.Where(h => h.HospitalSpecialties.Any(hs => specialtyGuids.Contains(hs.SpecialtyId)));
    }

    private static List<Guid> ParseSpecialtyGuids(IEnumerable<string> specialtyIds)
    {
        var specialtyGuids = new List<Guid>();
        foreach (var specialtyIdStr in specialtyIds)
        {
            if (Guid.TryParse(specialtyIdStr, out var specialtyGuid))
            {
                specialtyGuids.Add(specialtyGuid);
            }
            else
            {
                Console.WriteLine($"Invalid specialty ID format: {specialtyIdStr}");
            }
        }
        return specialtyGuids;
    }

    private async Task LogSpecialtyDebugInfoAsync(List<Guid> specialtyGuids)
    {
        var hospitalsWithSpecialties = await _context.Hospitals
            .Where(h => h.HospitalSpecialties.Any())
            .CountAsync();
        Console.WriteLine($"Total hospitals with specialties: {hospitalsWithSpecialties}");

        var hospitalsWithSpecificSpecialty = await _context.Hospitals
            .Where(h => h.HospitalSpecialties.Any(hs => specialtyGuids.Contains(hs.SpecialtyId)))
            .CountAsync();
        Console.WriteLine($"Hospitals with specific specialty: {hospitalsWithSpecificSpecialty}");

        var hospitalSpecialtyCount = await _context.HospitalSpecialties
            .Where(hs => specialtyGuids.Contains(hs.SpecialtyId))
            .CountAsync();
        Console.WriteLine($"Hospital-specialty relationships: {hospitalSpecialtyCount}");

        var totalHospitalSpecialtyRelations = await _context.HospitalSpecialties.CountAsync();
        Console.WriteLine($"Total hospital-specialty relationships: {totalHospitalSpecialtyRelations}");

        var totalHospitals = await _context.Hospitals.CountAsync();
        Console.WriteLine($"Total hospitals in database: {totalHospitals}");
    }

    private async Task<List<HospitalEntity>> ProjectSortAndPaginateAsync(IQueryable<HospitalEntity> query, HospitalListOptimizedFilterRequest filter)
    {
        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(h => h.Name) : query.OrderBy(h => h.Name),
            "address" => filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(h => h.Address) : query.OrderBy(h => h.Address),
            _ => query.OrderBy(h => h.Name)
        };

        return await query
            .Include(h => h.HospitalSpecialties)
            .Select(h => new HospitalEntity
            {
                Id = h.Id,
                Name = h.Name,
                Address = h.Address,
                AvatarUrl = h.AvatarUrl,
                HospitalSpecialties = h.HospitalSpecialties.Select(hs => new HospitalSpecialtyEntity
                {
                    HospitalId = hs.HospitalId,
                    SpecialtyId = hs.SpecialtyId
                }).ToList()
            })
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Get specialty information directly from database for performance optimization
    /// </summary>
    public async Task<Dictionary<Guid, (string Name, string? ImageUrl)>> GetSpecialtyInfoByIdsAsync(List<Guid> specialtyIds)
    {
        if (specialtyIds == null || !specialtyIds.Any())
        {
            return new Dictionary<Guid, (string Name, string? ImageUrl)>();
        }

        // This would require a direct connection to Doctor database
        // For now, we'll return empty dictionary and rely on gRPC
        // In a real implementation, you might want to:
        // 1. Use a shared database
        // 2. Use database federation
        // 3. Use a data warehouse
        // 4. Use event sourcing to sync specialty data

        return new Dictionary<Guid, (string Name, string? ImageUrl)>();
    }

    #endregion
}
