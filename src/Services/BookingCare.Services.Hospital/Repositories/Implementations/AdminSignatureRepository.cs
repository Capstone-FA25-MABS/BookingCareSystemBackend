using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Hospital.Repositories.Implementations;

/// <summary>
/// Repository implementation for admin signature operations
/// </summary>
public class AdminSignatureRepository : IAdminSignatureRepository
{
    private readonly HospitalDbContext _context;
    private readonly ILogger<AdminSignatureRepository> _logger;

    public AdminSignatureRepository(
        HospitalDbContext context,
        ILogger<AdminSignatureRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AdminSignatureEntity?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.AdminSignatures
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin signature by ID: {Id}", id);
            throw;
        }
    }

    public async Task<AdminSignatureEntity?> GetByAdminIdAsync(string adminId)
    {
        try
        {
            return await _context.AdminSignatures
                .Where(s => s.AdminId == adminId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin signature by admin ID: {AdminId}", adminId);
            throw;
        }
    }

    public async Task<AdminSignatureEntity?> GetActiveByAdminIdAsync(string adminId)
    {
        try
        {
            return await _context.AdminSignatures
                .Where(s => s.AdminId == adminId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active admin signature by admin ID: {AdminId}", adminId);
            throw;
        }
    }

    public async Task<IEnumerable<AdminSignatureEntity>> GetAllAsync()
    {
        try
        {
            return await _context.AdminSignatures
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all admin signatures");
            throw;
        }
    }

    public async Task<AdminSignatureEntity> CreateAsync(AdminSignatureEntity signature)
    {
        try
        {
            await _context.AdminSignatures.AddAsync(signature);
            await _context.SaveChangesAsync();
            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin signature for admin: {AdminId}", signature.AdminId);
            throw;
        }
    }

    public async Task<AdminSignatureEntity> UpdateAsync(AdminSignatureEntity signature)
    {
        try
        {
            _context.AdminSignatures.Update(signature);
            await _context.SaveChangesAsync();
            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating admin signature: {Id}", signature.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var signature = await GetByIdAsync(id);
            if (signature == null)
                return false;

            _context.AdminSignatures.Remove(signature);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting admin signature: {Id}", id);
            throw;
        }
    }

    public async Task DeactivateAllByAdminIdAsync(string adminId)
    {
        try
        {
            var signatures = await _context.AdminSignatures
                .Where(s => s.AdminId == adminId && s.IsActive)
                .ToListAsync();

            foreach (var signature in signatures)
            {
                signature.IsActive = false;
                signature.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating signatures for admin: {AdminId}", adminId);
            throw;
        }
    }
}
