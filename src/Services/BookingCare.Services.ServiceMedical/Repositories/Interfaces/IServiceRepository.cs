using BookingCare.Services.ServiceMedical.Models.Entities;
using BookingCare.Services.ServiceMedical.Models.DTOs.Requests;
using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;

namespace BookingCare.Services.ServiceMedical.Repositories.Interfaces
{
    public interface IServiceRepository
    {
        // CRUD Operations
        Task<ServiceEntity> CreateAsync(ServiceEntity entity);
        Task<ServiceEntity?> GetByIdAsync(Guid id);
        Task<ServiceEntity> UpdateAsync(ServiceEntity entity);
        Task<bool> DeleteAsync(Guid id);

        // Query Operations
        Task<List<ServiceEntity>> GetAllAsync();
        Task<List<ServiceEntity>> GetActiveServicesAsync();
        Task<List<ServiceEntity>> GetServicesByCategoryAsync(Guid categoryId);
        Task<List<ServiceEntity>> GetServicesByHospitalAsync(Guid hospitalId);
        Task<(List<ServiceEntity> Services, int TotalCount)> GetPagedAsync(ServiceQueryRequest request);

        // Business Operations
        Task<List<Guid>> GetHospitalIdsByCategoryAsync(Guid categoryId);
        Task<List<ServiceEntity>> GetServicesWithCategoryAsync();
        Task<List<Guid>> GetAllDistinctHospitalIdsAsync();

        // gRPC Optimized Operations (with projection for performance)
        Task<List<ServiceBasicInfoDto>> GetServicesBasicInfoByIdsAsync(IEnumerable<Guid> ids);

        // Validation Operations
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ServiceExistsForHospitalAsync(Guid serviceId, Guid hospitalId);

        // Queryable for complex queries
        IQueryable<ServiceEntity> GetQueryable();
    }
}
