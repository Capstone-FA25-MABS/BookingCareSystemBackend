using BookingCare.Services.ServiceMedical.Models.Entities;

namespace BookingCare.Services.ServiceMedical.Repositories.Interfaces
{
    public interface IServiceCategoryRepository
    {
        // CRUD Operations
        Task<ServiceCategoryEntity> CreateAsync(ServiceCategoryEntity entity);
        Task<ServiceCategoryEntity?> GetByIdAsync(Guid id);
        Task<ServiceCategoryEntity> UpdateAsync(ServiceCategoryEntity entity);
        Task<bool> DeleteAsync(Guid id);

        // Query Operations
        Task<List<ServiceCategoryEntity>> GetAllAsync();
        Task<List<ServiceCategoryEntity>> GetParentCategoriesAsync();
        Task<List<ServiceCategoryEntity>> GetChildrenAsync(Guid parentId);
        Task<List<ServiceCategoryEntity>> GetActiveCategoriesAsync();
        Task<(List<ServiceCategoryEntity> Categories, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? searchTerm = null, string? status = null, Guid? parentId = null);

        // Validation Operations
        Task<bool> ExistsAsync(Guid id);
        Task<bool> HasChildrenAsync(Guid id);
        Task<bool> IsValidParentAsync(Guid parentId, Guid childId);

        // Business Operations
        Task<List<ServiceCategoryEntity>> GetCategoryHierarchyAsync(Guid categoryId);
        
        // Queryable for complex queries
        IQueryable<ServiceCategoryEntity> GetQueryable();
    }
}
