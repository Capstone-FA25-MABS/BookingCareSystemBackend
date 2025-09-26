using BookingCare.Services.ServiceMedical.Models.DTOs.Requests;
using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;

namespace BookingCare.Services.ServiceMedical.Services.Interfaces
{
    public interface IServiceMedicalService
    {
        #region ServiceCategory Operations

        // CRUD Operations
        Task<ServiceCategoryResponse> CreateServiceCategoryAsync(CreateServiceCategoryRequest request);
        Task<ServiceCategoryResponse?> GetServiceCategoryByIdAsync(Guid id);
        Task<ServiceCategoryResponse> UpdateServiceCategoryAsync(UpdateServiceCategoryRequest request);
        Task<bool> DeleteServiceCategoryAsync(Guid id);

        // Query Operations
        Task<ServiceCategoryListResponse> GetServiceCategoriesAsync(ServiceCategoryQueryRequest query);
        Task<List<ServiceCategoryResponse>> GetParentServiceCategoriesAsync();
        Task<List<ServiceCategoryResponse>> GetServiceCategoryChildrenAsync(GetServiceCategoryChildrenRequest request);
        Task<List<ServiceCategoryResponse>> GetActiveServiceCategoriesAsync();

        // Business Operations
        Task<List<ServiceCategoryResponse>> GetServiceCategoryHierarchyAsync(Guid categoryId);

        #endregion

        #region Service Operations

        // CRUD Operations
        Task<ServiceResponse> CreateServiceAsync(CreateServiceRequest request);
        Task<ServiceResponse?> GetServiceByIdAsync(Guid id);
        Task<ServiceResponse> UpdateServiceAsync(UpdateServiceRequest request);
        Task<bool> DeleteServiceAsync(Guid id);

        // Query Operations
        Task<ServiceListResponse> GetServicesAsync(ServiceQueryRequest query);
        Task<ServiceListResponse> GetServicesByCategoryAsync(GetServicesByCategoryRequest request);
        Task<List<ServiceResponse>> GetServicesByHospitalAsync(Guid hospitalId);
        Task<List<ServiceResponse>> GetActiveServicesAsync();

        // Business Operations - Theo luồng bạn yêu cầu
        Task<HospitalsByServiceCategoryResponse> GetHospitalsByServiceCategoryAsync(GetHospitalsByServiceCategoryRequest request);

        #endregion


        #region Validation Operations

        Task<bool> ServiceCategoryExistsAsync(Guid id);
        Task<bool> ServiceExistsAsync(Guid id);

        #endregion
    }
}
