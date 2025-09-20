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
        var queryable = _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.ServiceType)
            .Include(d => d.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .AsQueryable();

        // Apply filters
        if (query.AccountId.HasValue)
            queryable = queryable.Where(d => d.AccountId == query.AccountId.Value);
        if (query.PositionId.HasValue)
            queryable = queryable.Where(d => d.PositionId == query.PositionId.Value);
        if (query.SpecialtyId.HasValue)
            queryable = queryable.Where(d => d.SpecialtyId == query.SpecialtyId.Value);
        if (query.HospitalId.HasValue)
            queryable = queryable.Where(d => d.HospitalId == query.HospitalId.Value);
        if (query.Gender.HasValue)
            queryable = queryable.Where(d => d.Gender == query.Gender.Value);
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            queryable = queryable.Where(d =>
                d.FirstName.ToLower().Contains(searchTerm) ||
                d.LastName.ToLower().Contains(searchTerm) ||
                d.Email.ToLower().Contains(searchTerm));
        }
        if (query.MinYearsOfExperience.HasValue)
            queryable = queryable.Where(d => d.YearsOfExperience >= query.MinYearsOfExperience.Value);
        if (query.MaxYearsOfExperience.HasValue)
            queryable = queryable.Where(d => d.YearsOfExperience <= query.MaxYearsOfExperience.Value);
        if (!string.IsNullOrEmpty(query.Address))
            queryable = queryable.Where(d => d.Address != null && d.Address.Contains(query.Address));
        if (!string.IsNullOrEmpty(query.Language))
            queryable = queryable.Where(d => d.DoctorLanguages.Any(dl => dl.Language.Name.Contains(query.Language)));
        if (!string.IsNullOrEmpty(query.ServiceType))
            queryable = queryable.Where(d => d.DoctorPrices.Any(dp => dp.ServiceType.Name.Contains(query.ServiceType)));
        if (query.MinRating.HasValue)
            queryable = queryable.Where(d => d.Bio != null && d.Bio.Contains("rating:" + query.MinRating.Value));

        // Price filter - filter by doctor prices
        if (query.MinPrice.HasValue || query.MaxPrice.HasValue)
        {
            queryable = queryable.Where(d => d.DoctorPrices.Any(dp =>
                (!query.MinPrice.HasValue || dp.Amount >= query.MinPrice.Value) &&
                (!query.MaxPrice.HasValue || dp.Amount <= query.MaxPrice.Value)));
        }

        // Sort
        if (!string.IsNullOrEmpty(query.SortBy))
        {
            if (query.SortBy == "YearsOfExperience")
                queryable = query.SortOrder == "desc" ? queryable.OrderByDescending(d => d.YearsOfExperience) : queryable.OrderBy(d => d.YearsOfExperience);
            else if (query.SortBy == "CreatedAt")
                queryable = query.SortOrder == "desc" ? queryable.OrderByDescending(d => d.CreatedAt) : queryable.OrderBy(d => d.CreatedAt);
        }

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var doctors = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();
        return (doctors, totalCount);
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
