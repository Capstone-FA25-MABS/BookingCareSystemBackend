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

public class SpecialtyService : BaseService, ISpecialtyService
{
    private readonly ISpecialtyRepository _repository;
    private readonly IMapper _mapper;

    public SpecialtyService(ISpecialtyRepository repository, IMapper mapper, ILogger<SpecialtyService> logger) : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    #region Specialty CRUD Operations

    public async Task<SpecialtyResponse> CreateSpecialtyAsync(CreateSpecialtyRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Validate unique constraint
            if (await _repository.SpecialtyNameExistsAsync(request.Name))
            {
                throw SpecialtyConflictException.WithName(request.Name);
            }

            // Create specialty entity
            var specialty = _mapper.Map<SpecialtyEntity>(request);
            specialty.Id = Guid.NewGuid();

            var createdSpecialty = await _repository.CreateSpecialtyAsync(specialty);
            return _mapper.Map<SpecialtyResponse>(createdSpecialty);
        }, nameof(CreateSpecialtyAsync));
    }

    public async Task<SpecialtyResponse?> GetSpecialtyByIdAsync(Guid id)
    {
        var specialty = await _repository.GetSpecialtyByIdAsync(id);
        return specialty != null ? _mapper.Map<SpecialtyResponse>(specialty) : null;
    }

    public async Task<SpecialtyResponse?> GetSpecialtyByNameAsync(string name)
    {
        var specialty = await _repository.GetSpecialtyByNameAsync(name);
        return specialty != null ? _mapper.Map<SpecialtyResponse>(specialty) : null;
    }

    public async Task<SpecialtyResponse> UpdateSpecialtyAsync(UpdateSpecialtyRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Check if specialty exists
            var existingSpecialty = await _repository.GetSpecialtyByIdAsync(request.Id);
            if (existingSpecialty == null)
            {
                throw SpecialtyNotFoundException.WithId(request.Id);
            }

            // Validate unique constraint if name is being updated
            if (!string.IsNullOrEmpty(request.Name) && request.Name != existingSpecialty.Name &&
                await _repository.SpecialtyNameExistsAsync(request.Name, request.Id))
            {
                throw SpecialtyConflictException.WithName(request.Name);
            }

            // Update specialty entity
            _mapper.Map(request, existingSpecialty);
            existingSpecialty.UpdatedAt = DateTime.UtcNow;

            var updatedSpecialty = await _repository.UpdateSpecialtyAsync(existingSpecialty);
            return _mapper.Map<SpecialtyResponse>(updatedSpecialty);
        }, nameof(UpdateSpecialtyAsync));
    }

    public async Task<bool> DeleteSpecialtyAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            return await _repository.DeleteSpecialtyAsync(id);
        }, nameof(DeleteSpecialtyAsync));
    }

    public async Task<bool> ToggleSpecialtyStatusAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var specialty = await _repository.GetSpecialtyByIdAsync(id);
            if (specialty == null)
            {
                throw SpecialtyNotFoundException.WithId(id);
            }

            // Toggle status: ACTIVE -> INACTIVE, INACTIVE -> ACTIVE
            specialty.Status = specialty.Status == Status.ACTIVE ? Status.INACTIVE : Status.ACTIVE;
            specialty.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateSpecialtyAsync(specialty);
            return true;
        }, nameof(ToggleSpecialtyStatusAsync));
    }

    #endregion

    #region Specialty Query Operations

    public async Task<SpecialtyListResponse> GetSpecialtiesAsync(SpecialtyQueryRequest query)
    {
        var (specialties, totalCount) = await _repository.GetSpecialtiesAsync(query);
        var response = _mapper.Map<SpecialtyListResponse>((specialties, totalCount));

        // Set pagination info
        response.PageNumber = query.PageNumber;
        response.PageSize = query.PageSize;
        response.TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        return response;
    }

    public async Task<List<SpecialtyResponse>> GetAllSpecialtiesAsync()
    {
        var specialties = await _repository.GetAllSpecialtiesAsync();
        return _mapper.Map<List<SpecialtyResponse>>(specialties);
    }

    public async Task<List<SpecialtyResponse>> GetActiveSpecialtiesAsync()
    {
        var specialties = await _repository.GetActiveSpecialtiesAsync();
        return _mapper.Map<List<SpecialtyResponse>>(specialties);
    }

    public async Task<List<SpecialtySimpleResponse>> GetActiveSpecialtiesSimpleAsync()
    {
        var specialties = await _repository.GetActiveSpecialtiesSimpleAsync();
        return _mapper.Map<List<SpecialtySimpleResponse>>(specialties);
    }

    #endregion

    #region Validation Operations

    public async Task<bool> SpecialtyExistsAsync(Guid id)
    {
        return await _repository.SpecialtyExistsAsync(id);
    }

    public async Task<bool> SpecialtyNameExistsAsync(string name, Guid? excludeId = null)
    {
        return await _repository.SpecialtyNameExistsAsync(name, excludeId);
    }

    public async Task<List<SpecialtyResponse>> GetSpecialtiesByIdsAsync(List<Guid> ids)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            if (ids == null || ids.Count == 0)
            {
                return new List<SpecialtyResponse>();
            }

            var specialties = await _repository.GetSpecialtiesByIdsAsync(ids);
            return _mapper.Map<List<SpecialtyResponse>>(specialties);
        }, nameof(GetSpecialtiesByIdsAsync));
    }

    #endregion
}
