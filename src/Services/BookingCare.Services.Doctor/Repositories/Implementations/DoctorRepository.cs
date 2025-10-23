using BookingCare.Services.Doctor.Data;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Shared.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Doctor.Repositories.Implementations;

public class DoctorRepository : IDoctorRepository
{
    private readonly DoctorDbContext _context;

    public DoctorRepository(DoctorDbContext context)
    {
        _context = context;
    }

    #region Doctor CRUD Operations

    public async Task<DoctorEntity?> GetDoctorByIdAsync(Guid id)
    {
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DoctorEntity?> GetDoctorByEmailAsync(string email)
    {
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .FirstOrDefaultAsync(d => d.Email == email);
    }

    public async Task<DoctorEntity?> GetDoctorByAccountIdAsync(Guid accountId)
    {
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .FirstOrDefaultAsync(d => d.AccountId == accountId);
    }

    public async Task<DoctorEntity> CreateDoctorAsync(DoctorEntity doctor)
    {
        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();

        // Load the created doctor with related entities
        return await GetDoctorByIdAsync(doctor.Id) ?? doctor;
    }

    public async Task<DoctorEntity> UpdateDoctorAsync(DoctorEntity doctor)
    {
        _context.Doctors.Update(doctor);
        await _context.SaveChangesAsync();

        // Load the updated doctor with related entities
        return await GetDoctorByIdAsync(doctor.Id) ?? doctor;
    }

    public async Task<bool> DeleteDoctorAsync(Guid id)
    {
        var doctor = await GetDoctorByIdAsync(id);
        if (doctor == null) return false;

        _context.Doctors.Remove(doctor);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DoctorExistsAsync(Guid id)
    {
        return await _context.Doctors
            .AnyAsync(d => d.Id == id);
    }

    public async Task<bool> DoctorEmailExistsAsync(string email, Guid? excludeId = null)
    {
        var query = _context.Doctors.Where(d => d.Email == email);

        if (excludeId.HasValue)
        {
            query = query.Where(d => d.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> DoctorAccountExistsAsync(Guid accountId, Guid? excludeId = null)
    {
        var query = _context.Doctors.Where(d => d.AccountId == accountId);

        if (excludeId.HasValue)
        {
            query = query.Where(d => d.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    #endregion

    #region Doctor Query Operations

    public async Task<(List<DoctorEntity> Doctors, int TotalCount)> GetDoctorsAsync(DoctorQueryRequest query)
    {
        var queryable = GetBaseQueryable();

        queryable = ApplyFilters(queryable, query);
        queryable = ApplySorting(queryable, query);

        var totalCount = await queryable.CountAsync();
        var doctors = await ApplyPagination(queryable, query).ToListAsync();

        return (doctors, totalCount);
    }

    private IQueryable<DoctorEntity> GetBaseQueryable()
    {
        return _context.Doctors
            .AsNoTracking() // Optimize for read-only operations
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .AsQueryable();
    }

    private IQueryable<DoctorEntity> ApplyFilters(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        queryable = ApplyBasicFilters(queryable, query);
        queryable = ApplySearchFilters(queryable, query);
        queryable = ApplyPriceFilters(queryable, query);

        return queryable;
    }

    private IQueryable<DoctorEntity> ApplyBasicFilters(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        queryable = ApplyAccountFilter(queryable, query);
        queryable = ApplyPositionFilters(queryable, query);
        queryable = ApplySpecialtyFilters(queryable, query);
        queryable = ApplyHospitalFilters(queryable, query);
        queryable = ApplyLocationFilters(queryable, query);
        queryable = ApplyGenderFilters(queryable, query);
        queryable = ApplyExperienceFilters(queryable, query);
        queryable = ApplyAddressFilter(queryable, query);

        return queryable;
    }

    private IQueryable<DoctorEntity> ApplyAccountFilter(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        if (query.AccountId.HasValue)
            queryable = queryable.Where(d => d.AccountId == query.AccountId.Value);
        return queryable;
    }

    private IQueryable<DoctorEntity> ApplyPositionFilters(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        if (query.PositionId.HasValue)
            queryable = queryable.Where(d => d.PositionId == query.PositionId.Value);
        if (query.PositionIds != null && query.PositionIds.Any())
            queryable = queryable.Where(d => d.PositionId.HasValue && query.PositionIds.Contains(d.PositionId.Value));
        return queryable;
    }

    private IQueryable<DoctorEntity> ApplySpecialtyFilters(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        if (query.SpecialtyId.HasValue)
            queryable = queryable.Where(d => d.SpecialtyId == query.SpecialtyId.Value);
        if (query.SpecialtyIds != null && query.SpecialtyIds.Any())
            queryable = queryable.Where(d => d.SpecialtyId.HasValue && query.SpecialtyIds.Contains(d.SpecialtyId.Value));
        return queryable;
    }

    private IQueryable<DoctorEntity> ApplyHospitalFilters(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        if (query.HospitalId.HasValue)
            queryable = queryable.Where(d => d.HospitalId == query.HospitalId.Value);
        if (query.HospitalIds != null && query.HospitalIds.Any())
            queryable = queryable.Where(d => d.HospitalId.HasValue && query.HospitalIds.Contains(d.HospitalId.Value));
        return queryable;
    }

    private IQueryable<DoctorEntity> ApplyLocationFilters(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        if (!string.IsNullOrEmpty(query.ProvinceId) || !string.IsNullOrEmpty(query.DistrictId))
        {
            Console.WriteLine($"Location filtering requested - ProvinceId: {query.ProvinceId}, DistrictId: {query.DistrictId}");
            Console.WriteLine("Location filtering will be handled at service layer with distance calculation");
        }
        return queryable;
    }

    private IQueryable<DoctorEntity> ApplyGenderFilters(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        if (!string.IsNullOrEmpty(query.Gender))
        {
            Console.WriteLine($"Filtering by single gender: {query.Gender}");
            var genderEnum = Enum.Parse<Gender>(query.Gender.ToUpper());
            queryable = queryable.Where(d => d.Gender == genderEnum);
        }
        if (query.Genders != null && query.Genders.Any())
        {
            Console.WriteLine($"Filtering by multiple genders: {string.Join(", ", query.Genders)}");
            var genderEnums = query.Genders
                .Select(g => Enum.Parse<Gender>(g.ToUpper()))
                .ToList();
            queryable = queryable.Where(d => d.Gender.HasValue && genderEnums.Contains(d.Gender.Value));
        }
        return queryable;
    }

    private IQueryable<DoctorEntity> ApplyExperienceFilters(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        if (query.MinYearsOfExperience.HasValue)
        {
            Console.WriteLine($"Applying min experience filter: {query.MinYearsOfExperience.Value} years");
            queryable = queryable.Where(d => d.YearsOfExperience >= query.MinYearsOfExperience.Value);
        }
        if (query.MaxYearsOfExperience.HasValue)
        {
            Console.WriteLine($"Applying max experience filter: {query.MaxYearsOfExperience.Value} years");
            queryable = queryable.Where(d => d.YearsOfExperience <= query.MaxYearsOfExperience.Value);
        }

        // Only apply experience ranges if min/max are not specified (for backward compatibility)
        if (!query.MinYearsOfExperience.HasValue && !query.MaxYearsOfExperience.HasValue &&
            query.ExperienceRanges != null && query.ExperienceRanges.Any())
        {
            Console.WriteLine($"Applying experience ranges filter: {query.ExperienceRanges.Count} ranges");
            var ranges = query.ExperienceRanges.ToList();
            queryable = queryable.Where(d => ranges.Any(r =>
                d.YearsOfExperience >= r.MinYears && d.YearsOfExperience <= r.MaxYears));
        }
        return queryable;
    }

    private IQueryable<DoctorEntity> ApplyAddressFilter(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        if (!string.IsNullOrEmpty(query.Address))
            queryable = queryable.Where(d => d.Address != null && d.Address.Contains(query.Address));
        return queryable;
    }

    private IQueryable<DoctorEntity> ApplySearchFilters(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            queryable = queryable.Where(d =>
                d.FirstName.ToLower().Contains(searchTerm) ||
                d.LastName.ToLower().Contains(searchTerm) ||
                (d.FirstName + " " + d.LastName).ToLower().Contains(searchTerm));
        }

        // Language filters - optimized with joins instead of subqueries
        if (!string.IsNullOrEmpty(query.Language))
        {
            Console.WriteLine($"Filtering by single language: {query.Language}");
            queryable = queryable.Where(d => d.DoctorLanguages.Any(dl => dl.Language.Name == query.Language));
        }
        if (query.Languages != null && query.Languages.Any())
        {
            Console.WriteLine($"Filtering by multiple languages: {string.Join(", ", query.Languages)}");
            queryable = queryable.Where(d => d.DoctorLanguages.Any(dl => query.Languages.Contains(dl.Language.Name)));
        }

        // Service type filters - optimized with joins instead of subqueries
        if (!string.IsNullOrEmpty(query.ServiceType))
        {
            Console.WriteLine($"Filtering by single service type: {query.ServiceType}");
            queryable = queryable.Where(d => d.DoctorPrices.Any(dp => dp.ServiceType.Name == query.ServiceType));
        }
        if (query.ServiceTypes != null && query.ServiceTypes.Any())
        {
            Console.WriteLine($"Filtering by multiple service types: {string.Join(", ", query.ServiceTypes)}");
            queryable = queryable.Where(d => d.DoctorPrices.Any(dp => query.ServiceTypes.Contains(dp.ServiceType.Name)));
        }

        return queryable;
    }

    private IQueryable<DoctorEntity> ApplyPriceFilters(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        if (query.MinPrice.HasValue || query.MaxPrice.HasValue)
        {
            queryable = queryable.Where(d => d.DoctorPrices.Any(dp =>
                (!query.MinPrice.HasValue || dp.Amount >= query.MinPrice.Value) &&
                (!query.MaxPrice.HasValue || dp.Amount <= query.MaxPrice.Value)));
        }

        return queryable;
    }

    private IQueryable<DoctorEntity> ApplySorting(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        if (string.IsNullOrEmpty(query.SortBy)) return queryable;

        return query.SortBy switch
        {
            "YearsOfExperience" => query.SortOrder == "desc"
                ? queryable.OrderByDescending(d => d.YearsOfExperience)
                : queryable.OrderBy(d => d.YearsOfExperience),
            "CreatedAt" => query.SortOrder == "desc"
                ? queryable.OrderByDescending(d => d.CreatedAt)
                : queryable.OrderBy(d => d.CreatedAt),
            _ => queryable
        };
    }

    private IQueryable<DoctorEntity> ApplyPagination(IQueryable<DoctorEntity> queryable, DoctorQueryRequest query)
    {
        return queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize);
    }

    public IQueryable<DoctorEntity> GetQueryableDoctors()
    {
        return _context.Doctors
            .AsNoTracking() // Optimize for read-only operations
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .AsQueryable();
    }

    public async Task<List<DoctorEntity>> GetDoctorsByHospitalAsync(Guid hospitalId)
    {
        return await _context.Doctors
            .AsNoTracking() // Optimize for read-only operations
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .Where(d => d.HospitalId == hospitalId)
            .ToListAsync();
    }

    public async Task<List<DoctorEntity>> GetDoctorsBySpecialtyAsync(Guid specialtyId)
    {
        return await _context.Doctors
            .AsNoTracking() // Optimize for read-only operations
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .Where(d => d.SpecialtyId == specialtyId)
            .ToListAsync();
    }

    public async Task<List<DoctorEntity>> GetDoctorsByPositionAsync(Guid positionId)
    {
        return await _context.Doctors
            .AsNoTracking() // Optimize for read-only operations
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .Where(d => d.PositionId == positionId)
            .ToListAsync();
    }

    public async Task<List<DoctorEntity>> GetActiveDoctorsAsync()
    {
        return await _context.Doctors
            .AsNoTracking() // Optimize for read-only operations
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .ToListAsync();
    }

    #endregion

    #region DoctorPrice CRUD Operations

    public async Task<DoctorPriceEntity?> GetDoctorPriceAsync(Guid doctorId, Guid priceId)
    {
        return await _context.DoctorPrices
            .Include(dp => dp.Doctor)
            .FirstOrDefaultAsync(dp => dp.DoctorId == doctorId && dp.Id == priceId);
    }

    public async Task<DoctorPriceEntity> CreateDoctorPriceAsync(DoctorPriceEntity doctorPrice)
    {
        _context.DoctorPrices.Add(doctorPrice);
        await _context.SaveChangesAsync();
        return doctorPrice;
    }

    public async Task<DoctorPriceEntity> UpdateDoctorPriceAsync(DoctorPriceEntity doctorPrice)
    {
        _context.DoctorPrices.Update(doctorPrice);
        await _context.SaveChangesAsync();
        return doctorPrice;
    }

    public async Task<bool> DeleteDoctorPriceAsync(Guid doctorId, Guid priceId)
    {
        var doctorPrice = await GetDoctorPriceAsync(doctorId, priceId);
        if (doctorPrice == null) return false;

        _context.DoctorPrices.Remove(doctorPrice);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DoctorPriceExistsAsync(Guid doctorId, Guid priceId)
    {
        return await _context.DoctorPrices
            .AnyAsync(dp => dp.DoctorId == doctorId && dp.Id == priceId);
    }

    #endregion

    #region DoctorPrice Query Operations

    public async Task<List<DoctorPriceEntity>> GetDoctorPricesAsync(Guid doctorId)
    {
        return await _context.DoctorPrices
            .Include(dp => dp.Doctor)
            .Include(dp => dp.ServiceType)
            .Where(dp => dp.DoctorId == doctorId)
            .ToListAsync();
    }

    public async Task<bool> DeleteAllDoctorPricesAsync(Guid doctorId)
    {
        var doctorPrices = await _context.DoctorPrices
            .Where(dp => dp.DoctorId == doctorId)
            .ToListAsync();

        if (!doctorPrices.Any()) return false;

        _context.DoctorPrices.RemoveRange(doctorPrices);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    public async Task<List<DoctorEntity>> GetDoctorsByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        if (!idList.Any()) return new List<DoctorEntity>();

        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .Where(d => idList.Contains(d.Id))
            .ToListAsync();
    }

    public async Task<List<DoctorEntity>> GetDoctorsByAccountIdsAsync(IEnumerable<Guid> accountIds)
    {
        var idList = accountIds.ToList();
        if (!idList.Any()) return new List<DoctorEntity>();

        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Where(d => idList.Contains(d.AccountId))
            .ToListAsync();
    }

    #region Language Operations

    public async Task<List<LanguageEntity>> GetLanguagesAsync()
    {
        return await _context.Languages
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<LanguageEntity?> GetLanguageByIdAsync(Guid id)
    {
        return await _context.Languages
            .FirstOrDefaultAsync(l => l.Id == id);
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

        _context.Languages.Remove(language);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region DoctorLanguage Operations

    public async Task<List<DoctorLanguageEntity>> GetDoctorLanguagesAsync(Guid doctorId)
    {
        return await _context.DoctorLanguages
            .Include(dl => dl.Language)
            .Where(dl => dl.DoctorId == doctorId)
            .ToListAsync();
    }

    public async Task<DoctorLanguageEntity> CreateDoctorLanguageAsync(DoctorLanguageEntity doctorLanguage)
    {
        _context.DoctorLanguages.Add(doctorLanguage);
        await _context.SaveChangesAsync();
        return doctorLanguage;
    }

    public async Task<bool> DeleteDoctorLanguageAsync(Guid doctorId, Guid languageId)
    {
        var doctorLanguage = await _context.DoctorLanguages
            .FirstOrDefaultAsync(dl => dl.DoctorId == doctorId && dl.LanguageId == languageId);

        if (doctorLanguage == null) return false;

        _context.DoctorLanguages.Remove(doctorLanguage);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAllDoctorLanguagesAsync(Guid doctorId)
    {
        var doctorLanguages = await _context.DoctorLanguages
            .Where(dl => dl.DoctorId == doctorId)
            .ToListAsync();

        if (!doctorLanguages.Any()) return false;

        _context.DoctorLanguages.RemoveRange(doctorLanguages);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region ServiceType Operations

    public async Task<List<ServiceTypeEntity>> GetServiceTypesAsync()
    {
        return await _context.ServiceTypes
            .OrderBy(st => st.Name)
            .ToListAsync();
    }

    public async Task<ServiceTypeEntity?> GetServiceTypeByIdAsync(Guid id)
    {
        return await _context.ServiceTypes
            .FirstOrDefaultAsync(st => st.Id == id);
    }

    public async Task<ServiceTypeEntity> CreateServiceTypeAsync(ServiceTypeEntity serviceType)
    {
        _context.ServiceTypes.Add(serviceType);
        await _context.SaveChangesAsync();
        return serviceType;
    }

    public async Task<ServiceTypeEntity> UpdateServiceTypeAsync(ServiceTypeEntity serviceType)
    {
        _context.ServiceTypes.Update(serviceType);
        await _context.SaveChangesAsync();
        return serviceType;
    }

    public async Task<bool> DeleteServiceTypeAsync(Guid id)
    {
        var serviceType = await GetServiceTypeByIdAsync(id);
        if (serviceType == null) return false;

        _context.ServiceTypes.Remove(serviceType);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Optimized Methods for gRPC Performance

    public async Task<DoctorEntity?> GetDoctorBasicInfoByIdAsync(Guid id)
    {
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Where(d => d.Id == id)
            .Select(d => new DoctorEntity
            {
                Id = d.Id,
                Email = d.Email,
                FirstName = d.FirstName,
                LastName = d.LastName,
                AvatarUrl = d.AvatarUrl,
                HospitalId = d.HospitalId,
                Position = d.Position != null ? new PositionEntity { Name = d.Position.Name } : null,
                Specialty = d.Specialty != null ? new SpecialtyEntity { Name = d.Specialty.Name } : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<DoctorEntity>> GetDoctorsBasicInfoByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Where(d => idList.Contains(d.Id))
            .Select(d => new DoctorEntity
            {
                Id = d.Id,
                Email = d.Email,
                FirstName = d.FirstName,
                LastName = d.LastName,
                AvatarUrl = d.AvatarUrl,
                HospitalId = d.HospitalId,
                Position = d.Position != null ? new PositionEntity { Name = d.Position.Name } : null,
                Specialty = d.Specialty != null ? new SpecialtyEntity { Name = d.Specialty.Name } : null
            })
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, decimal>> GetDoctorsPricesByServiceTypeAsync(IEnumerable<Guid> doctorIds, string serviceTypeName)
    {
        var idList = doctorIds.ToList();

        var prices = await _context.DoctorPrices
            .Include(dp => dp.ServiceType)
            .Where(dp => idList.Contains(dp.DoctorId) &&
                         dp.ServiceType != null &&
                         dp.ServiceType.Name == serviceTypeName)
            .Select(dp => new { dp.DoctorId, dp.Amount })
            .ToListAsync();

        return prices.ToDictionary(p => p.DoctorId, p => p.Amount);
    }

    /// <summary>
    /// Get doctors by hospital and specialty (for Appointment Service via gRPC)
    /// Returns lightweight doctor entities with Position and Specialty names
    /// Note: Status check is done in Service layer via Auth Service
    /// </summary>
    public async Task<List<DoctorEntity>> GetDoctorsByHospitalAndSpecialtyAsync(Guid hospitalId, Guid specialtyId)
    {
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.Specialty)
            .Where(d => d.HospitalId == hospitalId
                     && d.SpecialtyId == specialtyId)
            .Select(d => new DoctorEntity
            {
                Id = d.Id,
                AccountId = d.AccountId, // Need for status check in Service layer
                Email = d.Email,
                FirstName = d.FirstName,
                LastName = d.LastName,
                AvatarUrl = d.AvatarUrl,
                YearsOfExperience = d.YearsOfExperience,
                Position = d.Position != null ? new PositionEntity { Name = d.Position.Name } : null,
                Specialty = d.Specialty != null ? new SpecialtyEntity { Name = d.Specialty.Name } : null
            })
            .ToListAsync();
    }

    /// <summary>
    /// Get doctor price by ID (for Appointment Service - Option 3 reschedule)
    /// Returns price with service type name for comparison
    /// </summary>
    public async Task<DoctorPriceEntity?> GetDoctorPriceByIdAsync(Guid priceId)
    {
        return await _context.DoctorPrices.FirstOrDefaultAsync(p => p.Id == priceId);
    }

    #endregion

    #region Common Helper Methods

    /// <summary>
    /// Creates optimized projection for DoctorEntity with only necessary fields
    /// Used to eliminate code duplication in patient search methods
    /// </summary>
    private IQueryable<DoctorEntity> GetOptimizedDoctorProjection(IQueryable<DoctorEntity> baseQuery)
    {
        return baseQuery.Select(d => new DoctorEntity
        {
            // Basic doctor info
            Id = d.Id,
            AccountId = d.AccountId,
            FirstName = d.FirstName,
            LastName = d.LastName,
            YearsOfExperience = d.YearsOfExperience,
            AvatarUrl = d.AvatarUrl,

            // IDs for filtering
            PositionId = d.PositionId,
            SpecialtyId = d.SpecialtyId,
            HospitalId = d.HospitalId,
            Gender = d.Gender,
            Address = d.Address,

            // Navigation properties - only basic info
            Position = d.Position != null ? new PositionEntity
            {
                Id = d.Position.Id,
                Name = d.Position.Name
            } : null,

            Specialty = d.Specialty != null ? new SpecialtyEntity
            {
                Id = d.Specialty.Id,
                Name = d.Specialty.Name
            } : null,

            // Prices - only id, serviceTypeName, amount
            DoctorPrices = d.DoctorPrices.Select(dp => new DoctorPriceEntity
            {
                Id = dp.Id,
                DoctorId = dp.DoctorId,
                ServiceTypeId = dp.ServiceTypeId,
                Amount = dp.Amount,
                ServiceType = new ServiceTypeEntity
                {
                    Id = dp.ServiceType.Id,
                    Name = dp.ServiceType.Name
                }
            }).ToList(),

            // Languages - only id, name
            DoctorLanguages = d.DoctorLanguages.Select(dl => new DoctorLanguageEntity
            {
                Id = dl.Id,
                DoctorId = dl.DoctorId,
                LanguageId = dl.LanguageId,
                Language = new LanguageEntity
                {
                    Id = dl.Language.Id,
                    Name = dl.Language.Name
                }
            }).ToList()
        });
    }

    #endregion

    #region Optimized Patient Search

    /// <summary>
    /// Optimized query for patient search - only loads necessary fields using projection
    /// </summary>
    public async Task<(List<DoctorEntity> Doctors, int TotalCount)> GetDoctorsForPatientSearchAsync(DoctorQueryRequest query)
    {
        var queryable = GetOptimizedQueryableForPatientSearch();

        queryable = ApplyFilters(queryable, query);
        queryable = ApplySorting(queryable, query);

        var totalCount = await queryable.CountAsync();
        var doctors = await ApplyPagination(queryable, query).ToListAsync();

        return (doctors, totalCount);
    }

    /// <summary>
    /// Create optimized queryable with only necessary fields for patient search
    /// Uses Select projection to minimize data transfer
    /// </summary>
    private IQueryable<DoctorEntity> GetOptimizedQueryableForPatientSearch()
    {
        return GetOptimizedDoctorProjection(_context.Doctors.AsNoTracking());
    }

    /// <summary>
    /// Optimized method for complex filtering with multiple criteria
    /// Uses separate queries for better performance
    /// </summary>
    public async Task<(List<DoctorEntity> Doctors, int TotalCount)> GetDoctorsForComplexFilterAsync(DoctorQueryRequest query)
    {
        Console.WriteLine($"GetDoctorsForComplexFilterAsync called with {GetFilterCount(query)} filters");

        // For simple filters, use the optimized projection method
        if (!HasComplexFilters(query))
        {
            Console.WriteLine("No complex filters detected, using optimized projection method");
            return await GetDoctorsForPatientSearchAsync(query);
        }

        // For complex filters, use the multi-step approach
        Console.WriteLine("Complex filters detected, using multi-step approach");

        // Step 1: Get base doctor IDs that match basic filters (fast)
        var baseQuery = _context.Doctors.AsNoTracking();

        // Apply basic filters first (these are fast)
        baseQuery = ApplyBasicFilters(baseQuery, query);

        // Get doctor IDs that match basic filters
        var doctorIds = await baseQuery.Select(d => d.Id).ToListAsync();
        Console.WriteLine($"Basic filters returned {doctorIds.Count} doctor IDs");

        if (!doctorIds.Any())
        {
            return (new List<DoctorEntity>(), 0);
        }

        // Step 2: Apply complex filters (language, service type) on the subset
        doctorIds = await ApplyComplexFilters(doctorIds, query);
        Console.WriteLine($"After complex filters: {doctorIds.Count} doctor IDs");

        if (!doctorIds.Any())
        {
            return (new List<DoctorEntity>(), 0);
        }

        // Step 3: OPTIMIZED - Use projection to load only necessary fields
        var finalQuery = GetOptimizedDoctorProjection(
            _context.Doctors
                .AsNoTracking()
                .Where(d => doctorIds.Contains(d.Id)));

        // Apply sorting
        finalQuery = ApplySorting(finalQuery, query);

        var totalCount = doctorIds.Count;
        var doctors = await ApplyPagination(finalQuery, query).ToListAsync();

        Console.WriteLine($"Final result: {doctors.Count} doctors, totalCount: {totalCount}");
        return (doctors, totalCount);
    }

    /// <summary>
    /// Apply complex filters (language, service type) on a subset of doctor IDs
    /// </summary>
    private async Task<List<Guid>> ApplyComplexFilters(List<Guid> doctorIds, DoctorQueryRequest query)
    {
        var filteredIds = new HashSet<Guid>(doctorIds);

        // Language filters
        if (!string.IsNullOrEmpty(query.Language) || (query.Languages != null && query.Languages.Any()))
        {
            var languageQuery = _context.DoctorLanguages
                .AsNoTracking()
                .Where(dl => filteredIds.Contains(dl.DoctorId));

            if (!string.IsNullOrEmpty(query.Language))
            {
                languageQuery = languageQuery.Where(dl => dl.Language.Name == query.Language);
            }
            else if (query.Languages != null && query.Languages.Any())
            {
                languageQuery = languageQuery.Where(dl => query.Languages.Contains(dl.Language.Name));
            }

            var languageDoctorIds = await languageQuery.Select(dl => dl.DoctorId).ToListAsync();
            filteredIds.IntersectWith(languageDoctorIds);
            Console.WriteLine($"Language filter: {filteredIds.Count} doctors remaining");
        }

        // Service type filters
        if (!string.IsNullOrEmpty(query.ServiceType) || (query.ServiceTypes != null && query.ServiceTypes.Any()))
        {
            var serviceQuery = _context.DoctorPrices
                .AsNoTracking()
                .Where(dp => filteredIds.Contains(dp.DoctorId));

            if (!string.IsNullOrEmpty(query.ServiceType))
            {
                serviceQuery = serviceQuery.Where(dp => dp.ServiceType.Name == query.ServiceType);
            }
            else if (query.ServiceTypes != null && query.ServiceTypes.Any())
            {
                serviceQuery = serviceQuery.Where(dp => query.ServiceTypes.Contains(dp.ServiceType.Name));
            }

            var serviceDoctorIds = await serviceQuery.Select(dp => dp.DoctorId).ToListAsync();
            filteredIds.IntersectWith(serviceDoctorIds);
            Console.WriteLine($"Service type filter: {filteredIds.Count} doctors remaining");
        }

        return filteredIds.ToList();
    }

    /// <summary>
    /// Check if query has complex filters that need special handling
    /// </summary>
    private bool HasComplexFilters(DoctorQueryRequest query)
    {
        return (!string.IsNullOrEmpty(query.Language) ||
                (query.Languages != null && query.Languages.Any()) ||
                (!string.IsNullOrEmpty(query.ServiceType) ||
                 (query.ServiceTypes != null && query.ServiceTypes.Any())));
    }

    /// <summary>
    /// Count the number of active filters
    /// </summary>
    private int GetFilterCount(DoctorQueryRequest query)
    {
        int count = 0;
        if (query.SpecialtyId.HasValue) count++;
        if (query.PositionId.HasValue) count++;
        if (!string.IsNullOrEmpty(query.Gender)) count++;
        if (query.MinYearsOfExperience.HasValue) count++;
        if (query.MaxYearsOfExperience.HasValue) count++;
        if (!string.IsNullOrEmpty(query.Language)) count++;
        if (query.Languages != null && query.Languages.Any()) count++;
        if (!string.IsNullOrEmpty(query.ServiceType)) count++;
        if (query.ServiceTypes != null && query.ServiceTypes.Any()) count++;
        if (query.MinPrice.HasValue) count++;
        if (query.MaxPrice.HasValue) count++;
        if (!string.IsNullOrEmpty(query.ProvinceId)) count++;
        if (!string.IsNullOrEmpty(query.DistrictId)) count++;
        return count;
    }

    #endregion
}
