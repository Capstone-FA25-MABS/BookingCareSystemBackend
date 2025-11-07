using BookingCare.Services.ServiceMedical.Constants;
using BookingCare.Services.ServiceMedical.Models.DTOs.Requests;
using BookingCare.Services.ServiceMedical.Protos;
using BookingCare.Services.ServiceMedical.Services.Interfaces;
using Grpc.Core;

namespace BookingCare.Services.ServiceMedical.Services.Grpc
{
    public class ServiceMedicalGrpcService : Protos.ServiceMedicalService.ServiceMedicalServiceBase
    {
        private readonly IServiceMedicalService _serviceMedicalService;
        private readonly ILogger<ServiceMedicalGrpcService> _logger;

        public ServiceMedicalGrpcService(IServiceMedicalService serviceMedicalService, ILogger<ServiceMedicalGrpcService> logger)
        {
            _serviceMedicalService = serviceMedicalService;
            _logger = logger;
        }

        public override async Task<ServiceCategoriesResponse> GetParentServiceCategories(
            GetParentServiceCategoriesRequest request, ServerCallContext context)
        {
            try
            {
                var categories = await _serviceMedicalService.GetParentServiceCategoriesAsync();

                var response = new ServiceCategoriesResponse();
                foreach (var category in categories)
                {
                    response.Categories.Add(new ServiceCategoryResponse
                    {
                        Id = category.Id.ToString(),
                        Name = category.Name,
                        Description = category.Description ?? string.Empty,
                        ImageUrl = category.ImageUrl ?? string.Empty,
                        ParentId = category.ParentId?.ToString() ?? string.Empty,
                        Status = category.Status
                    });
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parent service categories via gRPC");
                throw new RpcException(new Status(StatusCode.Internal, StatusConstants.InternalServerError));
            }
        }

        public override async Task<ServiceCategoriesResponse> GetServiceCategoryChildren(
            GetServiceCategoryChildrenGrpcRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.ParentId, out var parentId))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid parent ID"));
                }

                var requestDto = new Models.DTOs.Requests.GetServiceCategoryChildrenRequest
                {
                    ParentId = parentId,
                    IncludeInactive = request.IncludeInactive
                };

                var categories = await _serviceMedicalService.GetServiceCategoryChildrenAsync(requestDto);

                var response = new ServiceCategoriesResponse();
                foreach (var category in categories)
                {
                    response.Categories.Add(new ServiceCategoryResponse
                    {
                        Id = category.Id.ToString(),
                        Name = category.Name,
                        Description = category.Description ?? string.Empty,
                        ImageUrl = category.ImageUrl ?? string.Empty,
                        ParentId = category.ParentId?.ToString() ?? string.Empty,
                        Status = category.Status
                    });
                }

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service category children via gRPC for parent: {ParentId}", request.ParentId);
                throw new RpcException(new Status(StatusCode.Internal, StatusConstants.InternalServerError));
            }
        }

        public override async Task<HospitalsByServiceCategoryResponse> GetHospitalsByServiceCategory(
            GetHospitalsByServiceCategoryGrpcRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.ServiceCategoryId, out var categoryId))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid service category ID"));
                }

                var requestDto = new Models.DTOs.Requests.GetHospitalsByServiceCategoryRequest
                {
                    ServiceCategoryId = categoryId,
                    IncludeInactive = request.IncludeInactive
                };

                var result = await _serviceMedicalService.GetHospitalsByServiceCategoryAsync(requestDto);

                var response = new HospitalsByServiceCategoryResponse
                {
                    ServiceCategoryId = result.ServiceCategoryId.ToString(),
                    ServiceCategoryName = result.ServiceCategoryName,
                    TotalHospitals = result.TotalHospitals
                };

                foreach (var hospitalId in result.HospitalIds)
                {
                    response.HospitalIds.Add(hospitalId.ToString());
                }

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (ArgumentException ex)
            {
                throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hospitals by service category via gRPC for category: {CategoryId}", request.ServiceCategoryId);
                throw new RpcException(new Status(StatusCode.Internal, StatusConstants.InternalServerError));
            }
        }

        public override async Task<Protos.ServiceResponse> GetService(
            GetServiceGrpcRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var serviceId))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid service ID"));
                }

                var service = await _serviceMedicalService.GetServiceByIdAsync(serviceId);
                if (service == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Service not found"));
                }

                var response = new Protos.ServiceResponse
                {
                    Id = service.Id.ToString(),
                    Name = service.Name,
                    Description = service.Description ?? string.Empty,
                    Price = service.Price.ToString("F2"),
                    ImageUrl = service.ImageUrl ?? string.Empty,
                    HospitalId = service.HospitalId.ToString(),
                    ServiceCategoryId = service.ServiceCategoryId?.ToString() ?? string.Empty,
                    DurationTime = service.DurationTime,
                    Status = service.Status
                };

                if (service.ServiceCategory != null)
                {
                    response.ServiceCategory = new ServiceCategoryResponse
                    {
                        Id = service.ServiceCategory.Id.ToString(),
                        Name = service.ServiceCategory.Name,
                        Description = service.ServiceCategory.Description ?? string.Empty,
                        ImageUrl = service.ServiceCategory.ImageUrl ?? string.Empty,
                        ParentId = service.ServiceCategory.ParentId?.ToString() ?? string.Empty,
                        Status = service.ServiceCategory.Status
                    };
                }

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service via gRPC: {ServiceId}", request.Id);
                throw new RpcException(new Status(StatusCode.Internal, StatusConstants.InternalServerError));
            }
        }

        public override async Task<ServicesResponse> GetServicesByCategory(
            GetServicesByCategoryGrpcRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.ServiceCategoryId, out var categoryId))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid service category ID"));
                }

                var requestDto = new Models.DTOs.Requests.GetServicesByCategoryRequest
                {
                    ServiceCategoryId = categoryId,
                    Page = request.Page > 0 ? request.Page : 1,
                    PageSize = request.PageSize > 0 ? request.PageSize : 10,
                    IncludeInactive = request.IncludeInactive
                };

                var result = await _serviceMedicalService.GetServicesByCategoryAsync(requestDto);

                var response = new ServicesResponse
                {
                    TotalCount = result.TotalCount,
                    Page = result.Page,
                    PageSize = result.PageSize,
                    TotalPages = result.TotalPages
                };

                foreach (var service in result.Services)
                {
                    var serviceResponse = new Protos.ServiceResponse
                    {
                        Id = service.Id.ToString(),
                        Name = service.Name,
                        Description = service.Description ?? string.Empty,
                        Price = service.Price.ToString("F2"),
                        ImageUrl = service.ImageUrl ?? string.Empty,
                        HospitalId = service.HospitalId.ToString(),
                        ServiceCategoryId = service.ServiceCategoryId?.ToString() ?? string.Empty,
                        DurationTime = service.DurationTime,
                        Status = service.Status
                    };

                    if (service.ServiceCategory != null)
                    {
                        serviceResponse.ServiceCategory = new ServiceCategoryResponse
                        {
                            Id = service.ServiceCategory.Id.ToString(),
                            Name = service.ServiceCategory.Name,
                            Description = service.ServiceCategory.Description ?? string.Empty,
                            ImageUrl = service.ServiceCategory.ImageUrl ?? string.Empty,
                            ParentId = service.ServiceCategory.ParentId?.ToString() ?? string.Empty,
                            Status = service.ServiceCategory.Status
                        };
                    }

                    response.Services.Add(serviceResponse);
                }

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by category via gRPC for category: {CategoryId}", request.ServiceCategoryId);
                throw new RpcException(new Status(StatusCode.Internal, StatusConstants.InternalServerError));
            }
        }

        public override async Task<ServicesResponse> GetServicesByHospital(
            GetServicesByHospitalGrpcRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.HospitalId, out var hospitalId))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid hospital ID"));
                }

                _logger.LogInformation("[ServiceMedicalGrpcService] GetServicesByHospital called for hospital: {HospitalId}", hospitalId);

                var services = await _serviceMedicalService.GetServicesByHospitalAsync(hospitalId);

                var response = new ServicesResponse
                {
                    TotalCount = services.Count,
                    Page = 1,
                    PageSize = services.Count,
                    TotalPages = 1
                };

                foreach (var service in services)
                {
                    var serviceResponse = new Protos.ServiceResponse
                    {
                        Id = service.Id.ToString(),
                        Name = service.Name,
                        Description = service.Description ?? string.Empty,
                        Price = service.Price.ToString("F2"),
                        ImageUrl = service.ImageUrl ?? string.Empty,
                        HospitalId = service.HospitalId.ToString(),
                        ServiceCategoryId = service.ServiceCategoryId?.ToString() ?? string.Empty,
                        DurationTime = service.DurationTime,
                        Status = service.Status
                    };

                    if (service.ServiceCategory != null)
                    {
                        serviceResponse.ServiceCategory = new ServiceCategoryResponse
                        {
                            Id = service.ServiceCategory.Id.ToString(),
                            Name = service.ServiceCategory.Name,
                            Description = service.ServiceCategory.Description ?? string.Empty,
                            ImageUrl = service.ServiceCategory.ImageUrl ?? string.Empty,
                            ParentId = service.ServiceCategory.ParentId?.ToString() ?? string.Empty,
                            Status = service.ServiceCategory.Status
                        };
                    }

                    response.Services.Add(serviceResponse);
                }

                _logger.LogInformation("[ServiceMedicalGrpcService] Returning {Count} services for hospital {HospitalId}", services.Count, hospitalId);
                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by hospital via gRPC for hospital: {HospitalId}", request.HospitalId);
                throw new RpcException(new Status(StatusCode.Internal, StatusConstants.InternalServerError));
            }
        }

        public override async Task<ValidateServiceMedicalResponse> ValidateServiceMedical(
            ValidateServiceMedicalRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var serviceId))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid service ID"));
                }

                var service = await _serviceMedicalService.GetServiceByIdAsync(serviceId);

                var response = new ValidateServiceMedicalResponse
                {
                    ServiceId = serviceId.ToString(),
                    IsValid = service != null,
                    IsActive = service != null && service.Status == "ACTIVE",
                    ServiceName = service?.Name ?? string.Empty
                };

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating service medical via gRPC: {ServiceId}", request.Id);
                throw new RpcException(new Status(StatusCode.Internal, StatusConstants.InternalServerError));
            }
        }
    }
}
