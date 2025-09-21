using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Repositories.Interfaces;

public interface IServiceTypeRepository
{
    // ServiceType CRUD operations
    Task<ServiceTypeEntity?> GetServiceTypeByIdAsync(Guid id);
    Task<ServiceTypeEntity?> GetServiceTypeByNameAsync(string name);
    Task<ServiceTypeEntity> CreateServiceTypeAsync(ServiceTypeEntity serviceType);
    Task<ServiceTypeEntity> UpdateServiceTypeAsync(ServiceTypeEntity serviceType);
    Task<bool> DeleteServiceTypeAsync(Guid id);
    Task<bool> ServiceTypeExistsAsync(Guid id);
    Task<bool> ServiceTypeNameExistsAsync(string name, Guid? excludeId = null);

    // ServiceType Query operations
    Task<(List<ServiceTypeEntity> ServiceTypes, int TotalCount)> GetServiceTypesAsync(ServiceTypeQueryRequest query);
    Task<List<ServiceTypeEntity>> GetAllServiceTypesAsync();
    IQueryable<ServiceTypeEntity> GetQueryableServiceTypes();
}
