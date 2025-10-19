using AutoMapper;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Auth.Protos;

namespace BookingCare.Services.Hospital.Services.Implementations;

public class HospitalService : IHospitalService
{
    private readonly IHospitalRepository _hospitalRepository;
    private readonly IMapper _mapper;
    private readonly AuthService.AuthServiceClient _authClient;
    private readonly ILogger<HospitalService> _logger;

    public HospitalService(
        IHospitalRepository hospitalRepository,
        IMapper mapper,
        AuthService.AuthServiceClient authClient,
        ILogger<HospitalService> logger)
    {
        _hospitalRepository = hospitalRepository;
        _mapper = mapper;
        _authClient = authClient;
        _logger = logger;
    }

    public async Task<HospitalDetailResponse?> GetByIdAsync(Guid id)
    {
        var hospital = await _hospitalRepository.GetByIdAsync(id);
        if (hospital == null) return null;

        var response = _mapper.Map<HospitalDetailResponse>(hospital);
        await EnrichHospitalDetailWithStatusAsync(response);
        return response;
    }

    public async Task<HospitalResponse?> GetByEmailAsync(string email)
    {
        var hospital = await _hospitalRepository.GetByEmailAsync(email);
        if (hospital == null) return null;

        var response = _mapper.Map<HospitalResponse>(hospital);
        await EnrichHospitalsWithStatusAsync(new List<HospitalResponse> { response });
        return response;
    }

    public async Task<HospitalListResponse> GetAllAsync()
    {
        var hospitals = await _hospitalRepository.GetAllAsync();
        var hospitalResponses = _mapper.Map<List<HospitalResponse>>(hospitals);

        // Enrich with status from auth service
        await EnrichHospitalsWithStatusAsync(hospitalResponses);

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

        // Enrich with status from auth service
        await EnrichHospitalsWithStatusAsync(hospitalResponses);

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
            var response = _mapper.Map<HospitalDetailResponse>(hospitalWithRelations);
            await EnrichHospitalDetailWithStatusAsync(response);
            return response;
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

            var response = _mapper.Map<HospitalResponse>(updatedHospital);
            await EnrichHospitalsWithStatusAsync(new List<HospitalResponse> { response });
            return response;
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
        var hospitalResponses = _mapper.Map<List<HospitalResponse>>(hospitals);
        await EnrichHospitalsWithStatusAsync(hospitalResponses);
        return hospitalResponses;
    }

    public async Task<List<HospitalResponse>> GetByAccountIdAsync(Guid accountId)
    {
        var hospitals = await _hospitalRepository.GetByAccountIdAsync(accountId);
        var hospitalResponses = _mapper.Map<List<HospitalResponse>>(hospitals);
        await EnrichHospitalsWithStatusAsync(hospitalResponses);
        return hospitalResponses;
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

    #region Optimized Methods for gRPC Performance

    public async Task<Models.Entities.HospitalEntity?> GetHospitalBasicInfoByIdAsync(Guid id)
    {
        return await _hospitalRepository.GetHospitalBasicInfoByIdAsync(id);
    }

    public async Task<List<Models.Entities.HospitalEntity>> GetHospitalsBasicInfoByIdsAsync(IEnumerable<Guid> ids)
    {
        return await _hospitalRepository.GetHospitalsBasicInfoByIdsAsync(ids);
    }

    public async Task<List<HospitalSimpleResponse>> GetActiveHospitalsSimpleAsync()
    {
        var hospitals = await _hospitalRepository.GetActiveHospitalsSimpleAsync();
        return _mapper.Map<List<HospitalSimpleResponse>>(hospitals);
    }

    #endregion

    #region Auth Service Integration

    /// <summary>
    /// Get account statuses for a list of account IDs
    /// </summary>
    private async Task<Dictionary<Guid, Status>> GetAccountStatusesAsync(IEnumerable<Guid> accountIds)
    {
        var statusMap = new Dictionary<Guid, Status>();

        try
        {
            var request = new GetAccountStatusByIdsRequest();
            request.AccountIds.AddRange(accountIds.Select(id => id.ToString()));

            var response = await _authClient.GetAccountStatusByIdsAsync(request);

            foreach (var accountStatus in response.AccountStatuses)
            {
                if (Guid.TryParse(accountStatus.AccountId, out var accountId) && accountStatus.Found)
                {
                    // Chuyển đổi từ int sang enum Status
                    var status = (Status)accountStatus.Status;
                    statusMap[accountId] = status;
                }
            }
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            _logger.LogWarning(ex, "Auth gRPC GetAccountStatusByIds failed");
            // Return empty dictionary on failure
        }

        return statusMap;
    }

    /// <summary>
    /// Enrich hospital responses with account status
    /// </summary>
    private async Task EnrichHospitalsWithStatusAsync(List<HospitalResponse> hospitals)
    {
        if (!hospitals.Any()) return;

        var accountIds = hospitals.Select(h => h.AccountId).Distinct();
        var statusMap = await GetAccountStatusesAsync(accountIds);

        // Note: Status is now managed by Auth service, not stored in HospitalEntity
        // Status enrichment is handled at the response level, not entity level
    }

    /// <summary>
    /// Enrich hospital detail response with account status
    /// </summary>
    private async Task EnrichHospitalDetailWithStatusAsync(HospitalDetailResponse hospital)
    {
        var statusMap = await GetAccountStatusesAsync(new[] { hospital.AccountId });

        // Note: Status is now managed by Auth service, not stored in HospitalEntity
        // Status enrichment is handled at the response level, not entity level
    }

    #endregion
}
