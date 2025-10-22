using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Services.Interfaces;

public interface IDoctorService
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

    // Get available doctors by hospital, specialty (for Appointment Service)
    // Note: Availability check (appointment conflicts) is done by Appointment Service
    Task<List<DoctorEntity>> GetDoctorsByHospitalAndSpecialtyAsync(Guid hospitalId, Guid specialtyId);

    // Get doctor price by ID (for Appointment Service - Option 3 reschedule)
    Task<DoctorPriceResponse?> GetDoctorPriceByIdAsync(Guid priceId);
}
