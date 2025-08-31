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
        {
            queryable = queryable.Where(d => d.AccountId == query.AccountId.Value);
        }

        if (query.PositionId.HasValue)
        {
            queryable = queryable.Where(d => d.PositionId == query.PositionId.Value);
        }

        if (query.SpecialtyId.HasValue)
        {
            queryable = queryable.Where(d => d.SpecialtyId == query.SpecialtyId.Value);
        }

        if (query.ClinicId.HasValue)
        {
            queryable = queryable.Where(d => d.ClinicId == query.ClinicId.Value);
        }

        if (query.Gender.HasValue)
        {
            queryable = queryable.Where(d => d.Gender == query.Gender.Value);
        }

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            queryable = queryable.Where(d => 
                d.FirstName.ToLower().Contains(searchTerm) ||
                d.LastName.ToLower().Contains(searchTerm) ||
                d.Email.ToLower().Contains(searchTerm));
        }

        if (query.MinYearsOfExperience.HasValue)
        {
            queryable = queryable.Where(d => d.YearsOfExperience >= query.MinYearsOfExperience.Value);
        }

        if (query.MaxYearsOfExperience.HasValue)
        {
            queryable = queryable.Where(d => d.YearsOfExperience <= query.MaxYearsOfExperience.Value);
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

    #region Price CRUD Operations

    public async Task<PriceEntity?> GetPriceByIdAsync(Guid id)
    {
        return await _context.Prices
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PriceEntity> CreatePriceAsync(PriceEntity price)
    {
        _context.Prices.Add(price);
        await _context.SaveChangesAsync();
        return price;
    }

    public async Task<PriceEntity> UpdatePriceAsync(PriceEntity price)
    {
        _context.Prices.Update(price);
        await _context.SaveChangesAsync();
        return price;
    }

    public async Task<bool> DeletePriceAsync(Guid id)
    {
        var price = await GetPriceByIdAsync(id);
        if (price == null) return false;

        _context.Prices.Remove(price);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PriceExistsAsync(Guid id)
    {
        return await _context.Prices
            .AnyAsync(p => p.Id == id);
    }

    #endregion

    #region Price Query Operations

    public async Task<(List<PriceEntity> Prices, int TotalCount)> GetPricesAsync(PriceQueryRequest query)
    {
        var queryable = _context.Prices.AsQueryable();

        // Apply amount filters
        if (query.MinAmount.HasValue)
        {
            queryable = queryable.Where(p => p.Amount >= query.MinAmount.Value);
        }

        if (query.MaxAmount.HasValue)
        {
            queryable = queryable.Where(p => p.Amount <= query.MaxAmount.Value);
        }

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var prices = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (prices, totalCount);
    }

    public async Task<List<PriceEntity>> GetAllPricesAsync()
    {
        return await _context.Prices.ToListAsync();
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
