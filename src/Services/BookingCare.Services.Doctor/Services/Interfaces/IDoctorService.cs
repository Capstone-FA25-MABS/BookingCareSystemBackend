using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Shared.Common.Interfaces;

namespace BookingCare.Services.Doctor.Services.Interfaces;

public interface IDoctorService : IAvatarService
{
    // Doctor CRUD operations
    Task<DoctorResponse> CreateDoctorAsync(CreateDoctorRequest request);
    Task<DoctorByIdResponse?> GetDoctorByIdAsync(Guid id);
    Task<DoctorResponse?> GetDoctorByEmailAsync(string email);
    Task<DoctorResponse?> GetDoctorByAccountIdAsync(Guid accountId);
    Task<DoctorResponse> UpdateDoctorAsync(UpdateDoctorRequest request);
    Task<bool> DeleteDoctorAsync(Guid id);
    Task<bool> ToggleDoctorStatusAsync(Guid id);

    // Doctor Query operations
    Task<DoctorListResponse> GetDoctorsAsync(DoctorQueryRequest query);
    Task<DoctorListResponse> FilterDoctorsAsync(DoctorAdvancedFilterRequest filter);
    Task<DoctorSearchListResponse> FilterDoctorsOptimizedAsync(DoctorAdvancedFilterRequest filter);
    Task<List<DoctorResponse>> GetDoctorsByHospitalAsync(Guid hospitalId);
    Task<DoctorSearchListResponse> GetDoctorsByHospitalOptimizedAsync(Guid hospitalId, int pageNumber = 1, int pageSize = 10);
    Task<List<DoctorResponse>> GetDoctorsBySpecialtyAsync(Guid specialtyId);
    Task<List<DoctorResponse>> GetDoctorsByPositionAsync(Guid positionId);
    Task<List<DoctorResponse>> GetActiveDoctorsAsync();
    Task<DoctorListResponse> GetPatientFavoriteDoctorsAsync(Guid patientId, int page = 1, int pageSize = 9, string? searchTerm = null);
    Task<List<DoctorBasicInfoResponse>> GetDoctorsByAccountIdsAsync(IEnumerable<Guid> accountIds);

    // DoctorPrice operations
    Task<List<DoctorPriceResponse>> GetDoctorPricesAsync(Guid doctorId);
    Task<DoctorPriceResponse> AssignPriceToDoctorAsync(AssignPriceToDoctorRequest request);
    Task<bool> RemovePriceFromDoctorAsync(Guid doctorId, Guid priceId);

    // Validation operations
    Task<bool> DoctorExistsAsync(Guid id);
    Task<bool> DoctorEmailExistsAsync(string email, Guid? excludeId = null);
    Task<bool> DoctorAccountExistsAsync(Guid accountId, Guid? excludeId = null);
    Task<bool> DoctorPriceExistsAsync(Guid doctorId, Guid priceId);

    // Helper methods
    IQueryable<DoctorEntity> GetQueryableDoctors();

    // Optimized methods for Patient Search
    Task<DoctorSearchListResponse> SearchDoctorsForPatientsAsync(DoctorQueryRequest query, Guid? patientId = null);
    Task<DoctorListResponse> GetDoctorsWithFavoriteStatusAsync(DoctorQueryRequest query, Guid patientId);

    // Optimized methods for gRPC performance
    Task<DoctorEntity?> GetDoctorBasicInfoByIdAsync(Guid id);
    Task<List<DoctorEntity>> GetDoctorsBasicInfoByIdsAsync(IEnumerable<Guid> ids);

    /// <summary>
    /// Get consultation fees for multiple doctors by service type (batch operation for performance)
    /// Returns a dictionary of doctorId -> price, only includes doctors that have the specified service type
    /// </summary>
    Task<Dictionary<Guid, decimal>> GetDoctorsPricesByServiceTypeAsync(IEnumerable<Guid> doctorIds, string serviceTypeName);

    // Get available doctors by hospital, specialty (for Appointment Service)
    // Note: Availability check (appointment conflicts) is done by Appointment Service
    Task<List<DoctorEntity>> GetDoctorsByHospitalAndSpecialtyAsync(Guid hospitalId, Guid specialtyId);

    /// <summary>
    /// Get active doctor IDs by hospital and specialty (optimized for schedule aggregation)
    /// Returns only active doctor IDs after filtering by Auth Service status
    /// Optionally filters by appointment type (service type name)
    /// </summary>
    Task<List<Guid>> GetActiveDoctorIdsByHospitalAndSpecialtyAsync(Guid hospitalId, Guid specialtyId, string? appointmentType = null);

    // Get doctor price by ID (for Appointment Service - Option 3 reschedule)
    Task<DoctorPriceResponse?> GetDoctorPriceByIdAsync(Guid priceId);

    // Doctor count operations
    Task<Dictionary<Guid, int>> GetDoctorCountsBySpecialtyAndHospitalAsync(Guid hospitalId, IEnumerable<Guid> specialtyIds);

    // Get service types by hospital with doctor count
    Task<List<(Guid ServiceTypeId, string ServiceTypeName, string? ServiceTypeImageUrl, int DoctorCount)>> GetServiceTypesByHospitalAsync(Guid hospitalId);

    // Avatar operations
    Task<bool> UpdateDoctorAvatarAsync(Guid accountId, string avatarUrl);

    // Hospital staff management operations (optimized for performance)
    Task<List<Guid>> GetDoctorAccountIdsByHospitalIdAsync(Guid hospitalId);

    // Filter doctors for AI recommendations (by specialty IDs, location)
    Task<List<DoctorEntity>> FilterDoctorsForRecommendationAsync(
        List<Guid> specialtyIds,
        string? provinceId,
        string? districtId,
        int maxResults = 10);

    // Methods for doctor assignment flow (hospital staff assigns doctor to pending appointments)
    /// <summary>
    /// Get doctors for assignment by hospital, specialty and appointment type
    /// Returns doctors with full info (rating, position, specialty, consultation fee, account status)
    /// </summary>
    Task<List<DoctorForAssignmentResponse>> GetDoctorsForAssignmentAsync(
        Guid hospitalId,
        Guid specialtyId,
        string appointmentType);

    /// <summary>
    /// Get doctors for assignment by specific doctor IDs
    /// Used for "previous doctors" section - doctors who have treated this patient before
    /// </summary>
    Task<List<DoctorForAssignmentResponse>> GetDoctorsByIdsForAssignmentAsync(
        List<Guid> doctorIds,
        string appointmentType);
}
