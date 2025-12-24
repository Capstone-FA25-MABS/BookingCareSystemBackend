using AutoMapper;
using BookingCare.Services.Hospital;
using BookingCare.Services.Review.Grpc;
using BookingCare.Services.ServiceMedical.Models.DTOs.Requests;
using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;
using BookingCare.Services.ServiceMedical.Models.Entities;
using BookingCare.Services.ServiceMedical.Repositories.Interfaces;
using BookingCare.Services.ServiceMedical.Services.Interfaces;
using BookingCare.Shared.Common.Interfaces;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILocationApiService _locationApiService;
        private readonly SubscriptionUsageGrpc.SubscriptionUsageGrpcClient _subscriptionUsageClient;
        private readonly ReviewService.ReviewServiceClient _reviewServiceClient;

        public sealed class ServiceMedicalGrpcClients
        {
            public ServiceMedicalGrpcClients(
                SubscriptionUsageGrpc.SubscriptionUsageGrpcClient subscriptionUsageClient,
                ReviewService.ReviewServiceClient reviewServiceClient
            )
            {
                SubscriptionUsageClient = subscriptionUsageClient;
                ReviewServiceClient = reviewServiceClient;
            }

            public SubscriptionUsageGrpc.SubscriptionUsageGrpcClient SubscriptionUsageClient { get; }
            public ReviewService.ReviewServiceClient ReviewServiceClient { get; }
        }

        public ServiceMedicalService(
            IServiceCategoryRepository categoryRepository,
            IServiceRepository serviceRepository,
            IMapper mapper,
            ILogger<ServiceMedicalService> logger,
            IHospitalService hospitalService,
            ILocationApiService locationApiService,
            ServiceMedicalGrpcClients grpcClients
        )
        {
            _categoryRepository = categoryRepository;
            _serviceRepository = serviceRepository;
            _mapper = mapper;
            _logger = logger;
            _hospitalService = hospitalService;
            _locationApiService = locationApiService;
            _subscriptionUsageClient = grpcClients.SubscriptionUsageClient;
            _reviewServiceClient = grpcClients.ReviewServiceClient;
        }

        #region ServiceCategory Operations

        public async Task<ServiceCategoryResponse> CreateServiceCategoryAsync(
            CreateServiceCategoryRequest request
        )
        {
            try
            {
                // Validate parent if provided
                if (request.ParentId.HasValue)
                {
                    var isValidParent = await _categoryRepository.IsValidParentAsync(
                        request.ParentId.Value,
                        Guid.Empty
                    );
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
                throw new InvalidOperationException(
                    $"Failed to create service category '{request.Name}'",
                    ex
                );
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
                throw new InvalidOperationException(
                    $"Failed to retrieve service category with ID '{id}'",
                    ex
                );
            }
        }

        public async Task<ServiceCategoryResponse> UpdateServiceCategoryAsync(
            UpdateServiceCategoryRequest request
        )
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
                    var isValidParent = await _categoryRepository.IsValidParentAsync(
                        request.ParentId.Value,
                        request.Id
                    );
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
                throw new InvalidOperationException(
                    $"Failed to update service category with ID '{request.Id}'",
                    ex
                );
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
                    throw new InvalidOperationException(
                        "Không thể xóa danh mục dịch vụ cha. Vui lòng xóa tất cả danh mục dịch vụ con trước."
                    );
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
                throw new InvalidOperationException(
                    $"Failed to delete service category with ID '{id}'",
                    ex
                );
            }
        }

        public async Task<ServiceCategoryListResponse> GetServiceCategoriesAsync(
            ServiceCategoryQueryRequest query
        )
        {
            try
            {
                var (categories, totalCount) = await _categoryRepository.GetPagedAsync(
                    query.Page,
                    query.PageSize,
                    query.SearchTerm,
                    query.Status,
                    query.ParentId,
                    query.SortBy,
                    query.SortDirection
                );

                var response = new ServiceCategoryListResponse
                {
                    Categories = _mapper.Map<List<ServiceCategoryResponse>>(categories),
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize),
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
                throw new InvalidOperationException(
                    "Failed to retrieve parent service categories",
                    ex
                );
            }
        }

        public async Task<List<ServiceCategoryResponse>> GetServiceCategoryChildrenAsync(
            GetServiceCategoryChildrenRequest request
        )
        {
            try
            {
                var entities = await _categoryRepository.GetChildrenAsync(request.ParentId);
                return _mapper.Map<List<ServiceCategoryResponse>>(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting service category children for parent: {ParentId}",
                    request.ParentId
                );
                throw new InvalidOperationException(
                    $"Failed to retrieve service category children for parent '{request.ParentId}'",
                    ex
                );
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
                throw new InvalidOperationException(
                    "Failed to retrieve active service categories",
                    ex
                );
            }
        }

        public async Task<List<ServiceCategoryResponse>> GetServiceCategoryHierarchyAsync(
            Guid categoryId
        )
        {
            try
            {
                var entities = await _categoryRepository.GetCategoryHierarchyAsync(categoryId);
                return _mapper.Map<List<ServiceCategoryResponse>>(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting service category hierarchy for: {CategoryId}",
                    categoryId
                );
                throw new InvalidOperationException(
                    $"Failed to retrieve service category hierarchy for '{categoryId}'",
                    ex
                );
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
                    var categoryExists = await _categoryRepository.ExistsAsync(
                        request.ServiceCategoryId.Value
                    );
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
                throw new InvalidOperationException(
                    $"Failed to create service '{request.Name}'",
                    ex
                );
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
                throw new InvalidOperationException(
                    $"Failed to retrieve service with ID '{id}'",
                    ex
                );
            }
        }

        public async Task<ServiceWithHospitalResponse?> GetServiceWithHospitalByIdAsync(Guid id)
        {
            try
            {
                var entity = await _serviceRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    return null;
                }

                // Map basic service info
                var serviceResponse = _mapper.Map<ServiceWithHospitalResponse>(entity);

                // Get hospital information
                var hospitals = await _hospitalService.GetHospitalsByIdsAsync(
                    new List<Guid> { entity.HospitalId }
                );
                if (hospitals.Any())
                {
                    serviceResponse.Hospital = _mapper.Map<HospitalInfoResponse>(hospitals[0]);
                }

                // Get service category information
                if (entity.ServiceCategoryId.HasValue)
                {
                    var category = await _categoryRepository.GetByIdAsync(
                        entity.ServiceCategoryId.Value
                    );
                    if (category != null)
                    {
                        serviceResponse.ServiceCategory = _mapper.Map<ServiceCategoryResponse>(
                            category
                        );
                    }
                }

                // Get review statistics from Review service via gRPC
                serviceResponse.ReviewStatistics = await GetServiceReviewStatisticsAsync(id);

                return serviceResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service with hospital by id: {Id}", id);
                throw new InvalidOperationException(
                    $"Failed to retrieve service with hospital information for ID '{id}'",
                    ex
                );
            }
        }

        /// <summary>
        /// Get review statistics for a service from Review service via gRPC
        /// </summary>
        private async Task<ServiceReviewStatisticsResponse?> GetServiceReviewStatisticsAsync(
            Guid serviceId
        )
        {
            try
            {
                var request = new GetServiceStatisticsRequest { ServiceId = serviceId.ToString() };

                var response = await _reviewServiceClient.GetServiceDetailedStatisticsAsync(
                    request
                );

                return new ServiceReviewStatisticsResponse
                {
                    AverageRating = response.AverageRating,
                    TotalReviews = response.TotalReviews,
                };
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.NotFound)
            {
                _logger.LogInformation(
                    ex,
                    "No review statistics found for service: {ServiceId}",
                    serviceId
                );
                return new ServiceReviewStatisticsResponse { AverageRating = 0, TotalReviews = 0 };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to get review statistics for service: {ServiceId}. Returning default values.",
                    serviceId
                );
                return new ServiceReviewStatisticsResponse { AverageRating = 0, TotalReviews = 0 };
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
                if (
                    request.ServiceCategoryId.HasValue
                    && request.ServiceCategoryId != existingEntity.ServiceCategoryId
                )
                {
                    var categoryExists = await _categoryRepository.ExistsAsync(
                        request.ServiceCategoryId.Value
                    );
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
                throw new InvalidOperationException(
                    $"Failed to update service with ID '{request.Id}'",
                    ex
                );
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
                    TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize),
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services");
                throw new InvalidOperationException("Failed to retrieve services", ex);
            }
        }

        public async Task<ServiceListResponse> GetServicesByCategoryAsync(
            GetServicesByCategoryRequest request
        )
        {
            try
            {
                // If location filtering is needed, we should get all services first (handled in GetServicesByCategoryWithHospitalAsync)
                // Otherwise, apply pagination at repository level
                var serviceQuery = new ServiceQueryRequest
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    ServiceCategoryId = request.ServiceCategoryId,
                    Status = request.IncludeInactive ? null : "ACTIVE",
                    SearchTerm = request.SearchTerm,
                };

                var (services, totalCount) = await _serviceRepository.GetPagedAsync(serviceQuery);

                // Apply HospitalIds filter if provided (client-side filtering as repository doesn't support multiple hospital IDs)
                List<ServiceResponse> filteredServices = _mapper.Map<List<ServiceResponse>>(
                    services
                );
                if (request.HospitalIds != null && request.HospitalIds.Any())
                {
                    filteredServices = filteredServices
                        .Where(s => request.HospitalIds.Contains(s.HospitalId))
                        .ToList();
                    totalCount = filteredServices.Count;
                }

                var response = new ServiceListResponse
                {
                    Services = filteredServices,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting services by category: {CategoryId}",
                    request.ServiceCategoryId
                );
                throw new InvalidOperationException(
                    $"Failed to retrieve services for category '{request.ServiceCategoryId}'",
                    ex
                );
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
                _logger.LogError(
                    ex,
                    "Error getting services by hospital: {HospitalId}",
                    hospitalId
                );
                throw new InvalidOperationException(
                    $"Failed to retrieve services for hospital '{hospitalId}'",
                    ex
                );
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
        public async Task<HospitalsByServiceCategoryResponse> GetHospitalsByServiceCategoryAsync(
            GetHospitalsByServiceCategoryRequest request
        )
        {
            try
            {
                // Get category info
                var category = await _categoryRepository.GetByIdAsync(request.ServiceCategoryId);
                if (category == null)
                {
                    throw new ArgumentException(
                        $"Service category with ID {request.ServiceCategoryId} not found"
                    );
                }

                // Get hospital IDs that have services in this category
                var hospitalIds = await _serviceRepository.GetHospitalIdsByCategoryAsync(
                    request.ServiceCategoryId
                );

                return new HospitalsByServiceCategoryResponse
                {
                    ServiceCategoryId = request.ServiceCategoryId,
                    ServiceCategoryName = category.Name,
                    HospitalIds = hospitalIds,
                    TotalHospitals = hospitalIds.Count,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting hospitals by service category: {CategoryId}",
                    request.ServiceCategoryId
                );
                throw new InvalidOperationException(
                    $"Failed to retrieve hospitals for service category '{request.ServiceCategoryId}'",
                    ex
                );
            }
        }

        // Get services by category with hospital information (optimized)
        public async Task<ServicesByCategoryOptimizedResponse> GetServicesByCategoryWithHospitalAsync(
            GetServicesByCategoryRequest request
        )
        {
            try
            {
                // Get category info with parent information
                var category = await _categoryRepository.GetByIdAsync(request.ServiceCategoryId);
                if (category == null)
                {
                    throw new ArgumentException(
                        $"Service category with ID {request.ServiceCategoryId} not found"
                    );
                }

                // Check if we need location filtering - if yes, get all services first, then filter, then paginate
                bool needsLocationFiltering =
                    !string.IsNullOrEmpty(request.ProvinceId)
                    || !string.IsNullOrEmpty(request.DistrictId);
                bool needsHospitalFiltering =
                    request.HospitalIds != null && request.HospitalIds.Any();

                ServiceListResponse servicesResult;
                if (needsLocationFiltering || needsHospitalFiltering)
                {
                    servicesResult = await GetFilteredServicesByCategoryAsync(
                        request,
                        needsLocationFiltering
                    );
                }
                else
                {
                    // No location/hospital filtering needed - use standard pagination
                    servicesResult = await GetServicesByCategoryAsync(request);
                }

                // Extract hospital IDs from paginated services for hospital info retrieval
                var hospitalIds = servicesResult
                    .Services.Select(s => s.HospitalId)
                    .Distinct()
                    .ToList();

                // Get hospital information for the paginated services
                var hospitals = await _hospitalService.GetHospitalsByIdsAsync(hospitalIds);
                var hospitalDict = hospitals.ToDictionary(h => h.Id, h => h);

                // Map services with optimized hospital information
                var servicesOptimized = servicesResult
                    .Services.Select(service =>
                    {
                        var serviceOptimized = _mapper.Map<ServiceOptimizedResponse>(service);

                        // Map hospital basic info
                        if (hospitalDict.TryGetValue(service.HospitalId, out var hospital))
                        {
                            serviceOptimized.Hospital =
                                _mapper.Map<ServiceMedicalHospitalBasicInfo>(hospital);
                        }

                        // Set parent category name for each service
                        serviceOptimized.ParentCategoryName = category.Parent?.Name;

                        return serviceOptimized;
                    })
                    .ToList();

                return new ServicesByCategoryOptimizedResponse
                {
                    ServiceCategoryId = request.ServiceCategoryId,
                    ServiceCategoryName = category.Name,
                    ServiceCategoryDescription = category.Description,
                    ParentCategoryName = category.Parent?.Name,
                    TotalServices = servicesResult.TotalCount,
                    Page = servicesResult.Page,
                    PageSize = servicesResult.PageSize,
                    TotalPages = servicesResult.TotalPages,
                    Services = servicesOptimized,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting services by category with hospital info: {CategoryId}",
                    request.ServiceCategoryId
                );
                throw new InvalidOperationException(
                    $"Failed to retrieve services with hospital information for category '{request.ServiceCategoryId}'",
                    ex
                );
            }
        }

        /// <summary>
        /// Get filtered services by category with location and hospital filtering
        /// </summary>
        private async Task<ServiceListResponse> GetFilteredServicesByCategoryAsync(
            GetServicesByCategoryRequest request,
            bool needsLocationFiltering
        )
        {
            // Get all services first (without pagination) to apply location/hospital filtering
            var allServicesResponse = await GetAllServicesForCategoryAsync(request);

            // Extract unique hospital IDs and get hospital information
            var allHospitalIds = allServicesResponse.Select(s => s.HospitalId).Distinct().ToList();
            var allHospitals = await _hospitalService.GetHospitalsByIdsAsync(allHospitalIds);

            // Apply filters to get filtered hospital IDs
            var filteredHospitalIds = await GetFilteredHospitalIdsAsync(
                allHospitals,
                allHospitalIds,
                request,
                needsLocationFiltering
            );

            // Filter services and apply pagination
            return ApplyPaginationToFilteredServices(
                allServicesResponse,
                filteredHospitalIds,
                request
            );
        }

        /// <summary>
        /// Get all services for a category without pagination
        /// </summary>
        private async Task<List<ServiceResponse>> GetAllServicesForCategoryAsync(
            GetServicesByCategoryRequest request
        )
        {
            var serviceQueryAll = new ServiceQueryRequest
            {
                Page = 1,
                PageSize = int.MaxValue,
                ServiceCategoryId = request.ServiceCategoryId,
                Status = request.IncludeInactive ? null : "ACTIVE",
                SearchTerm = request.SearchTerm,
            };

            var (allServices, _) = await _serviceRepository.GetPagedAsync(serviceQueryAll);
            return _mapper.Map<List<ServiceResponse>>(allServices);
        }

        /// <summary>
        /// Get filtered hospital IDs based on location and hospital filters
        /// </summary>
        private async Task<List<Guid>> GetFilteredHospitalIdsAsync(
            List<HospitalInfoResponse> allHospitals,
            List<Guid> allHospitalIds,
            GetServicesByCategoryRequest request,
            bool needsLocationFiltering
        )
        {
            List<Guid> filteredHospitalIds = allHospitalIds;

            if (needsLocationFiltering)
            {
                filteredHospitalIds = await ApplyLocationFilterAsync(
                    allHospitals,
                    allHospitalIds,
                    request.ProvinceId,
                    request.DistrictId
                );
            }

            if (request.HospitalIds != null && request.HospitalIds.Any())
            {
                filteredHospitalIds = filteredHospitalIds
                    .Where(id => request.HospitalIds.Contains(id))
                    .ToList();
                _logger.LogInformation(
                    "HospitalIds filtering result: {FilteredCount} hospitals",
                    filteredHospitalIds.Count
                );
            }

            return filteredHospitalIds;
        }

        /// <summary>
        /// Apply location filtering to hospitals
        /// </summary>
        private async Task<List<Guid>> ApplyLocationFilterAsync(
            List<HospitalInfoResponse> allHospitals,
            List<Guid> allHospitalIds,
            string? provinceId,
            string? districtId
        )
        {
            _logger.LogInformation(
                "Applying location filtering - ProvinceId: {ProvinceId}, DistrictId: {DistrictId}",
                provinceId,
                districtId
            );

            var locationFilteredHospitals = await FilterHospitalsByLocationAsync(
                allHospitals,
                provinceId,
                districtId
            );

            var filteredIds = locationFilteredHospitals.Select(h => h.Id).ToList();
            _logger.LogInformation(
                "Location filtering result: {FilteredCount} out of {TotalCount} hospitals",
                filteredIds.Count,
                allHospitalIds.Count
            );

            return filteredIds;
        }

        /// <summary>
        /// Apply pagination to filtered services
        /// </summary>
        private ServiceListResponse ApplyPaginationToFilteredServices(
            List<ServiceResponse> allServices,
            List<Guid> filteredHospitalIds,
            GetServicesByCategoryRequest request
        )
        {
            var filteredServices = allServices
                .Where(s => filteredHospitalIds.Contains(s.HospitalId))
                .ToList();

            var totalCount = filteredServices.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            var paginatedServices = filteredServices
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new ServiceListResponse
            {
                Services = paginatedServices,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
            };
        }

        /// <summary>
        /// Filter hospitals by location (province and/or district)
        /// </summary>
        private async Task<List<HospitalInfoResponse>> FilterHospitalsByLocationAsync(
            List<HospitalInfoResponse> hospitals,
            string? provinceId,
            string? districtId
        )
        {
            if (string.IsNullOrEmpty(provinceId) && string.IsNullOrEmpty(districtId))
            {
                return hospitals;
            }

            try
            {
                var (provinceName, districtName) = await GetLocationNamesAsync(
                    provinceId,
                    districtId
                );
                if (string.IsNullOrEmpty(provinceName) && !string.IsNullOrEmpty(provinceId))
                {
                    return hospitals; // Return all if cannot get province name
                }

                return FilterHospitalsByLocationNames(hospitals, provinceName, districtName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error filtering hospitals by location. Returning all hospitals."
                );
                return hospitals; // Return all if error occurs
            }
        }

        /// <summary>
        /// Get province and district names from their IDs
        /// </summary>
        private async Task<(string? provinceName, string? districtName)> GetLocationNamesAsync(
            string? provinceId,
            string? districtId
        )
        {
            string? provinceName = null;
            string? districtName = null;

            if (!string.IsNullOrEmpty(provinceId))
            {
                provinceName = await _locationApiService.GetProvinceNameByIdAsync(provinceId);
                if (string.IsNullOrEmpty(provinceName))
                {
                    _logger.LogWarning("Province name not found for ID: {ProvinceId}", provinceId);
                }
            }

            if (!string.IsNullOrEmpty(districtId))
            {
                districtName = await _locationApiService.GetDistrictNameByIdAsync(districtId);
                if (string.IsNullOrEmpty(districtName))
                {
                    _logger.LogWarning("District name not found for ID: {DistrictId}", districtId);
                }
            }

            return (provinceName, districtName);
        }

        /// <summary>
        /// Filter hospitals by location names
        /// </summary>
        private List<HospitalInfoResponse> FilterHospitalsByLocationNames(
            List<HospitalInfoResponse> hospitals,
            string? provinceName,
            string? districtName
        )
        {
            var filteredHospitals = hospitals
                .Where(hospital =>
                {
                    if (string.IsNullOrEmpty(hospital.Address))
                    {
                        return false;
                    }

                    var hospitalAddress = hospital.Address.ToLowerInvariant();
                    var cleanProvinceName = CleanLocationName(provinceName, true);
                    var cleanDistrictName = CleanLocationName(districtName, false);

                    if (!string.IsNullOrEmpty(districtName))
                    {
                        return MatchesLocation(
                            hospitalAddress,
                            provinceName,
                            cleanProvinceName,
                            districtName,
                            cleanDistrictName
                        );
                    }

                    if (!string.IsNullOrEmpty(provinceName))
                    {
                        return hospitalAddress.Contains(provinceName.ToLowerInvariant())
                            || hospitalAddress.Contains(cleanProvinceName);
                    }

                    return false;
                })
                .ToList();

            _logger.LogInformation(
                "Location filtering completed. {FilteredCount} out of {TotalCount} hospitals match the criteria",
                filteredHospitals.Count,
                hospitals.Count
            );

            return filteredHospitals;
        }

        /// <summary>
        /// Clean location name by removing common prefixes
        /// </summary>
        private static string CleanLocationName(string? locationName, bool isProvince)
        {
            if (string.IsNullOrEmpty(locationName))
            {
                return string.Empty;
            }

            var cleaned = locationName.ToLowerInvariant();
            if (isProvince)
            {
                cleaned = cleaned
                    .Replace("thành phố", "")
                    .Replace("tỉnh", "")
                    .Replace("tp.", "")
                    .Replace("tp ", "")
                    .Trim();
            }
            else
            {
                cleaned = cleaned
                    .Replace("quận", "")
                    .Replace("huyện", "")
                    .Replace("thị xã", "")
                    .Replace("thành phố", "")
                    .Replace("tx.", "")
                    .Replace("q.", "")
                    .Replace("h.", "")
                    .Trim();
            }

            return cleaned;
        }

        /// <summary>
        /// Check if hospital address matches location criteria
        /// </summary>
        private static bool MatchesLocation(
            string hospitalAddress,
            string? provinceName,
            string cleanProvinceName,
            string districtName,
            string cleanDistrictName
        )
        {
            var provinceNameLower = provinceName?.ToLowerInvariant() ?? "";
            var districtNameLower = districtName.ToLowerInvariant();

            var provinceMatch =
                string.IsNullOrEmpty(provinceName)
                || hospitalAddress.Contains(provinceNameLower)
                || hospitalAddress.Contains(cleanProvinceName);

            var districtMatch =
                hospitalAddress.Contains(districtNameLower)
                || hospitalAddress.Contains(cleanDistrictName);

            return provinceMatch && districtMatch;
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
                    .Select(h => new SimpleItemResponse { Id = h.Id, Name = h.Name })
                    .OrderBy(h => h.Name)
                    .ToList();

                // Get all child service categories (categories with ParentId != null and status = ACTIVE)
                var allCategories = await _categoryRepository.GetActiveCategoriesAsync();
                var childCategories = allCategories
                    .Where(c => c.ParentId != null)
                    .Select(c => new SimpleItemResponse { Id = c.Id, Name = c.Name })
                    .OrderBy(c => c.Name)
                    .ToList();

                return new FilterOptionsResponse
                {
                    Hospitals = hospitalList,
                    ServiceCategories = childCategories,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filter options");
                throw new InvalidOperationException("Failed to retrieve filter options", ex);
            }
        }

        // Get all services with hospital name and category name (with filtering and sorting)
        public async Task<ServiceDetailListResponse> GetAllServicesWithDetailsAsync(
            ServiceQueryRequest? query = null
        )
        {
            try
            {
                // If no query provided, get all services
                if (query == null)
                {
                    query = new ServiceQueryRequest
                    {
                        Page = 1,
                        PageSize =
                            int.MaxValue // Get all if no pagination specified
                        ,
                    };
                }

                // Use GetPagedAsync for filtering and sorting (except HospitalName)
                var (services, totalCount) = await _serviceRepository.GetPagedAsync(query);

                // Extract unique hospital IDs from services
                var hospitalIds = services.Select(s => s.HospitalId).Distinct().ToList();

                // Get hospital information from Hospital Service via gRPC
                var hospitals = await _hospitalService.GetHospitalsByIdsAsync(hospitalIds);
                var hospitalDict = hospitals.ToDictionary(h => h.Id, h => h);

                // Map services with hospital name and category name
                var serviceDetails = services
                    .Select(service =>
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
                            HospitalName = hospitalDict.TryGetValue(
                                service.HospitalId,
                                out var hospital
                            )
                                ? hospital.Name
                                : null,
                        };

                        return serviceDetail;
                    })
                    .ToList();

                return new ServiceDetailListResponse
                {
                    Services = serviceDetails,
                    TotalCount = totalCount,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all services with details");
                throw new InvalidOperationException(
                    "Failed to retrieve all services with details",
                    ex
                );
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
                var request = new CheckLimitRequest { HospitalId = hospitalId.ToString() };

                var response = await _subscriptionUsageClient.CheckServiceLimitAsync(request);

                if (!response.CanAdd)
                {
                    throw new InvalidOperationException(
                        response.Message
                            ?? "Bạn đã đạt giới hạn số lượng dịch vụ cho phép trong gói đăng ký. Vui lòng nâng cấp gói để thêm dịch vụ."
                    );
                }
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy gói đăng ký cho bệnh viện này."
                );
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.FailedPrecondition)
            {
                throw new InvalidOperationException(
                    ex.Status.Detail ?? "Không thể thêm dịch vụ do giới hạn gói đăng ký."
                );
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error checking service limit via gRPC for hospital {HospitalId}",
                    hospitalId
                );
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
                var request = new IncrementRequest { HospitalId = hospitalId.ToString() };

                await _subscriptionUsageClient.IncrementServiceCountAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error incrementing service count via gRPC for hospital {HospitalId}",
                    hospitalId
                );
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
                var request = new IncrementRequest { HospitalId = hospitalId.ToString() };

                await _subscriptionUsageClient.DecrementServiceCountAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error decrementing service count via gRPC for hospital {HospitalId}",
                    hospitalId
                );
                // Don't throw - service already deleted, just log the error
            }
        }

        #endregion

        #region gRPC Optimized Operations

        /// <summary>
        /// Get basic info for multiple services by IDs (batch operation for gRPC performance)
        /// Uses projection at repository level for optimal database query
        /// </summary>
        public async Task<List<ServiceBasicInfoDto>> GetServicesBasicInfoByIdsAsync(
            IEnumerable<Guid> ids
        )
        {
            try
            {
                var idList = ids.ToList();
                if (!idList.Any())
                {
                    return new List<ServiceBasicInfoDto>();
                }

                // Repository uses projection to only SELECT required columns
                return await _serviceRepository.GetServicesBasicInfoByIdsAsync(idList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services basic info by IDs");
                throw new InvalidOperationException("Failed to get services basic info by IDs", ex);
            }
        }

        #endregion

        #region Performance Optimization Operations

        /// <summary>
        /// Get only service IDs by hospital (optimized for performance)
        /// </summary>
        public async Task<List<Guid>> GetServiceIdsByHospitalAsync(Guid hospitalId)
        {
            try
            {
                _logger.LogInformation("Getting service IDs for hospital {HospitalId}", hospitalId);

                var serviceIds = await _serviceRepository
                    .GetQueryable()
                    .Where(s => s.HospitalId == hospitalId)
                    .Select(s => s.Id)
                    .ToListAsync();

                _logger.LogInformation(
                    "Found {Count} services for hospital {HospitalId}",
                    serviceIds.Count,
                    hospitalId
                );

                return serviceIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting service IDs for hospital {HospitalId}",
                    hospitalId
                );
                throw new InvalidOperationException(
                    $"Failed to get service IDs for hospital {hospitalId}",
                    ex
                );
            }
        }

        /// <summary>
        /// Get all service IDs (optimized for Admin Dashboard - returns only IDs)
        /// </summary>
        public async Task<List<Guid>> GetAllServiceIdsAsync()
        {
            try
            {
                _logger.LogInformation("Getting all service IDs");

                var serviceIds = await _serviceRepository
                    .GetQueryable()
                    .Select(s => s.Id)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} services in total", serviceIds.Count);

                return serviceIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all service IDs");
                throw new InvalidOperationException("Failed to get all service IDs", ex);
            }
        }

        #endregion
    }
}
