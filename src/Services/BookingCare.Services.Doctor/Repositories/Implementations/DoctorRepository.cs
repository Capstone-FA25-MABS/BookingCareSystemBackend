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
            .Include(d => d.Position)
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
        if (query.AccountId.HasValue)
            queryable = queryable.Where(d => d.AccountId == query.AccountId.Value);

        // Position filters - support both single and multiple
        if (query.PositionId.HasValue)
            queryable = queryable.Where(d => d.PositionId == query.PositionId.Value);
        if (query.PositionIds != null && query.PositionIds.Any())
            queryable = queryable.Where(d => d.PositionId.HasValue && query.PositionIds.Contains(d.PositionId.Value));

        // Specialty filters - support both single and multiple
        if (query.SpecialtyId.HasValue)
            queryable = queryable.Where(d => d.SpecialtyId == query.SpecialtyId.Value);
        if (query.SpecialtyIds != null && query.SpecialtyIds.Any())
            queryable = queryable.Where(d => d.SpecialtyId.HasValue && query.SpecialtyIds.Contains(d.SpecialtyId.Value));

        // Hospital filters - support both single and multiple
        if (query.HospitalId.HasValue)
            queryable = queryable.Where(d => d.HospitalId == query.HospitalId.Value);
        if (query.HospitalIds != null && query.HospitalIds.Any())
            queryable = queryable.Where(d => d.HospitalId.HasValue && query.HospitalIds.Contains(d.HospitalId.Value));

        // Location filters - filter by province/district (requires hospital address data)
        // Note: This filtering will be enhanced at service layer with distance calculation
        if (!string.IsNullOrEmpty(query.ProvinceId) || !string.IsNullOrEmpty(query.DistrictId))
        {
            Console.WriteLine($"Location filtering requested - ProvinceId: {query.ProvinceId}, DistrictId: {query.DistrictId}");
            // Basic filtering - this will be enhanced with distance calculation at service layer
            // For now, we return all doctors and let the service layer handle location-based filtering
            Console.WriteLine("Location filtering will be handled at service layer with distance calculation");
        }

        // Gender filters - support both single and multiple
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

        // Experience filters - prioritize min/max over ranges (like price filter)
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

        if (!string.IsNullOrEmpty(query.Address))
            queryable = queryable.Where(d => d.Address != null && d.Address.Contains(query.Address));

        // Rating filters - will be handled at service layer using Review service
        // Note: Rating filtering is complex and requires calling Review service
        // This will be implemented in DoctorService layer

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
                d.Email.ToLower().Contains(searchTerm));
        }

        // Language filters - support both single and multiple
        if (!string.IsNullOrEmpty(query.Language))
            queryable = queryable.Where(d => d.DoctorLanguages.Any(dl => dl.Language.Name == query.Language));
        if (query.Languages != null && query.Languages.Any())
            queryable = queryable.Where(d => d.DoctorLanguages.Any(dl => query.Languages.Contains(dl.Language.Name)));

        // Service type filters - support both single and multiple
        if (!string.IsNullOrEmpty(query.ServiceType))
            queryable = queryable.Where(d => d.DoctorPrices.Any(dp => dp.ServiceType.Name == query.ServiceType));
        if (query.ServiceTypes != null && query.ServiceTypes.Any())
            queryable = queryable.Where(d => d.DoctorPrices.Any(dp => query.ServiceTypes.Contains(dp.ServiceType.Name)));

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
            .Include(d => d.Position)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .AsQueryable();
    }

    public async Task<List<DoctorEntity>> GetDoctorsByHospitalAsync(Guid hospitalId)
    {
        return await _context.Doctors
            .Include(d => d.Position)
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
            .Include(d => d.Position)
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
            .Include(d => d.Position)
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
            .Include(d => d.Position)
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
}
