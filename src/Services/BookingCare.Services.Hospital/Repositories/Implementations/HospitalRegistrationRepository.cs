using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Enums;
using BookingCare.Services.Hospital.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Hospital.Repositories.Implementations;

public class HospitalRegistrationRepository : IHospitalRegistrationRepository
{
    private readonly HospitalDbContext _context;
    private readonly ILogger<HospitalRegistrationRepository> _logger;

    public HospitalRegistrationRepository(
        HospitalDbContext context,
        ILogger<HospitalRegistrationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HospitalRegistrationEntity> CreateAsync(HospitalRegistrationEntity registration)
    {
        try
        {
            await _context.HospitalRegistrations.AddAsync(registration);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created hospital registration with ID: {RegistrationId}", registration.Id);
            return registration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hospital registration");
            throw new HospitalRegistrationException("Failed to create hospital registration", ex);
        }
    }

    public async Task<HospitalRegistrationEntity?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.HospitalRegistrations
                .Include(r => r.Hospital)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hospital registration by ID: {RegistrationId}", id);
            throw new HospitalRegistrationException("Failed to get hospital registration by ID", ex);
        }
    }

    public async Task<HospitalRegistrationEntity?> GetByEmailAsync(string email)
    {
        try
        {
            return await _context.HospitalRegistrations
                .FirstOrDefaultAsync(r => r.Email.ToLower() == email.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hospital registration by email: {Email}", email);
            throw new HospitalRegistrationException("Failed to get hospital registration by email", ex);
        }
    }

    public async Task<HospitalRegistrationEntity?> GetByTaxCodeAsync(string taxCode)
    {
        try
        {
            return await _context.HospitalRegistrations
                .FirstOrDefaultAsync(r => r.TaxCode == taxCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hospital registration by tax code: {TaxCode}", taxCode);
            throw new HospitalRegistrationException("Failed to get hospital registration by tax code", ex);
        }
    }

    public async Task<(List<HospitalRegistrationEntity> Registrations, int TotalCount)> GetAllAsync(
        string? searchTerm,
        RegistrationStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder)
    {
        try
        {
            var query = _context.HospitalRegistrations
                .Include(r => r.Hospital)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(r =>
                    r.HospitalName.ToLower().Contains(lowerSearchTerm) ||
                    r.Email.ToLower().Contains(lowerSearchTerm) ||
                    r.Phone.Contains(searchTerm) ||
                    r.TaxCode.Contains(searchTerm));
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= toDate.Value);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "hospitalname" => sortOrder.ToUpper() == "ASC"
                    ? query.OrderBy(r => r.HospitalName)
                    : query.OrderByDescending(r => r.HospitalName),
                "email" => sortOrder.ToUpper() == "ASC"
                    ? query.OrderBy(r => r.Email)
                    : query.OrderByDescending(r => r.Email),
                "status" => sortOrder.ToUpper() == "ASC"
                    ? query.OrderBy(r => r.Status)
                    : query.OrderByDescending(r => r.Status),
                "updatedat" => sortOrder.ToUpper() == "ASC"
                    ? query.OrderBy(r => r.UpdatedAt)
                    : query.OrderByDescending(r => r.UpdatedAt),
                _ => sortOrder.ToUpper() == "ASC"
                    ? query.OrderBy(r => r.CreatedAt)
                    : query.OrderByDescending(r => r.CreatedAt)
            };

            // Apply pagination
            var registrations = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} hospital registrations (Page {Page}, PageSize {PageSize})",
                registrations.Count, page, pageSize);

            return (registrations, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all hospital registrations");
            throw new HospitalRegistrationException("Failed to get all hospital registrations", ex);
        }
    }

    public async Task<HospitalRegistrationEntity> UpdateAsync(HospitalRegistrationEntity registration)
    {
        try
        {
            registration.UpdatedAt = DateTime.Now;
            _context.HospitalRegistrations.Update(registration);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated hospital registration: {RegistrationId}", registration.Id);
            return registration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hospital registration: {RegistrationId}", registration.Id);
            throw new HospitalRegistrationException("Failed to update hospital registration", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var registration = await GetByIdAsync(id);
            if (registration == null)
            {
                _logger.LogWarning("Hospital registration not found for deletion: {RegistrationId}", id);
                return false;
            }

            _context.HospitalRegistrations.Remove(registration);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted hospital registration: {RegistrationId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hospital registration: {RegistrationId}", id);
            throw new HospitalRegistrationException("Failed to delete hospital registration", ex);
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            return await _context.HospitalRegistrations.AnyAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking hospital registration existence: {RegistrationId}", id);
            throw new HospitalRegistrationException("Failed to check hospital registration existence", ex);
        }
    }

}

