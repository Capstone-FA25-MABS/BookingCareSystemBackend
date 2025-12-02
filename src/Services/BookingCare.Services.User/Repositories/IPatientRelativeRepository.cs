using BookingCare.Services.User.Models.Entities;

namespace BookingCare.Services.User.Repositories;

/// <summary>
/// Repository interface for PatientRelative operations
/// </summary>
public interface IPatientRelativeRepository
{
    /// <summary>
    /// Get all relatives for a specific user
    /// </summary>
    Task<List<PatientRelativeEntity>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Get a specific relative by ID
    /// </summary>
    Task<PatientRelativeEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get a specific relative by ID and verify ownership
    /// </summary>
    Task<PatientRelativeEntity?> GetByIdAndUserIdAsync(Guid id, Guid userId);

    /// <summary>
    /// Create a new relative
    /// </summary>
    Task<PatientRelativeEntity> CreateAsync(PatientRelativeEntity entity);

    /// <summary>
    /// Update an existing relative
    /// </summary>
    Task<PatientRelativeEntity> UpdateAsync(PatientRelativeEntity entity);

    /// <summary>
    /// Delete a relative
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Count relatives for a user (for limit checking)
    /// </summary>
    Task<int> CountByUserIdAsync(Guid userId);

    /// <summary>
    /// Check if relative exists
    /// </summary>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// Get relatives by IDs (for batch retrieval)
    /// </summary>
    Task<List<PatientRelativeEntity>> GetByIdsAsync(List<Guid> ids);
}
