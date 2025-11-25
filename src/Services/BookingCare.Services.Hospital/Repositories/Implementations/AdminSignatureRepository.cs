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

    public AdminSignatureRepository(HospitalDbContext context)
    {
        _context = context;
    }

    public async Task<AdminSignatureEntity?> GetByIdAsync(Guid id)
    {
        return await _context.AdminSignatures
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<AdminSignatureEntity?> GetByAdminIdAsync(string adminId)
    {
        return await _context.AdminSignatures
            .Where(s => s.AdminId == adminId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<AdminSignatureEntity?> GetActiveByAdminIdAsync(string adminId)
    {
        return await _context.AdminSignatures
            .Where(s => s.AdminId == adminId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<AdminSignatureEntity>> GetAllAsync()
    {
        return await _context.AdminSignatures
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<AdminSignatureEntity> CreateAsync(AdminSignatureEntity signature)
    {
        await _context.AdminSignatures.AddAsync(signature);
        await _context.SaveChangesAsync();
        return signature;
    }

    public async Task<AdminSignatureEntity> UpdateAsync(AdminSignatureEntity signature)
    {
        _context.AdminSignatures.Update(signature);
        await _context.SaveChangesAsync();
        return signature;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var signature = await GetByIdAsync(id);
        if (signature == null)
            return false;

        _context.AdminSignatures.Remove(signature);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task DeactivateAllByAdminIdAsync(string adminId)
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
}
