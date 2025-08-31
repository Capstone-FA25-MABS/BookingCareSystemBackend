using BookingCare.Services.Clinic.Protos;

namespace BookingCare.Services.Discount.Services;

/// <summary>
/// Interface for clinic validation services via gRPC
/// </summary>
public interface IClinicValidationService
{
    /// <summary>
    /// Validates if a clinic exists and is active
    /// </summary>
    /// <param name="clinicId">The clinic ID to validate</param>
    /// <returns>True if clinic is valid and active, false otherwise</returns>
    Task<bool> ValidateClinicAsync(long clinicId);
    
    /// <summary>
    /// Gets clinic information including validation status
    /// </summary>
    /// <param name="clinicId">The clinic ID to get information for</param>
    /// <returns>Clinic validation response with detailed information</returns>
    Task<ValidateClinicResponse> GetClinicValidationAsync(long clinicId);
}
