using BookingCare.Services.User.Models.DTOs;

namespace BookingCare.Services.User.Services;

/// <summary>
/// Service interface for PatientRelative operations
/// </summary>
public interface IPatientRelativeService
{
    /// <summary>
    /// Get all relatives for the current user
    /// </summary>
    Task<List<PatientRelativeResponse>> GetMyRelativesAsync(Guid userId);

    /// <summary>
    /// Get all relatives for dropdown/selection (lightweight)
    /// </summary>
    Task<List<PatientRelativeBasicResponse>> GetMyRelativesBasicAsync(Guid userId);

    /// <summary>
    /// Get a specific relative by ID
    /// </summary>
    Task<PatientRelativeResponse?> GetRelativeByIdAsync(Guid id, Guid userId);

    /// <summary>
    /// Create a new relative
    /// </summary>
    Task<PatientRelativeResponse> CreateRelativeAsync(Guid userId, CreatePatientRelativeRequest request);

    /// <summary>
    /// Update an existing relative
    /// </summary>
    Task<PatientRelativeResponse> UpdateRelativeAsync(Guid id, Guid userId, UpdatePatientRelativeRequest request);

    /// <summary>
    /// Delete a relative
    /// </summary>
    Task<bool> DeleteRelativeAsync(Guid id, Guid userId);

    /// <summary>
    /// Get relatives by IDs (for gRPC batch retrieval)
    /// </summary>
    Task<List<PatientRelativeResponse>> GetRelativesByIdsAsync(List<Guid> ids);
}
