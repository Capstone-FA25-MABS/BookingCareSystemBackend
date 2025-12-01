using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Enums;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
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
        return await _context
            .Hospitals.Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalServiceTypes)
            .Include(h => h.HospitalServiceMedicals)
            .Include(h => h.HospitalImages)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<HospitalEntity?> GetByEmailAsync(string email)
    {
        return await _context
            .Hospitals.Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalImages)
            .FirstOrDefaultAsync(h => h.Email == email);
    }

    public async Task<List<HospitalEntity>> GetAllAsync()
    {
        return await _context
            .Hospitals.Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalImages)
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<(List<HospitalEntity> hospitals, int totalCount)> GetFilteredAsync(
        HospitalFilterRequest filter
    )
    {
        var query = _context
            .Hospitals.Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalImages)
            .AsQueryable();

        query = ApplyFilters(query, filter);
        var totalCount = await query.CountAsync();
        query = ApplySorting(query, filter);
        var hospitals = await ApplyPagination(query, filter).ToListAsync();

        return (hospitals, totalCount);
    }

    private IQueryable<HospitalEntity> ApplyFilters(
        IQueryable<HospitalEntity> query,
        HospitalFilterRequest filter
    )
    {
        if (!string.IsNullOrEmpty(filter.Name))
            query = query.Where(h => h.Name.Contains(filter.Name));

        if (!string.IsNullOrEmpty(filter.Email))
            query = query.Where(h => h.Email.Contains(filter.Email));

        // Note: Status filtering is now handled by Auth service, not in database query

        if (filter.SpecialtyIds != null && filter.SpecialtyIds.Any())
            query = query.Where(h =>
                h.HospitalSpecialties.Any(hs => filter.SpecialtyIds.Contains(hs.SpecialtyId))
            );

        return query;
    }

    private IQueryable<HospitalEntity> ApplySorting(
        IQueryable<HospitalEntity> query,
        HospitalFilterRequest filter
    )
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
            _ => query.OrderBy(h => h.Name),
        };
    }

    private IQueryable<HospitalEntity> ApplyPagination(
        IQueryable<HospitalEntity> query,
        HospitalFilterRequest filter
    )
    {
        return query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);
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
        return await _context
            .Hospitals.Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalImages)
            .Where(h => h.HospitalSpecialties.Any(hs => hs.SpecialtyId == specialtyId))
            .ToListAsync();
    }

    public async Task<List<HospitalEntity>> GetByAccountIdAsync(Guid accountId)
    {
        return await _context
            .Hospitals.Include(h => h.HospitalSpecialties)
            .Include(h => h.HospitalServiceTypes)
            .Include(h => h.HospitalServiceMedicals)
            .Include(h => h.HospitalImages)
            .Where(h => h.AccountId == accountId)
            .ToListAsync();
    }

    public async Task<List<HospitalEntity>> GetByAccountIdsAsync(IEnumerable<Guid> accountIds)
    {
        if (accountIds == null || !accountIds.Any())
            return new List<HospitalEntity>();

        return await _context
            .Hospitals.Where(h => accountIds.Contains(h.AccountId))
            .Select(h => new HospitalEntity
            {
                Id = h.Id,
                AccountId = h.AccountId,
                Name = h.Name,
                Email = h.Email,
                Phone = h.Phone,
                Address = h.Address,
                AvatarUrl = h.AvatarUrl,
            })
            .ToListAsync();
    }

    #region Optimized Methods for gRPC Performance

    public async Task<HospitalEntity?> GetHospitalBasicInfoByIdAsync(Guid id)
    {
        return await _context
            .Hospitals.Where(h => h.Id == id)
            .Select(h => new HospitalEntity
            {
                Id = h.Id,
                Name = h.Name,
                Address = h.Address,
                Phone = h.Phone,
                Email = h.Email,
                AvatarUrl = h.AvatarUrl,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<HospitalEntity>> GetHospitalsBasicInfoByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        return await _context
            .Hospitals.Where(h => idList.Contains(h.Id))
            .Select(h => new HospitalEntity
            {
                Id = h.Id,
                Name = h.Name,
                Address = h.Address,
                Phone = h.Phone,
                Email = h.Email,
                AvatarUrl = h.AvatarUrl,
            })
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, string>> GetHospitalNamesByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        return await _context
            .Hospitals.Where(h => idList.Contains(h.Id))
            .Select(h => new { h.Id, h.Name })
            .ToDictionaryAsync(h => h.Id, h => h.Name);
    }

    public async Task<List<HospitalEntity>> GetActiveHospitalsSimpleAsync()
    {
        // Note: Status filtering is now handled by Auth service
        // This method returns all hospitals, status will be enriched later
        return await _context
            .Hospitals.Select(h => new HospitalEntity
            {
                Id = h.Id,
                Name = h.Name,
                AvatarUrl = h.AvatarUrl,
            })
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<(
        List<HospitalEntity> hospitals,
        int totalCount
    )> GetOptimizedHospitalListAsync(HospitalListOptimizedFilterRequest filter)
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

    private static IQueryable<HospitalEntity> ApplyOptimizedSearchFilter(
        IQueryable<HospitalEntity> query,
        string? search
    )
    {
        if (string.IsNullOrEmpty(search))
            return query;

        Console.WriteLine($"Applying search filter: '{search}'");
        return query.Where(h => h.Name.Contains(search) || h.Address.Contains(search));
    }

    private async Task<IQueryable<HospitalEntity>> ApplyOptimizedSpecialtyFilterAsync(
        IQueryable<HospitalEntity> query,
        string[]? specialtyIds
    )
    {
        if (specialtyIds == null || specialtyIds.Length == 0)
            return query;

        Console.WriteLine(
            $"Applying specialty filter with {specialtyIds.Length} specialty IDs: {string.Join(", ", specialtyIds)}"
        );

        var specialtyGuids = ParseSpecialtyGuids(specialtyIds);
        if (!specialtyGuids.Any())
            return query;

        await LogSpecialtyDebugInfoAsync(specialtyGuids);
        return query.Where(h =>
            h.HospitalSpecialties.Any(hs => specialtyGuids.Contains(hs.SpecialtyId))
        );
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
        var hospitalsWithSpecialties = await _context
            .Hospitals.Where(h => h.HospitalSpecialties.Any())
            .CountAsync();
        Console.WriteLine($"Total hospitals with specialties: {hospitalsWithSpecialties}");

        var hospitalsWithSpecificSpecialty = await _context
            .Hospitals.Where(h =>
                h.HospitalSpecialties.Any(hs => specialtyGuids.Contains(hs.SpecialtyId))
            )
            .CountAsync();
        Console.WriteLine($"Hospitals with specific specialty: {hospitalsWithSpecificSpecialty}");

        var hospitalSpecialtyCount = await _context
            .HospitalSpecialties.Where(hs => specialtyGuids.Contains(hs.SpecialtyId))
            .CountAsync();
        Console.WriteLine($"Hospital-specialty relationships: {hospitalSpecialtyCount}");

        var totalHospitalSpecialtyRelations = await _context.HospitalSpecialties.CountAsync();
        Console.WriteLine(
            $"Total hospital-specialty relationships: {totalHospitalSpecialtyRelations}"
        );

        var totalHospitals = await _context.Hospitals.CountAsync();
        Console.WriteLine($"Total hospitals in database: {totalHospitals}");
    }

    private async Task<List<HospitalEntity>> ProjectSortAndPaginateAsync(
        IQueryable<HospitalEntity> query,
        HospitalListOptimizedFilterRequest filter
    )
    {
        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(h => h.Name)
                : query.OrderBy(h => h.Name),
            "address" => filter.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(h => h.Address)
                : query.OrderBy(h => h.Address),
            _ => query.OrderBy(h => h.Name),
        };

        return await query
            .Include(h => h.HospitalSpecialties)
            .Select(h => new HospitalEntity
            {
                Id = h.Id,
                Name = h.Name,
                Address = h.Address,
                AvatarUrl = h.AvatarUrl,
                HospitalSpecialties = h
                    .HospitalSpecialties.Select(hs => new HospitalSpecialtyEntity
                    {
                        HospitalId = hs.HospitalId,
                        SpecialtyId = hs.SpecialtyId,
                    })
                    .ToList(),
            })
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Get specialty information directly from database for performance optimization
    /// </summary>
    public Task<Dictionary<Guid, (string Name, string? ImageUrl)>> GetSpecialtyInfoByIdsAsync(
        List<Guid> specialtyIds
    )
    {
        if (specialtyIds == null || !specialtyIds.Any())
        {
            return Task.FromResult(new Dictionary<Guid, (string Name, string? ImageUrl)>());
        }

        // This would require a direct connection to Doctor database
        // For now, we'll return empty dictionary and rely on gRPC
        // In a real implementation, you might want to:
        // 1. Use a shared database
        // 2. Use database federation
        // 3. Use a data warehouse
        // 4. Use event sourcing to sync specialty data

        return Task.FromResult(new Dictionary<Guid, (string Name, string? ImageUrl)>());
    }

    #endregion

    #region Specialty Management

    public async Task<bool> AddSpecialtyAsync(Guid hospitalId, Guid specialtyId)
    {
        var exists = await _context.HospitalSpecialties.AnyAsync(hs =>
            hs.HospitalId == hospitalId && hs.SpecialtyId == specialtyId
        );

        if (exists)
        {
            return true; // Already exists
        }

        _context.HospitalSpecialties.Add(
            new HospitalSpecialtyEntity { HospitalId = hospitalId, SpecialtyId = specialtyId }
        );

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveSpecialtyAsync(Guid hospitalId, Guid specialtyId)
    {
        var hospitalSpecialty = await _context.HospitalSpecialties.FirstOrDefaultAsync(hs =>
            hs.HospitalId == hospitalId && hs.SpecialtyId == specialtyId
        );

        if (hospitalSpecialty == null)
        {
            return false;
        }

        _context.HospitalSpecialties.Remove(hospitalSpecialty);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task UpdateHospitalSpecialtiesBatchAsync(Guid hospitalId, List<Guid> specialtyIds)
    {
        var distinctSpecialtyIds = specialtyIds?.Distinct().ToHashSet() ?? new HashSet<Guid>();

        // Get existing specialty IDs only (more efficient than loading full entities)
        var existingSpecialtyIdsList = await _context
            .HospitalSpecialties.Where(hs => hs.HospitalId == hospitalId)
            .Select(hs => hs.SpecialtyId)
            .ToListAsync();
        var existingSpecialtyIds = existingSpecialtyIdsList.ToHashSet();

        // Calculate changes to minimize database operations
        var specialtiesToRemove = existingSpecialtyIds.Except(distinctSpecialtyIds).ToList();
        var specialtiesToAdd = distinctSpecialtyIds.Except(existingSpecialtyIds).ToList();

        // Only perform operations if there are actual changes
        if (!specialtiesToRemove.Any() && !specialtiesToAdd.Any())
        {
            return; // No changes needed
        }

        // Remove specialties that are no longer needed
        if (specialtiesToRemove.Any())
        {
            await _context
                .HospitalSpecialties.Where(hs =>
                    hs.HospitalId == hospitalId && specialtiesToRemove.Contains(hs.SpecialtyId)
                )
                .ExecuteDeleteAsync(); // More efficient bulk delete
        }

        // Add new specialties
        if (specialtiesToAdd.Any())
        {
            var newSpecialties = specialtiesToAdd
                .Select(specialtyId => new HospitalSpecialtyEntity
                {
                    HospitalId = hospitalId,
                    SpecialtyId = specialtyId,
                })
                .ToList();

            await _context.HospitalSpecialties.AddRangeAsync(newSpecialties);
        }

        // Single SaveChanges call for all operations
        await _context.SaveChangesAsync();
    }

    public async Task<List<Guid>> GetHospitalSpecialtyIdsAsync(Guid hospitalId)
    {
        // Optimized: Only query IDs, don't load full hospital entity
        return await _context
            .HospitalSpecialties.Where(hs => hs.HospitalId == hospitalId)
            .Select(hs => hs.SpecialtyId)
            .ToListAsync();
    }

    #endregion

    #region ServiceType Management

    public async Task<bool> AddServiceTypeAsync(Guid hospitalId, Guid serviceTypeId)
    {
        var exists = await _context.HospitalServiceTypes.AnyAsync(hst =>
            hst.HospitalId == hospitalId && hst.ServiceTypeId == serviceTypeId
        );

        if (exists)
        {
            return true; // Already exists
        }

        _context.HospitalServiceTypes.Add(
            new HospitalServiceTypeEntity
            {
                HospitalId = hospitalId,
                ServiceTypeId = serviceTypeId,
                CreatedAt = DateTime.Now,
            }
        );

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveServiceTypeAsync(Guid hospitalId, Guid serviceTypeId)
    {
        var hospitalServiceType = await _context.HospitalServiceTypes.FirstOrDefaultAsync(hst =>
            hst.HospitalId == hospitalId && hst.ServiceTypeId == serviceTypeId
        );

        if (hospitalServiceType == null)
        {
            return false;
        }

        _context.HospitalServiceTypes.Remove(hospitalServiceType);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task UpdateHospitalServiceTypesBatchAsync(
        Guid hospitalId,
        List<Guid> serviceTypeIds
    )
    {
        var distinctServiceTypeIds = serviceTypeIds?.Distinct().ToHashSet() ?? new HashSet<Guid>();

        // Get existing service type IDs only (more efficient than loading full entities)
        var existingServiceTypeIdsList = await _context
            .HospitalServiceTypes.Where(hst => hst.HospitalId == hospitalId)
            .Select(hst => hst.ServiceTypeId)
            .ToListAsync();
        var existingServiceTypeIds = existingServiceTypeIdsList.ToHashSet();

        // Calculate changes to minimize database operations
        var serviceTypesToRemove = existingServiceTypeIds.Except(distinctServiceTypeIds).ToList();
        var serviceTypesToAdd = distinctServiceTypeIds.Except(existingServiceTypeIds).ToList();

        // Only perform operations if there are actual changes
        if (!serviceTypesToRemove.Any() && !serviceTypesToAdd.Any())
        {
            return; // No changes needed
        }

        // Remove service types that are no longer needed
        if (serviceTypesToRemove.Any())
        {
            await _context
                .HospitalServiceTypes.Where(hst =>
                    hst.HospitalId == hospitalId && serviceTypesToRemove.Contains(hst.ServiceTypeId)
                )
                .ExecuteDeleteAsync(); // More efficient bulk delete
        }

        // Add new service types
        if (serviceTypesToAdd.Any())
        {
            var newServiceTypes = serviceTypesToAdd
                .Select(serviceTypeId => new HospitalServiceTypeEntity
                {
                    HospitalId = hospitalId,
                    ServiceTypeId = serviceTypeId,
                    CreatedAt = DateTime.UtcNow,
                })
                .ToList();

            await _context.HospitalServiceTypes.AddRangeAsync(newServiceTypes);
        }

        // Single SaveChanges call for all operations
        await _context.SaveChangesAsync();
    }

    public async Task<List<Guid>> GetHospitalServiceTypeIdsAsync(Guid hospitalId)
    {
        // Optimized: Only query IDs, don't load full hospital entity
        return await _context
            .HospitalServiceTypes.Where(hst => hst.HospitalId == hospitalId)
            .Select(hst => hst.ServiceTypeId)
            .ToListAsync();
    }

    #endregion

    #region ServiceMedical Management

    public async Task<bool> AddServiceMedicalAsync(Guid hospitalId, Guid serviceMedicalId)
    {
        var exists = await _context.HospitalServiceMedicals.AnyAsync(hsm =>
            hsm.HospitalId == hospitalId && hsm.ServiceMedicalId == serviceMedicalId
        );

        if (exists)
        {
            return true; // Already exists
        }

        _context.HospitalServiceMedicals.Add(
            new HospitalServiceMedicalEntity
            {
                HospitalId = hospitalId,
                ServiceMedicalId = serviceMedicalId,
                CreatedAt = DateTime.Now,
            }
        );

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveServiceMedicalAsync(Guid hospitalId, Guid serviceMedicalId)
    {
        var hospitalServiceMedical = await _context.HospitalServiceMedicals.FirstOrDefaultAsync(
            hsm => hsm.HospitalId == hospitalId && hsm.ServiceMedicalId == serviceMedicalId
        );

        if (hospitalServiceMedical == null)
        {
            return false;
        }

        _context.HospitalServiceMedicals.Remove(hospitalServiceMedical);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion
}
