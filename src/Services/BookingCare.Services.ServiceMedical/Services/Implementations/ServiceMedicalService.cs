using AutoMapper;
using BookingCare.Services.ServiceMedical.Models.DTOs.Requests;
using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;
using BookingCare.Services.ServiceMedical.Models.Entities;
using BookingCare.Services.ServiceMedical.Repositories.Interfaces;
using BookingCare.Services.ServiceMedical.Services.Interfaces;

namespace BookingCare.Services.ServiceMedical.Services.Implementations
{
    public class ServiceMedicalService : IServiceMedicalService
    {
        private readonly IServiceCategoryRepository _categoryRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ServiceMedicalService> _logger;
        private readonly IHospitalServiceClient _hospitalServiceClient;

        public ServiceMedicalService(
            IServiceCategoryRepository categoryRepository,
            IServiceRepository serviceRepository,
            IMapper mapper,
            ILogger<ServiceMedicalService> logger,
            IHospitalServiceClient hospitalServiceClient)
        {
            _categoryRepository = categoryRepository;
            _serviceRepository = serviceRepository;
            _mapper = mapper;
            _logger = logger;
            _hospitalServiceClient = hospitalServiceClient;
        }

        #region ServiceCategory Operations

        public async Task<ServiceCategoryResponse> CreateServiceCategoryAsync(CreateServiceCategoryRequest request)
        {
            try
            {
                // Validate parent if provided
                if (request.ParentId.HasValue)
                {
                    var isValidParent = await _categoryRepository.IsValidParentAsync(request.ParentId.Value, Guid.Empty);
                    if (!isValidParent)
                    {
                        throw new ArgumentException("Invalid parent category");
                    }
                }

                var entity = _mapper.Map<ServiceCategoryEntity>(request);
                var createdEntity = await _categoryRepository.CreateAsync(entity);

                return _mapper.Map<ServiceCategoryResponse>(createdEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service category: {Name}", request.Name);
                throw new InvalidOperationException($"Failed to create service category '{request.Name}'", ex);
            }
        }

        public async Task<ServiceCategoryResponse?> GetServiceCategoryByIdAsync(Guid id)
        {
            try
            {
                var entity = await _categoryRepository.GetByIdAsync(id);
                return entity == null ? null : _mapper.Map<ServiceCategoryResponse>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service category by id: {Id}", id);
                throw new InvalidOperationException($"Failed to retrieve service category with ID '{id}'", ex);
            }
        }

        public async Task<ServiceCategoryResponse> UpdateServiceCategoryAsync(UpdateServiceCategoryRequest request)
        {
            try
            {
                var existingEntity = await _categoryRepository.GetByIdAsync(request.Id);
                if (existingEntity == null)
                {
                    throw new ArgumentException($"Service category with ID {request.Id} not found");
                }

                // Validate parent if provided and different from current
                if (request.ParentId.HasValue && request.ParentId != existingEntity.ParentId)
                {
                    var isValidParent = await _categoryRepository.IsValidParentAsync(request.ParentId.Value, request.Id);
                    if (!isValidParent)
                    {
                        throw new ArgumentException("Invalid parent category");
                    }
                }

                _mapper.Map(request, existingEntity);
                var updatedEntity = await _categoryRepository.UpdateAsync(existingEntity);

                return _mapper.Map<ServiceCategoryResponse>(updatedEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service category: {Id}", request.Id);
                throw new InvalidOperationException($"Failed to update service category with ID '{request.Id}'", ex);
            }
        }

        public async Task<bool> DeleteServiceCategoryAsync(Guid id)
        {
            try
            {
                // Check if category has children
                var hasChildren = await _categoryRepository.HasChildrenAsync(id);
                if (hasChildren)
                {
                    throw new InvalidOperationException("Cannot delete category that has child categories");
                }

                return await _categoryRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service category: {Id}", id);
                throw new InvalidOperationException($"Failed to delete service category with ID '{id}'", ex);
            }
        }

        public async Task<ServiceCategoryListResponse> GetServiceCategoriesAsync(ServiceCategoryQueryRequest query)
        {
            try
            {
                var (categories, totalCount) = await _categoryRepository.GetPagedAsync(
                    query.Page, query.PageSize, query.SearchTerm, query.Status, query.ParentId);

                var response = new ServiceCategoryListResponse
                {
                    Categories = _mapper.Map<List<ServiceCategoryResponse>>(categories),
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service categories");
                throw new InvalidOperationException("Failed to retrieve service categories", ex);
            }
        }

        public async Task<List<ServiceCategoryResponse>> GetParentServiceCategoriesAsync()
        {
            try
            {
                var entities = await _categoryRepository.GetParentCategoriesAsync();
                return _mapper.Map<List<ServiceCategoryResponse>>(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parent service categories");
                throw new InvalidOperationException("Failed to retrieve parent service categories", ex);
            }
        }

        public async Task<List<ServiceCategoryResponse>> GetServiceCategoryChildrenAsync(GetServiceCategoryChildrenRequest request)
        {
            try
            {
                var entities = await _categoryRepository.GetChildrenAsync(request.ParentId);
                return _mapper.Map<List<ServiceCategoryResponse>>(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service category children for parent: {ParentId}", request.ParentId);
                throw new InvalidOperationException($"Failed to retrieve service category children for parent '{request.ParentId}'", ex);
            }
        }

        public async Task<List<ServiceCategoryResponse>> GetActiveServiceCategoriesAsync()
        {
            try
            {
                var entities = await _categoryRepository.GetActiveCategoriesAsync();
                return _mapper.Map<List<ServiceCategoryResponse>>(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active service categories");
                throw new InvalidOperationException("Failed to retrieve active service categories", ex);
            }
        }

        public async Task<List<ServiceCategoryResponse>> GetServiceCategoryHierarchyAsync(Guid categoryId)
        {
            try
            {
                var entities = await _categoryRepository.GetCategoryHierarchyAsync(categoryId);
                return _mapper.Map<List<ServiceCategoryResponse>>(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service category hierarchy for: {CategoryId}", categoryId);
                throw new InvalidOperationException($"Failed to retrieve service category hierarchy for '{categoryId}'", ex);
            }
        }

        #endregion

        #region Service Operations

        public async Task<ServiceResponse> CreateServiceAsync(CreateServiceRequest request)
        {
            try
            {
                // Validate service category if provided
                if (request.ServiceCategoryId.HasValue)
                {
                    var categoryExists = await _categoryRepository.ExistsAsync(request.ServiceCategoryId.Value);
                    if (!categoryExists)
                    {
                        throw new ArgumentException("Invalid service category");
                    }
                }

                var entity = _mapper.Map<ServiceEntity>(request);
                var createdEntity = await _serviceRepository.CreateAsync(entity);

                // Load related data for response
                var fullEntity = await _serviceRepository.GetByIdAsync(createdEntity.Id);
                return _mapper.Map<ServiceResponse>(fullEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service: {Name}", request.Name);
                throw new InvalidOperationException($"Failed to create service '{request.Name}'", ex);
            }
        }

        public async Task<ServiceResponse?> GetServiceByIdAsync(Guid id)
        {
            try
            {
                var entity = await _serviceRepository.GetByIdAsync(id);
                return entity == null ? null : _mapper.Map<ServiceResponse>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service by id: {Id}", id);
                throw new InvalidOperationException($"Failed to retrieve service with ID '{id}'", ex);
            }
        }

        public async Task<ServiceResponse> UpdateServiceAsync(UpdateServiceRequest request)
        {
            try
            {
                var existingEntity = await _serviceRepository.GetByIdAsync(request.Id);
                if (existingEntity == null)
                {
                    throw new ArgumentException($"Service with ID {request.Id} not found");
                }

                // Validate service category if provided and different from current
                if (request.ServiceCategoryId.HasValue && request.ServiceCategoryId != existingEntity.ServiceCategoryId)
                {
                    var categoryExists = await _categoryRepository.ExistsAsync(request.ServiceCategoryId.Value);
                    if (!categoryExists)
                    {
                        throw new ArgumentException("Invalid service category");
                    }
                }

                _mapper.Map(request, existingEntity);
                var updatedEntity = await _serviceRepository.UpdateAsync(existingEntity);

                // Load related data for response
                var fullEntity = await _serviceRepository.GetByIdAsync(updatedEntity.Id);
                return _mapper.Map<ServiceResponse>(fullEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service: {Id}", request.Id);
                throw new InvalidOperationException($"Failed to update service with ID '{request.Id}'", ex);
            }
        }

        public async Task<bool> DeleteServiceAsync(Guid id)
        {
            try
            {
                return await _serviceRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service: {Id}", id);
                throw new InvalidOperationException($"Failed to delete service with ID '{id}'", ex);
            }
        }

        public async Task<ServiceListResponse> GetServicesAsync(ServiceQueryRequest query)
        {
            try
            {
                var (services, totalCount) = await _serviceRepository.GetPagedAsync(query);

                var response = new ServiceListResponse
                {
                    Services = _mapper.Map<List<ServiceResponse>>(services),
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services");
                throw new InvalidOperationException("Failed to retrieve services", ex);
            }
        }

        public async Task<ServiceListResponse> GetServicesByCategoryAsync(GetServicesByCategoryRequest request)
        {
            try
            {
                var serviceQuery = new ServiceQueryRequest
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    ServiceCategoryId = request.ServiceCategoryId,
                    Status = request.IncludeInactive ? null : "ACTIVE"
                };
                
                var (services, totalCount) = await _serviceRepository.GetPagedAsync(serviceQuery);

                var response = new ServiceListResponse
                {
                    Services = _mapper.Map<List<ServiceResponse>>(services),
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by category: {CategoryId}", request.ServiceCategoryId);
                throw new InvalidOperationException($"Failed to retrieve services for category '{request.ServiceCategoryId}'", ex);
            }
        }

        public async Task<List<ServiceResponse>> GetServicesByHospitalAsync(Guid hospitalId)
        {
            try
            {
                var entities = await _serviceRepository.GetServicesByHospitalAsync(hospitalId);
                return _mapper.Map<List<ServiceResponse>>(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by hospital: {HospitalId}", hospitalId);
                throw new InvalidOperationException($"Failed to retrieve services for hospital '{hospitalId}'", ex);
            }
        }

        public async Task<List<ServiceResponse>> GetActiveServicesAsync()
        {
            try
            {
                var entities = await _serviceRepository.GetActiveServicesAsync();
                return _mapper.Map<List<ServiceResponse>>(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active services");
                throw new InvalidOperationException("Failed to retrieve active services", ex);
            }
        }

        // Luồng chính theo yêu cầu của bạn
        public async Task<HospitalsByServiceCategoryResponse> GetHospitalsByServiceCategoryAsync(GetHospitalsByServiceCategoryRequest request)
        {
            try
            {
                // Get category info
                var category = await _categoryRepository.GetByIdAsync(request.ServiceCategoryId);
                if (category == null)
                {
                    throw new ArgumentException($"Service category with ID {request.ServiceCategoryId} not found");
                }

                // Get hospital IDs that have services in this category
                var hospitalIds = await _serviceRepository.GetHospitalIdsByCategoryAsync(request.ServiceCategoryId);

                return new HospitalsByServiceCategoryResponse
                {
                    ServiceCategoryId = request.ServiceCategoryId,
                    ServiceCategoryName = category.Name,
                    HospitalIds = hospitalIds,
                    TotalHospitals = hospitalIds.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hospitals by service category: {CategoryId}", request.ServiceCategoryId);
                throw new InvalidOperationException($"Failed to retrieve hospitals for service category '{request.ServiceCategoryId}'", ex);
            }
        }

        // New method: Get services by category with hospital information
        public async Task<ServicesByCategoryWithHospitalResponse> GetServicesByCategoryWithHospitalAsync(GetServicesByCategoryRequest request)
        {
            try
            {
                // Get category info
                var category = await _categoryRepository.GetByIdAsync(request.ServiceCategoryId);
                if (category == null)
                {
                    throw new ArgumentException($"Service category with ID {request.ServiceCategoryId} not found");
                }

                // Get services by category
                var servicesResult = await GetServicesByCategoryAsync(request);

                // Extract unique hospital IDs from services
                var hospitalIds = servicesResult.Services
                    .Select(s => s.HospitalId)
                    .Distinct()
                    .ToList();

                // Get hospital information from Hospital Service
                var hospitals = await _hospitalServiceClient.GetHospitalsByIdsAsync(hospitalIds);
                var hospitalDict = hospitals.ToDictionary(h => h.Id, h => h);

                // Map services with hospital information
                var servicesWithHospital = servicesResult.Services.Select(service =>
                {
                    var serviceWithHospital = _mapper.Map<ServiceWithHospitalResponse>(service);
                    serviceWithHospital.Hospital = hospitalDict.TryGetValue(service.HospitalId, out var hospital) ? hospital : null;
                    return serviceWithHospital;
                }).ToList();

                return new ServicesByCategoryWithHospitalResponse
                {
                    ServiceCategoryId = request.ServiceCategoryId,
                    ServiceCategoryName = category.Name,
                    TotalServices = servicesResult.TotalCount,
                    Page = servicesResult.Page,
                    PageSize = servicesResult.PageSize,
                    TotalPages = servicesResult.TotalPages,
                    Services = servicesWithHospital
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by category with hospital info: {CategoryId}", request.ServiceCategoryId);
                throw new InvalidOperationException($"Failed to retrieve services with hospital information for category '{request.ServiceCategoryId}'", ex);
            }
        }

        #endregion


        #region Validation Operations

        public async Task<bool> ServiceCategoryExistsAsync(Guid id)
        {
            return await _categoryRepository.ExistsAsync(id);
        }

        public async Task<bool> ServiceExistsAsync(Guid id)
        {
            return await _serviceRepository.ExistsAsync(id);
        }

        #endregion
    }
}
