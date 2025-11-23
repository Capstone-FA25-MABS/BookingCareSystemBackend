using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Models.DTOs.Requests;

namespace BookingCare.Services.Hospital.Repositories.Interfaces;

public interface IHospitalRepository
{
    Task<HospitalEntity?> GetByIdAsync(Guid id);
    Task<HospitalEntity?> GetByEmailAsync(string email);
    Task<List<HospitalEntity>> GetAllAsync();
    Task<(List<HospitalEntity> hospitals, int totalCount)> GetFilteredAsync(HospitalFilterRequest filter);
    Task<HospitalEntity> CreateAsync(HospitalEntity hospital);
    Task<HospitalEntity> UpdateAsync(HospitalEntity hospital);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
    Task<List<HospitalEntity>> GetBySpecialtyAsync(Guid specialtyId);
    Task<List<HospitalEntity>> GetByAccountIdAsync(Guid accountId);

    // Optimized methods for gRPC performance
    Task<HospitalEntity?> GetHospitalBasicInfoByIdAsync(Guid id);
    Task<List<HospitalEntity>> GetHospitalsBasicInfoByIdsAsync(IEnumerable<Guid> ids);

    // Optimized methods for simple responses
    Task<List<HospitalEntity>> GetActiveHospitalsSimpleAsync();

    // Optimized method for hospital list with essential fields and filters
    Task<(List<HospitalEntity> hospitals, int totalCount)> GetOptimizedHospitalListAsync(HospitalListOptimizedFilterRequest filter);

    // Get hospitals by account IDs (batch operation for admin management)
    Task<List<HospitalEntity>> GetByAccountIdsAsync(IEnumerable<Guid> accountIds);

    // Specialty Management
    Task<bool> AddSpecialtyAsync(Guid hospitalId, Guid specialtyId);
    Task<bool> RemoveSpecialtyAsync(Guid hospitalId, Guid specialtyId);
    Task UpdateHospitalSpecialtiesBatchAsync(Guid hospitalId, List<Guid> specialtyIds);
    Task<List<Guid>> GetHospitalSpecialtyIdsAsync(Guid hospitalId);

    // ServiceType Management
    Task<bool> AddServiceTypeAsync(Guid hospitalId, Guid serviceTypeId);
    Task<bool> RemoveServiceTypeAsync(Guid hospitalId, Guid serviceTypeId);
    Task UpdateHospitalServiceTypesBatchAsync(Guid hospitalId, List<Guid> serviceTypeIds);
    Task<List<Guid>> GetHospitalServiceTypeIdsAsync(Guid hospitalId);

    // ServiceMedical Management
    Task<bool> AddServiceMedicalAsync(Guid hospitalId, Guid serviceMedicalId);
    Task<bool> RemoveServiceMedicalAsync(Guid hospitalId, Guid serviceMedicalId);
}
