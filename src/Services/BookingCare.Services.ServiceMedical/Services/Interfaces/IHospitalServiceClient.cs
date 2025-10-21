using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;

namespace BookingCare.Services.ServiceMedical.Services.Interfaces
{
    /// <summary>
    /// Interface for Hospital Service integration
    /// </summary>
    public interface IHospitalServiceClient
    {
        /// <summary>
        /// Get hospital information by ID
        /// </summary>
        /// <param name="hospitalId">Hospital ID</param>
        /// <returns>Hospital information</returns>
        Task<HospitalInfoResponse?> GetHospitalByIdAsync(Guid hospitalId);

        /// <summary>
        /// Get multiple hospitals by IDs
        /// </summary>
        /// <param name="hospitalIds">List of hospital IDs</param>
        /// <returns>List of hospital information</returns>
        Task<List<HospitalInfoResponse>> GetHospitalsByIdsAsync(List<Guid> hospitalIds);
    }
}
