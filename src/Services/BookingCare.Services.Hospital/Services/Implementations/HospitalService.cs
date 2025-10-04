using AutoMapper;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Services.Implementations;

public class HospitalService : IHospitalService
{
    private readonly IHospitalRepository _hospitalRepository;
    private readonly IMapper _mapper;

    public HospitalService(IHospitalRepository hospitalRepository, IMapper mapper)
    {
        _hospitalRepository = hospitalRepository;
        _mapper = mapper;
    }

    public async Task<HospitalDetailResponse?> GetByIdAsync(Guid id)
    {
        var hospital = await _hospitalRepository.GetByIdAsync(id);
        return hospital != null ? _mapper.Map<HospitalDetailResponse>(hospital) : null;
    }

    public async Task<HospitalResponse?> GetByEmailAsync(string email)
    {
        var hospital = await _hospitalRepository.GetByEmailAsync(email);
        return hospital != null ? _mapper.Map<HospitalResponse>(hospital) : null;
    }

    public async Task<HospitalListResponse> GetAllAsync()
    {
        var hospitals = await _hospitalRepository.GetAllAsync();
        var hospitalResponses = _mapper.Map<List<HospitalResponse>>(hospitals);

        return new HospitalListResponse
        {
            Hospitals = hospitalResponses,
            TotalCount = hospitalResponses.Count,
            Page = 1,
            PageSize = hospitalResponses.Count,
            TotalPages = 1
        };
    }

    public async Task<HospitalListResponse> GetFilteredAsync(HospitalFilterRequest filter)
    {
        var (hospitals, totalCount) = await _hospitalRepository.GetFilteredAsync(filter);
        var hospitalResponses = _mapper.Map<List<HospitalResponse>>(hospitals);

        return new HospitalListResponse
        {
            Hospitals = hospitalResponses,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
        };
    }

    public async Task<HospitalDetailResponse> CreateAsync(CreateHospitalRequest request)
    {
        // Validate email uniqueness
        if (await _hospitalRepository.EmailExistsAsync(request.Email))
        {
            throw new HospitalAlreadyExistsException(request.Email);
        }

        var hospital = _mapper.Map<HospitalEntity>(request);
        hospital.Status = Status.ACTIVE;

        try
        {
            var createdHospital = await _hospitalRepository.CreateAsync(hospital);

            // Add specialties if provided
            if (request.SpecialtyIds != null && request.SpecialtyIds.Any())
            {
                foreach (var specialtyId in request.SpecialtyIds)
                {
                    await AddSpecialtyAsync(createdHospital.Id, specialtyId);
                }
            }

            // Reload hospital with relationships
            var hospitalWithRelations = await _hospitalRepository.GetByIdAsync(createdHospital.Id);
            return _mapper.Map<HospitalDetailResponse>(hospitalWithRelations);
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException("Failed to create hospital", ex);
        }
    }

    public async Task<HospitalResponse> UpdateAsync(Guid id, UpdateHospitalRequest request)
    {
        var existingHospital = await _hospitalRepository.GetByIdAsync(id);
        if (existingHospital == null)
        {
            throw new HospitalNotFoundException(id);
        }

        // Validate email uniqueness if email is being updated
        if (!string.IsNullOrEmpty(request.Email) && request.Email != existingHospital.Email && await _hospitalRepository.EmailExistsAsync(request.Email, id))
        {
            throw new HospitalAlreadyExistsException(request.Email);
        }

        // Update hospital properties
        _mapper.Map(request, existingHospital);

        try
        {
            var updatedHospital = await _hospitalRepository.UpdateAsync(existingHospital);

            // Update specialties if provided
            if (request.SpecialtyIds != null)
            {
                // Remove existing specialties and add new ones
                // This is a simplified approach - in a real scenario, you might want to be more selective
                // Clear existing specialties (this would need to be implemented in the repository)
                // Then add new specialties
                foreach (var specialtyId in request.SpecialtyIds)
                {
                    await AddSpecialtyAsync(id, specialtyId);
                }
            }

            return _mapper.Map<HospitalResponse>(updatedHospital);
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException("Failed to update hospital", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        if (!await _hospitalRepository.ExistsAsync(id))
        {
            throw new HospitalNotFoundException(id);
        }

        try
        {
            return await _hospitalRepository.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException("Failed to delete hospital", ex);
        }
    }

    public async Task<List<HospitalResponse>> GetBySpecialtyAsync(Guid specialtyId)
    {
        var hospitals = await _hospitalRepository.GetBySpecialtyAsync(specialtyId);
        return _mapper.Map<List<HospitalResponse>>(hospitals);
    }

    public async Task<List<HospitalResponse>> GetByAccountIdAsync(Guid accountId)
    {
        var hospitals = await _hospitalRepository.GetByAccountIdAsync(accountId);
        return _mapper.Map<List<HospitalResponse>>(hospitals);
    }

    public Task<bool> AddSpecialtyAsync(Guid hospitalId, Guid specialtyId)
    {
        // This would need to be implemented properly with a HospitalSpecialty repository
        // For now, this is a placeholder
        try
        {
            // Implementation would add a record to hospital_specialties table
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException($"Failed to add specialty {specialtyId} to hospital {hospitalId}", ex);
        }
    }

    public Task<bool> RemoveSpecialtyAsync(Guid hospitalId, Guid specialtyId)
    {
        // This would need to be implemented properly with a HospitalSpecialty repository
        // For now, this is a placeholder
        try
        {
            // Implementation would remove a record from hospital_specialties table
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException($"Failed to remove specialty {specialtyId} from hospital {hospitalId}", ex);
        }
    }
}
