using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

public interface IHospitalService
{
    Task<HospitalProfileResponse?> GetByIdAsync(Guid id);
    Task<HospitalResponse?> GetByEmailAsync(string email);
    Task<HospitalListResponse> GetAllAsync();
    Task<HospitalListResponse> GetFilteredAsync(HospitalFilterRequest filter);
    Task<HospitalDetailResponse> CreateAsync(CreateHospitalRequest request);
    Task<HospitalResponse> UpdateAsync(Guid id, UpdateHospitalRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<List<HospitalResponse>> GetBySpecialtyAsync(Guid specialtyId);
    Task<List<HospitalResponse>> GetByAccountIdAsync(Guid accountId);
    Task<bool> AddSpecialtyAsync(Guid hospitalId, Guid specialtyId);
    Task<bool> RemoveSpecialtyAsync(Guid hospitalId, Guid specialtyId);

    // Optimized methods for gRPC performance
    Task<Models.Entities.HospitalEntity?> GetHospitalBasicInfoByIdAsync(Guid id);
    Task<List<Models.Entities.HospitalEntity>> GetHospitalsBasicInfoByIdsAsync(IEnumerable<Guid> ids);

    // Optimized methods for simple responses
    Task<List<HospitalSimpleResponse>> GetActiveHospitalsSimpleAsync();

    // Optimized method for hospital list with essential fields and filters
    Task<HospitalListOptimizedPaginatedResponse> GetOptimizedHospitalListAsync(HospitalListOptimizedFilterRequest filter);

    // Get hospitals by account IDs (batch operation for admin management)
    Task<List<Models.Entities.HospitalEntity>> GetHospitalsByAccountIdsAsync(IEnumerable<Guid> accountIds);

    // Hospital Image Management
    Task<HospitalImageResponse?> AddHospitalImageAsync(CreateHospitalImageRequest request);
    Task<bool> DeleteHospitalImageAsync(Guid hospitalId, Guid imageId);
}
