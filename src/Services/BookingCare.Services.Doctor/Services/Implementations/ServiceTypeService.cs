using AutoMapper;
using BookingCare.Services.Doctor.Exceptions;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Doctor.Services.Implementations;

public class ServiceTypeService : BaseService, IServiceTypeService
{
    private readonly IServiceTypeRepository _repository;
    private readonly IMapper _mapper;

    public ServiceTypeService(IServiceTypeRepository repository, IMapper mapper, ILogger<ServiceTypeService> logger) : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    #region ServiceType CRUD Operations

    public async Task<ServiceTypeResponse> CreateServiceTypeAsync(CreateServiceTypeRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Validate unique name
            if (await _repository.ServiceTypeNameExistsAsync(request.Name))
            {
                throw new ArgumentException($"Service type with name '{request.Name}' already exists");
            }

            // Create service type entity
            var serviceType = _mapper.Map<ServiceTypeEntity>(request);
            serviceType.Id = Guid.NewGuid();

            var createdServiceType = await _repository.CreateServiceTypeAsync(serviceType);
            var response = _mapper.Map<ServiceTypeResponse>(createdServiceType);
            return response;
        }, nameof(CreateServiceTypeAsync));
    }

    public async Task<ServiceTypeResponse?> GetServiceTypeByIdAsync(Guid id)
    {
        var serviceType = await _repository.GetServiceTypeByIdAsync(id);
        return serviceType != null ? _mapper.Map<ServiceTypeResponse>(serviceType) : null;
    }

    public async Task<ServiceTypeResponse?> GetServiceTypeByNameAsync(string name)
    {
        var serviceType = await _repository.GetServiceTypeByNameAsync(name);
        return serviceType != null ? _mapper.Map<ServiceTypeResponse>(serviceType) : null;
    }

    public async Task<ServiceTypeResponse> UpdateServiceTypeAsync(UpdateServiceTypeRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Check if service type exists
            var existingServiceType = await _repository.GetServiceTypeByIdAsync(request.Id);
            if (existingServiceType == null)
            {
                throw new ArgumentException($"Service type with ID {request.Id} not found");
            }

            // Validate unique name (exclude current service type)
            if (await _repository.ServiceTypeNameExistsAsync(request.Name, request.Id))
            {
                throw new ArgumentException($"Service type with name '{request.Name}' already exists");
            }

            // Update service type entity
            _mapper.Map(request, existingServiceType);
            existingServiceType.UpdatedAt = DateTime.UtcNow;

            var updatedServiceType = await _repository.UpdateServiceTypeAsync(existingServiceType);
            var response = _mapper.Map<ServiceTypeResponse>(updatedServiceType);
            return response;
        }, nameof(UpdateServiceTypeAsync));
    }

    public async Task<bool> DeleteServiceTypeAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Check if service type exists
            if (!await _repository.ServiceTypeExistsAsync(id))
            {
                return false;
            }

            // Check if service type is being used by any doctor prices
            // This would require a method to check doctor_prices table
            // For now, we'll allow deletion and let database constraints handle it

            return await _repository.DeleteServiceTypeAsync(id);
        }, nameof(DeleteServiceTypeAsync));
    }

    public async Task<bool> ToggleDoctorServiceTypeStatusAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var serviceType = await _repository.GetServiceTypeByIdAsync(id);
            if (serviceType == null)
            {
                throw new ArgumentException($"Service type with ID {id} not found");
            }

            // Toggle status: ACTIVE -> INACTIVE, INACTIVE -> ACTIVE
            serviceType.Status = serviceType.Status == Status.ACTIVE ? Status.INACTIVE : Status.ACTIVE;
            serviceType.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateServiceTypeAsync(serviceType);
            return true;
        }, nameof(ToggleDoctorServiceTypeStatusAsync));
    }

    #endregion

    #region ServiceType Query Operations

    public async Task<ServiceTypeListResponse> GetServiceTypesAsync(ServiceTypeQueryRequest query)
    {
        var (serviceTypes, totalCount) = await _repository.GetServiceTypesAsync(query);
        var response = _mapper.Map<ServiceTypeListResponse>((serviceTypes, totalCount));

        // Set pagination info
        response.PageNumber = query.PageNumber;
        response.PageSize = query.PageSize;
        response.TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        return response;
    }

    public async Task<List<ServiceTypeResponse>> GetAllServiceTypesAsync()
    {
        var serviceTypes = await _repository.GetAllServiceTypesAsync();
        return _mapper.Map<List<ServiceTypeResponse>>(serviceTypes);
    }

    public async Task<List<ServiceTypeSimpleResponse>> GetActiveServiceTypesSimpleAsync()
    {
        var serviceTypes = await _repository.GetActiveServiceTypesSimpleAsync();
        return _mapper.Map<List<ServiceTypeSimpleResponse>>(serviceTypes);
    }

    #endregion

    #region Validation Operations

    public async Task<bool> ServiceTypeExistsAsync(Guid id)
    {
        return await _repository.ServiceTypeExistsAsync(id);
    }

    public async Task<bool> ServiceTypeNameExistsAsync(string name, Guid? excludeId = null)
    {
        return await _repository.ServiceTypeNameExistsAsync(name, excludeId);
    }

    #endregion
}
