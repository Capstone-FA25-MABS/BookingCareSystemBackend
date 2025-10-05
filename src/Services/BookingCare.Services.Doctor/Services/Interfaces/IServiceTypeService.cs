using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;

namespace BookingCare.Services.Doctor.Services.Interfaces;

public interface IServiceTypeService
{
    // ServiceType CRUD operations
    Task<ServiceTypeResponse> CreateServiceTypeAsync(CreateServiceTypeRequest request);
    Task<ServiceTypeResponse?> GetServiceTypeByIdAsync(Guid id);
    Task<ServiceTypeResponse?> GetServiceTypeByNameAsync(string name);
    Task<ServiceTypeResponse> UpdateServiceTypeAsync(UpdateServiceTypeRequest request);
    Task<bool> DeleteServiceTypeAsync(Guid id);
    Task<bool> ToggleDoctorServiceTypeStatusAsync(Guid id);

    // ServiceType Query operations
    Task<ServiceTypeListResponse> GetServiceTypesAsync(ServiceTypeQueryRequest query);
    Task<List<ServiceTypeResponse>> GetAllServiceTypesAsync();

    // Validation operations
    Task<bool> ServiceTypeExistsAsync(Guid id);
    Task<bool> ServiceTypeNameExistsAsync(string name, Guid? excludeId = null);
}
