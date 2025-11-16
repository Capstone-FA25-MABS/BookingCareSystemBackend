using AutoMapper;
using BookingCare.Services.ServiceMedical.Models.DTOs.Requests;
using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;
using BookingCare.Services.ServiceMedical.Models.Entities;
using BookingCare.Services.ServiceMedical.Repositories.Interfaces;
using BookingCare.Services.ServiceMedical.Services.Interfaces;
using BookingCare.Services.Hospital;
using Grpc.Core;
using GrpcStatusCode = Grpc.Core.StatusCode;
using ServiceMedicalHospitalBasicInfo = BookingCare.Services.ServiceMedical.Models.DTOs.Responses.HospitalBasicInfo;

namespace BookingCare.Services.ServiceMedical.Services.Implementations
{
    public class ServiceMedicalService : IServiceMedicalService
    {
        private readonly IServiceCategoryRepository _categoryRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ServiceMedicalService> _logger;
        private readonly IHospitalService _hospitalService;
        private readonly SubscriptionUsageGrpc.SubscriptionUsageGrpcClient _subscriptionUsageClient;

        public ServiceMedicalService(
            IServiceCategoryRepository categoryRepository,
            IServiceRepository serviceRepository,
            IMapper mapper,
            ILogger<ServiceMedicalService> logger,
            IHospitalService hospitalService,
            SubscriptionUsageGrpc.SubscriptionUsageGrpcClient subscriptionUsageClient)
        {
            _categoryRepository = categoryRepository;
            _serviceRepository = serviceRepository;
            _mapper = mapper;
            _logger = logger;
            _hospitalService = hospitalService;
            _subscriptionUsageClient = subscriptionUsageClient;
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
                // Check if category is a parent (has children) - cannot delete parent if it has any children
                var hasChildren = await _categoryRepository.HasChildrenAsync(id);
                if (hasChildren)
                {
                    throw new InvalidOperationException("Không thể xóa danh mục dịch vụ cha. Vui lòng xóa tất cả danh mục dịch vụ con trước.");
                }

                return await _categoryRepository.DeleteAsync(id);
            }
            catch (InvalidOperationException)
            {
                // Re-throw InvalidOperationException as-is (business rule violation)
                throw;
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
                    query.Page, query.PageSize, query.SearchTerm, query.Status, query.ParentId,
                    query.SortBy, query.SortDirection);

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

                // Check subscription limit before creating service
                await CheckAndValidateServiceLimitAsync(request.HospitalId);

                var entity = _mapper.Map<ServiceEntity>(request);
                var createdEntity = await _serviceRepository.CreateAsync(entity);

                // Increment service count after successful creation
                await IncrementServiceCountAsync(request.HospitalId);

                // Load related data for response
                var fullEntity = await _serviceRepository.GetByIdAsync(createdEntity.Id);
                return _mapper.Map<ServiceResponse>(fullEntity);
            }
            catch (InvalidOperationException)
            {
                throw;
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
                // Get service to find hospital ID before deletion
                var service = await _serviceRepository.GetByIdAsync(id);
                if (service == null)
                {
                    return false;
                }

                var result = await _serviceRepository.DeleteAsync(id);

                // Decrement service count after successful deletion
                if (result)
                {
                    await DecrementServiceCountAsync(service.HospitalId);
                }

                return result;
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

        // Get services by category with hospital information (optimized)
        public async Task<ServicesByCategoryOptimizedResponse> GetServicesByCategoryWithHospitalAsync(GetServicesByCategoryRequest request)
        {
            try
            {
                // Get category info with parent information
                var category = await _categoryRepository.GetByIdAsync(request.ServiceCategoryId);
                if (category == null)
                {
                    throw new ArgumentException($"Service category with ID {request.ServiceCategoryId} not found");
                }

                // Get services by category using optimized query
                var servicesResult = await GetServicesByCategoryAsync(request);

                // Extract unique hospital IDs from services
                var hospitalIds = servicesResult.Services
                    .Select(s => s.HospitalId)
                    .Distinct()
                    .ToList();

                // Get hospital information from Hospital Service via gRPC
                var hospitals = await _hospitalService.GetHospitalsByIdsAsync(hospitalIds);
                var hospitalDict = hospitals.ToDictionary(h => h.Id, h => h);

                // Map services with optimized hospital information
                var servicesOptimized = servicesResult.Services.Select(service =>
                {
                    var serviceOptimized = _mapper.Map<ServiceOptimizedResponse>(service);

                    // Map hospital basic info
                    if (hospitalDict.TryGetValue(service.HospitalId, out var hospital))
                    {
                        serviceOptimized.Hospital = _mapper.Map<ServiceMedicalHospitalBasicInfo>(hospital);
                    }

                    // Set parent category name for each service
                    serviceOptimized.ParentCategoryName = category.Parent?.Name;

                    return serviceOptimized;
                }).ToList();

                return new ServicesByCategoryOptimizedResponse
                {
                    ServiceCategoryId = request.ServiceCategoryId,
                    ServiceCategoryName = category.Name,
                    ServiceCategoryDescription = category.Description, // Add service category description
                    ParentCategoryName = category.Parent?.Name, // Add parent category name
                    TotalServices = servicesResult.TotalCount,
                    Page = servicesResult.Page,
                    PageSize = servicesResult.PageSize,
                    TotalPages = servicesResult.TotalPages,
                    Services = servicesOptimized
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by category with hospital info: {CategoryId}", request.ServiceCategoryId);
                throw new InvalidOperationException($"Failed to retrieve services with hospital information for category '{request.ServiceCategoryId}'", ex);
            }
        }

        // Get filter options (hospitals and service categories) for dropdown
        public async Task<FilterOptionsResponse> GetFilterOptionsAsync()
        {
            try
            {
                // Get all distinct hospital IDs from services
                var hospitalIds = await _serviceRepository.GetAllDistinctHospitalIdsAsync();

                // Get hospital information from Hospital Service via gRPC
                var hospitals = await _hospitalService.GetHospitalsByIdsAsync(hospitalIds);
                var hospitalList = hospitals
                    .Select(h => new SimpleItemResponse
                    {
                        Id = h.Id,
                        Name = h.Name
                    })
                    .OrderBy(h => h.Name)
                    .ToList();

                // Get all child service categories (categories with ParentId != null and status = ACTIVE)
                var allCategories = await _categoryRepository.GetActiveCategoriesAsync();
                var childCategories = allCategories
                    .Where(c => c.ParentId != null)
                    .Select(c => new SimpleItemResponse
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                    .OrderBy(c => c.Name)
                    .ToList();

                return new FilterOptionsResponse
                {
                    Hospitals = hospitalList,
                    ServiceCategories = childCategories
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filter options");
                throw new InvalidOperationException("Failed to retrieve filter options", ex);
            }
        }

        // Get all services with hospital name and category name (with filtering and sorting)
        public async Task<ServiceDetailListResponse> GetAllServicesWithDetailsAsync(ServiceQueryRequest? query = null)
        {
            try
            {
                // If no query provided, get all services
                if (query == null)
                {
                    query = new ServiceQueryRequest
                    {
                        Page = 1,
                        PageSize = int.MaxValue // Get all if no pagination specified
                    };
                }

                // Use GetPagedAsync for filtering and sorting (except HospitalName)
                var (services, totalCount) = await _serviceRepository.GetPagedAsync(query);

                // Extract unique hospital IDs from services
                var hospitalIds = services
                    .Select(s => s.HospitalId)
                    .Distinct()
                    .ToList();

                // Get hospital information from Hospital Service via gRPC
                var hospitals = await _hospitalService.GetHospitalsByIdsAsync(hospitalIds);
                var hospitalDict = hospitals.ToDictionary(h => h.Id, h => h);

                // Map services with hospital name and category name
                var serviceDetails = services.Select(service =>
                {
                    var serviceDetail = new ServiceDetailResponse
                    {
                        Id = service.Id,
                        Name = service.Name,
                        Description = service.Description,
                        Price = service.Price,
                        Duration = service.DurationTime,
                        Status = service.Status,
                        ServiceCategoryName = service.ServiceCategory?.Name,
                        HospitalName = hospitalDict.TryGetValue(service.HospitalId, out var hospital) ? hospital.Name : null
                    };

                    return serviceDetail;
                }).ToList();

                return new ServiceDetailListResponse
                {
                    Services = serviceDetails,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all services with details");
                throw new InvalidOperationException("Failed to retrieve all services with details", ex);
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

        #region Private Helper Methods

        /// <summary>
        /// Check and validate service limit via gRPC
        /// </summary>
        private async Task CheckAndValidateServiceLimitAsync(Guid hospitalId)
        {
            try
            {
                var request = new CheckLimitRequest
                {
                    HospitalId = hospitalId.ToString()
                };

                var response = await _subscriptionUsageClient.CheckServiceLimitAsync(request);

                if (!response.CanAdd)
                {
                    throw new InvalidOperationException(
                        response.Message ?? "Bạn đã đạt giới hạn số lượng dịch vụ cho phép trong gói đăng ký. Vui lòng nâng cấp gói để thêm dịch vụ."
                    );
                }
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.NotFound)
            {
                throw new InvalidOperationException("Không tìm thấy gói đăng ký cho bệnh viện này.");
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.FailedPrecondition)
            {
                throw new InvalidOperationException(ex.Status.Detail ?? "Không thể thêm dịch vụ do giới hạn gói đăng ký.");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking service limit via gRPC for hospital {HospitalId}", hospitalId);
                // Don't block creation if subscription service is unavailable, but log the warning
            }
        }

        /// <summary>
        /// Increment service count via gRPC
        /// </summary>
        private async Task IncrementServiceCountAsync(Guid hospitalId)
        {
            try
            {
                var request = new IncrementRequest
                {
                    HospitalId = hospitalId.ToString()
                };

                await _subscriptionUsageClient.IncrementServiceCountAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error incrementing service count via gRPC for hospital {HospitalId}", hospitalId);
                // Don't throw - service already created, just log the error
            }
        }

        /// <summary>
        /// Decrement service count via gRPC
        /// </summary>
        private async Task DecrementServiceCountAsync(Guid hospitalId)
        {
            try
            {
                var request = new IncrementRequest
                {
                    HospitalId = hospitalId.ToString()
                };

                await _subscriptionUsageClient.DecrementServiceCountAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error decrementing service count via gRPC for hospital {HospitalId}", hospitalId);
                // Don't throw - service already deleted, just log the error
            }
        }

        #endregion
    }
}
