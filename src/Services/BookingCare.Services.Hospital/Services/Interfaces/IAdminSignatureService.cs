using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

/// <summary>
/// Service interface for admin signature management
/// </summary>
public interface IAdminSignatureService
{
    /// <summary>
    /// Get admin signature by ID
    /// </summary>
    Task<AdminSignatureResponseDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get active admin signature for current admin
    /// </summary>
    Task<AdminSignatureResponseDto?> GetActiveSignatureAsync(string adminId);

    /// <summary>
    /// Get all admin signatures
    /// </summary>
    Task<IEnumerable<AdminSignatureResponseDto>> GetAllAsync();

    /// <summary>
    /// Create a new admin signature
    /// </summary>
    Task<AdminSignatureResponseDto> CreateAsync(string adminId, CreateAdminSignatureRequestDto request);

    /// <summary>
    /// Update an existing admin signature
    /// </summary>
    Task<AdminSignatureResponseDto> UpdateAsync(Guid id, string adminId, UpdateAdminSignatureRequestDto request);

    /// <summary>
    /// Delete an admin signature
    /// </summary>
    Task<bool> DeleteAsync(Guid id, string adminId);
}
