using BookingCare.Services.Hospital.Models.Entities;

namespace BookingCare.Services.Hospital.Repositories.Interfaces;

/// <summary>
/// Repository interface for admin signature operations
/// </summary>
public interface IAdminSignatureRepository
{
    /// <summary>
    /// Get admin signature by ID
    /// </summary>
    Task<AdminSignatureEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get admin signature by admin ID
    /// </summary>
    Task<AdminSignatureEntity?> GetByAdminIdAsync(string adminId);

    /// <summary>
    /// Get active admin signature by admin ID
    /// </summary>
    Task<AdminSignatureEntity?> GetActiveByAdminIdAsync(string adminId);

    /// <summary>
    /// Get all admin signatures
    /// </summary>
    Task<IEnumerable<AdminSignatureEntity>> GetAllAsync();

    /// <summary>
    /// Create a new admin signature
    /// </summary>
    Task<AdminSignatureEntity> CreateAsync(AdminSignatureEntity signature);

    /// <summary>
    /// Update an existing admin signature
    /// </summary>
    Task<AdminSignatureEntity> UpdateAsync(AdminSignatureEntity signature);

    /// <summary>
    /// Delete an admin signature
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Deactivate all signatures for an admin (before creating a new one)
    /// </summary>
    Task DeactivateAllByAdminIdAsync(string adminId);
}
