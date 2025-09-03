using BookingCare.Services.Doctor.Data;
using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Shared.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Doctor.Repositories;

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
                .ThenInclude(dp => dp.Price)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DoctorEntity?> GetDoctorByEmailAsync(string email)
    {
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.Price)
            .FirstOrDefaultAsync(d => d.Email == email);
    }

    public async Task<DoctorEntity?> GetDoctorByAccountIdAsync(Guid accountId)
    {
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.Price)
            .FirstOrDefaultAsync(d => d.AccountId == accountId);
    }

    public async Task<DoctorEntity> CreateDoctorAsync(DoctorEntity doctor)
    {
        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();
        return doctor;
    }

    public async Task<DoctorEntity> UpdateDoctorAsync(DoctorEntity doctor)
    {
        _context.Doctors.Update(doctor);
        await _context.SaveChangesAsync();
        return doctor;
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
                .ThenInclude(dp => dp.Price)
            .AsQueryable();

        // Apply filters
        if (query.AccountId.HasValue)
            queryable = queryable.Where(d => d.AccountId == query.AccountId.Value);
        if (query.PositionId.HasValue)
            queryable = queryable.Where(d => d.PositionId == query.PositionId.Value);
        if (query.SpecialtyId.HasValue)
            queryable = queryable.Where(d => d.SpecialtyId == query.SpecialtyId.Value);
        if (query.ClinicId.HasValue)
            queryable = queryable.Where(d => d.ClinicId == query.ClinicId.Value);
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
            queryable = queryable.Where(d => d.Bio != null && d.Bio.Contains(query.Language)); // cần trường riêng cho Language
        if (!string.IsNullOrEmpty(query.ServiceType))
            queryable = queryable.Where(d => d.Bio != null && d.Bio.Contains(query.ServiceType)); // cần trường riêng cho ServiceType
        if (query.MinRating.HasValue)
            queryable = queryable.Where(d => d.Bio != null && d.Bio.Contains("rating:" + query.MinRating.Value)); // cần trường riêng cho Rating
        // Price filter (dùng giá override hoặc dynamic, cần join hoặc xử lý ở service)
        if (query.MinPrice.HasValue || query.MaxPrice.HasValue)
        {
            // Lọc theo giá override (nếu có), nếu không thì sẽ filter ở service sau khi tính giá động
            queryable = queryable.Where(d => d.DoctorPrices.Any(dp => dp.IsOverride &&
                (!query.MinPrice.HasValue || dp.Price.Amount >= query.MinPrice.Value) &&
                (!query.MaxPrice.HasValue || dp.Price.Amount <= query.MaxPrice.Value)));
        }
        // AvailableTime filter: cần join với bảng lịch, chưa implement ở đây
        // Sort
        if (!string.IsNullOrEmpty(query.SortBy))
        {
            if (query.SortBy == "YearsOfExperience")
                queryable = query.SortOrder == "desc" ? queryable.OrderByDescending(d => d.YearsOfExperience) : queryable.OrderBy(d => d.YearsOfExperience);
            else if (query.SortBy == "CreatedAt")
                queryable = query.SortOrder == "desc" ? queryable.OrderByDescending(d => d.CreatedAt) : queryable.OrderBy(d => d.CreatedAt);
            // Có thể bổ sung sort theo các trường khác
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
                .ThenInclude(dp => dp.Price)
            .AsQueryable();
    }

    public async Task<List<DoctorEntity>> GetDoctorsByClinicAsync(Guid clinicId)
    {
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.Price)
            .Where(d => d.ClinicId == clinicId)
            .ToListAsync();
    }

    public async Task<List<DoctorEntity>> GetDoctorsBySpecialtyAsync(Guid specialtyId)
    {
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.Price)
            .Where(d => d.SpecialtyId == specialtyId)
            .ToListAsync();
    }

    public async Task<List<DoctorEntity>> GetDoctorsByPositionAsync(Guid positionId)
    {
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.Price)
            .Where(d => d.PositionId == positionId)
            .ToListAsync();
    }

    public async Task<List<DoctorEntity>> GetActiveDoctorsAsync()
    {
        return await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.DoctorPrices)
                .ThenInclude(dp => dp.Price)
            .ToListAsync();
    }

    #endregion

    #region DoctorPrice CRUD Operations

    public async Task<DoctorPriceEntity?> GetDoctorPriceAsync(Guid doctorId, Guid priceId)
    {
        return await _context.DoctorPrices
            .Include(dp => dp.Doctor)
            .Include(dp => dp.Price)
            .FirstOrDefaultAsync(dp => dp.DoctorId == doctorId && dp.PriceId == priceId);
    }

    public async Task<DoctorPriceEntity> CreateDoctorPriceAsync(DoctorPriceEntity doctorPrice)
    {
        _context.DoctorPrices.Add(doctorPrice);
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
            .AnyAsync(dp => dp.DoctorId == doctorId && dp.PriceId == priceId);
    }

    #endregion

    #region DoctorPrice Query Operations

    public async Task<List<DoctorPriceEntity>> GetDoctorPricesAsync(Guid doctorId)
    {
        return await _context.DoctorPrices
            .Include(dp => dp.Doctor)
            .Include(dp => dp.Price)
            .Where(dp => dp.DoctorId == doctorId)
            .ToListAsync();
    }

    public async Task<List<PriceEntity>> GetDoctorPricesByDoctorIdAsync(Guid doctorId)
    {
        return await _context.DoctorPrices
            .Include(dp => dp.Price)
            .Where(dp => dp.DoctorId == doctorId)
            .Select(dp => dp.Price)
            .ToListAsync();
    }

    public async Task<List<DoctorEntity>> GetDoctorsByPriceIdAsync(Guid priceId)
    {
        return await _context.DoctorPrices
            .Include(dp => dp.Doctor)
            .Include(dp => dp.Doctor.Position)
            .Where(dp => dp.PriceId == priceId)
            .Select(dp => dp.Doctor)
            .ToListAsync();
    }

    #endregion
}
