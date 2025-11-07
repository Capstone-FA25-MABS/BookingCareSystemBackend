using AutoMapper;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Services.Helpers;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Doctor.Protos;
using Microsoft.Extensions.Caching.Memory;

namespace BookingCare.Services.Hospital.Services.Implementations;

public class HospitalService : IHospitalService
{
    private readonly IHospitalRepository _hospitalRepository;
    private readonly IHospitalImageRepository _hospitalImageRepository;
    private readonly IMapper _mapper;
    private readonly HospitalServiceDependencies _dependencies;
    private readonly ILogger<HospitalService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly object _circuitBreakerLock = new object();
    private static int _consecutiveFailures = 0;
    private static DateTime _lastFailureTime = DateTime.MinValue;
    private const int MaxFailures = 5;
    private const int CircuitBreakerTimeoutMinutes = 2;

    public HospitalService(
        IHospitalRepository hospitalRepository,
        IHospitalImageRepository hospitalImageRepository,
        IMapper mapper,
        HospitalServiceDependencies dependencies,
        ILogger<HospitalService> logger,
        IMemoryCache cache)
    {
        _hospitalRepository = hospitalRepository;
        _hospitalImageRepository = hospitalImageRepository;
        _mapper = mapper;
        _dependencies = dependencies;
        _logger = logger;
        _cache = cache;
    }

    public async Task<HospitalProfileResponse?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting hospital by ID: {HospitalId}", id);
            var hospital = await _hospitalRepository.GetByIdAsync(id);
            if (hospital == null)
            {
                _logger.LogWarning("Hospital with ID {HospitalId} not found", id);
                return null;
            }

            _logger.LogInformation("Hospital found: {HospitalName}, mapping to response", hospital.Name);
            _logger.LogInformation("Hospital images count: {ImageCount}", hospital.HospitalImages?.Count ?? 0);
            _logger.LogInformation("Hospital specialties count: {SpecialtyCount}", hospital.HospitalSpecialties?.Count ?? 0);
            _logger.LogInformation("Hospital service types count: {ServiceTypeCount}", hospital.HospitalServiceTypes?.Count ?? 0);
            _logger.LogInformation("Hospital service medicals count: {ServiceMedicalCount}", hospital.HospitalServiceMedicals?.Count ?? 0);
            if (hospital.HospitalImages?.Any() == true)
            {
                _logger.LogInformation("Sample image URL: {ImageUrl}", hospital.HospitalImages.First().ImageUrl);
            }
            else
            {
                _logger.LogWarning("No images found for hospital {HospitalId}", id);
            }
            if (hospital.HospitalServiceTypes?.Any() == true)
            {
                _logger.LogInformation("Hospital service type IDs: {ServiceTypeIds}", string.Join(", ", hospital.HospitalServiceTypes.Select(st => st.ServiceTypeId)));
            }
            else
            {
                _logger.LogWarning("No service types found for hospital {HospitalId}", id);
            }
            var response = _mapper.Map<HospitalProfileResponse>(hospital);
            _logger.LogInformation("Response service types count: {ResponseServiceTypeCount}", response.ServiceTypes?.Count ?? 0);
            _logger.LogInformation("Response service medicals count: {ResponseServiceMedicalCount}", response.ServiceMedicals?.Count ?? 0);
            if (hospital.HospitalServiceMedicals?.Any() == true)
            {
                _logger.LogInformation("Hospital service medical IDs: {ServiceMedicalIds}", string.Join(", ", hospital.HospitalServiceMedicals.Select(sm => sm.ServiceMedicalId)));
            }
            else
            {
                _logger.LogWarning("No service medicals found for hospital {HospitalId}", id);
            }
            _logger.LogInformation("Mapping completed successfully");
            _logger.LogInformation("Response images count: {ResponseImageCount}", response.Images?.Count ?? 0);

            // Enrich specialties with name and image via Doctor gRPC
            var specialtyIds = hospital.HospitalSpecialties?.Select(hs => hs.SpecialtyId).ToList() ?? new List<Guid>();
            if (specialtyIds.Any() && _dependencies.DoctorClient != null)
            {
                try
                {
                    var bulkRequest = new GetSpecialtiesByIdsRequest();
                    bulkRequest.Ids.AddRange(specialtyIds.Select(x => x.ToString()));
                    var bulkResponse = await GetSpecialtiesBulkWithRetryAsync(bulkRequest);
                    if (bulkResponse?.Specialties != null)
                    {
                        var map = bulkResponse.Specialties
                            .Where(s => Guid.TryParse(s.Id, out _))
                            .ToDictionary(s => Guid.Parse(s.Id), s => s);

                        // Get doctor counts for each specialty
                        var doctorCounts = await GetDoctorCountsBySpecialtyAndHospitalAsync(id, specialtyIds);

                        response.Specialties = specialtyIds
                            .Where(id => map.ContainsKey(id))
                            .Select(specialtyId => new HospitalSpecialtyWithImageResponse
                            {
                                Id = specialtyId,
                                Name = map[specialtyId].Name,
                                ImageUrl = map[specialtyId].ImageUrl,
                                DoctorCount = doctorCounts.GetValueOrDefault(specialtyId, 0)
                            })
                            .ToList();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enrich specialties for hospital {HospitalId}, returning empty specialties list", id);
                    response.Specialties = new List<HospitalSpecialtyWithImageResponse>();
                }
            }
            else
            {
                // If no specialties or gRPC client is not available, return empty list
                response.Specialties = new List<HospitalSpecialtyWithImageResponse>();
            }

            // Enrich service medicals with details via ServiceMedical gRPC (similar to specialties)
            var serviceMedicalIds = hospital.HospitalServiceMedicals?.Select(hsm => hsm.ServiceMedicalId).ToList() ?? new List<Guid>();
            if (serviceMedicalIds.Any() && _dependencies.ServiceMedicalClient != null)
            {
                try
                {
                    _logger.LogInformation("Calling ServiceMedical gRPC to get {Count} service medicals for hospital {HospitalId}", serviceMedicalIds.Count, id);

                    // Call gRPC for each service medical to get details
                    var serviceMedicalTasks = serviceMedicalIds.Select(async smId =>
                    {
                        try
                        {
                            var smRequest = new ServiceMedical.Protos.GetServiceGrpcRequest { Id = smId.ToString() };
                            var smResponse = await _dependencies.ServiceMedicalClient.GetServiceAsync(smRequest);

                            if (smResponse != null && !string.IsNullOrEmpty(smResponse.Id))
                            {
                                return new HospitalServiceMedicalResponse
                                {
                                    Id = Guid.Parse(smResponse.Id),
                                    Name = smResponse.Name,
                                    ImageUrl = smResponse.ImageUrl,
                                    Price = decimal.Parse(smResponse.Price)
                                };
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to get service medical {ServiceMedicalId} from gRPC", smId);
                        }
                        return null;
                    }).ToList();

                    var serviceMedicals = await Task.WhenAll(serviceMedicalTasks);
                    response.ServiceMedicals = serviceMedicals.Where(sm => sm != null).ToList()!;

                    _logger.LogInformation("Enriched {Count} service medicals for hospital {HospitalId}", response.ServiceMedicals.Count, id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enrich service medicals for hospital {HospitalId}, returning empty list", id);
                    response.ServiceMedicals = new List<HospitalServiceMedicalResponse>();
                }
            }
            else
            {
                response.ServiceMedicals = new List<HospitalServiceMedicalResponse>();
                _logger.LogInformation("No service medical IDs found for hospital {HospitalId}", id);
            }

            // Enrich service types with doctor count via Doctor gRPC (similar to specialties)
            var serviceTypeIds = hospital.HospitalServiceTypes?.Select(hst => hst.ServiceTypeId).ToList() ?? new List<Guid>();
            if (serviceTypeIds.Any() && _dependencies.DoctorClient != null)
            {
                try
                {
                    _logger.LogInformation("Calling Doctor gRPC to get {Count} service types for hospital {HospitalId}", serviceTypeIds.Count, id);

                    var serviceTypeRequest = new Doctor.Protos.GetServiceTypesByHospitalRequest
                    {
                        HospitalId = id.ToString()
                    };
                    var serviceTypeResponse = await _dependencies.DoctorClient.GetServiceTypesByHospitalAsync(serviceTypeRequest);

                    if (serviceTypeResponse?.ServiceTypes != null && serviceTypeResponse.ServiceTypes.Any())
                    {
                        // Map service types from gRPC response that match our hospital's service type IDs
                        var serviceTypeMap = serviceTypeResponse.ServiceTypes
                            .Where(st => Guid.TryParse(st.Id, out _))
                            .ToDictionary(st => Guid.Parse(st.Id), st => st);

                        response.ServiceTypes = serviceTypeIds
                            .Where(stId => serviceTypeMap.ContainsKey(stId))
                            .Select(stId => new HospitalServiceTypeResponse
                            {
                                Id = stId,
                                Name = serviceTypeMap[stId].Name,
                                ImageUrl = serviceTypeMap[stId].ImageUrl,
                                DoctorCount = serviceTypeMap[stId].DoctorCount
                            })
                            .ToList();

                        _logger.LogInformation("Enriched {Count} service types for hospital {HospitalId}", response.ServiceTypes.Count, id);
                    }
                    else
                    {
                        response.ServiceTypes = new List<HospitalServiceTypeResponse>();
                        _logger.LogInformation("No service types returned from gRPC for hospital {HospitalId}", id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enrich service types for hospital {HospitalId}, returning empty list", id);
                    response.ServiceTypes = new List<HospitalServiceTypeResponse>();
                }
            }
            else
            {
                response.ServiceTypes = new List<HospitalServiceTypeResponse>();
                _logger.LogInformation("No service type IDs found for hospital {HospitalId}", id);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdAsync for hospital {HospitalId}", id);
            throw new HospitalOperationException($"Failed to retrieve hospital with ID {id}", ex);
        }
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

        // Email and Phone are read-only and cannot be updated
        // These fields are ignored even if provided in the request

        // Validate that required fields are provided
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidHospitalDataException("Tên bệnh viện là bắt buộc! Vui lòng nhập tên bệnh viện");
        }

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            throw new InvalidHospitalDataException("Địa chỉ là bắt buộc! Vui lòng nhập địa chỉ");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new InvalidHospitalDataException("Mô tả là bắt buộc! Vui lòng nhập mô tả");
        }

        // Update hospital properties (but not relationships)
        // Email and Phone are NOT updated - they remain unchanged
        existingHospital.Name = request.Name;
        existingHospital.Address = request.Address;
        // existingHospital.Phone = request.Phone ?? existingHospital.Phone; // Read-only, not updated
        // existingHospital.Email = request.Email ?? existingHospital.Email; // Read-only, not updated
        existingHospital.Description = request.Description;
        existingHospital.BackgroundUrl = request.BackgroundUrl ?? existingHospital.BackgroundUrl;
        existingHospital.AvatarUrl = request.AvatarUrl ?? existingHospital.AvatarUrl;
        existingHospital.UpdatedAt = DateTime.Now;

        try
        {
            // Update relationships if provided
            if (request.SpecialtyIds != null)
            {
                await UpdateHospitalSpecialtiesAsync(id, request.SpecialtyIds);
            }

            if (request.ServiceTypeIds != null)
            {
                await UpdateHospitalServiceTypesAsync(id, request.ServiceTypeIds);
            }

            if (request.ServiceMedicalIds != null)
            {
                await UpdateHospitalServiceMedicalsAsync(id, request.ServiceMedicalIds);
            }

            var updatedHospital = await _hospitalRepository.UpdateAsync(existingHospital);

            // Reload with all relationships
            var hospitalWithRelations = await _hospitalRepository.GetByIdAsync(id);
            var response = _mapper.Map<HospitalResponse>(hospitalWithRelations);
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

    public async Task<List<Models.Entities.HospitalEntity>> GetHospitalsByAccountIdsAsync(IEnumerable<Guid> accountIds)
    {
        try
        {
            _logger.LogInformation("Getting hospitals by {Count} account IDs", accountIds.Count());
            var hospitals = await _hospitalRepository.GetByAccountIdsAsync(accountIds);
            _logger.LogInformation("Retrieved {Count} hospitals for batch request", hospitals.Count);
            return hospitals;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetHospitalsByAccountIdsAsync");
            throw new HospitalOperationException("Failed to retrieve hospitals by account IDs", ex);
        }
    }

    public async Task<bool> AddSpecialtyAsync(Guid hospitalId, Guid specialtyId)
    {
        try
        {
            return await _hospitalRepository.AddSpecialtyAsync(hospitalId, specialtyId);
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException($"Failed to add specialty {specialtyId} to hospital {hospitalId}", ex);
        }
    }

    public async Task<bool> RemoveSpecialtyAsync(Guid hospitalId, Guid specialtyId)
    {
        try
        {
            return await _hospitalRepository.RemoveSpecialtyAsync(hospitalId, specialtyId);
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException($"Failed to remove specialty {specialtyId} from hospital {hospitalId}", ex);
        }
    }

    private async Task UpdateHospitalSpecialtiesAsync(Guid hospitalId, List<Guid> specialtyIds)
    {
        // Remove all existing specialties
        var hospital = await _hospitalRepository.GetByIdAsync(hospitalId);
        if (hospital?.HospitalSpecialties != null)
        {
            foreach (var specialty in hospital.HospitalSpecialties.ToList())
            {
                await RemoveSpecialtyAsync(hospitalId, specialty.SpecialtyId);
            }
        }

        // Add new specialties
        foreach (var specialtyId in specialtyIds.Distinct())
        {
            await AddSpecialtyAsync(hospitalId, specialtyId);
        }
    }

    private async Task UpdateHospitalServiceTypesAsync(Guid hospitalId, List<Guid> serviceTypeIds)
    {
        // Remove all existing service types
        var hospital = await _hospitalRepository.GetByIdAsync(hospitalId);
        if (hospital?.HospitalServiceTypes != null)
        {
            foreach (var serviceType in hospital.HospitalServiceTypes.ToList())
            {
                await _hospitalRepository.RemoveServiceTypeAsync(hospitalId, serviceType.ServiceTypeId);
            }
        }

        // Add new service types
        foreach (var serviceTypeId in serviceTypeIds.Distinct())
        {
            await _hospitalRepository.AddServiceTypeAsync(hospitalId, serviceTypeId);
        }
    }

    private async Task UpdateHospitalServiceMedicalsAsync(Guid hospitalId, List<Guid> serviceMedicalIds)
    {
        // Remove all existing service medicals
        var hospital = await _hospitalRepository.GetByIdAsync(hospitalId);
        if (hospital?.HospitalServiceMedicals != null)
        {
            foreach (var serviceMedical in hospital.HospitalServiceMedicals.ToList())
            {
                await _hospitalRepository.RemoveServiceMedicalAsync(hospitalId, serviceMedical.ServiceMedicalId);
            }
        }

        // Add new service medicals
        foreach (var serviceMedicalId in serviceMedicalIds.Distinct())
        {
            await _hospitalRepository.AddServiceMedicalAsync(hospitalId, serviceMedicalId);
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

    public async Task<HospitalListOptimizedPaginatedResponse> GetOptimizedHospitalListAsync(HospitalListOptimizedFilterRequest filter)
    {
        _logger.LogInformation("GetOptimizedHospitalListAsync called with filter: Search={Search}, SpecialtyIds={SpecialtyIds}, ProvinceId={ProvinceId}, DistrictId={DistrictId}, Page={Page}, PageSize={PageSize}",
            filter.Search, string.Join(",", filter.SpecialtyIds ?? new string[0]), filter.ProvinceId, filter.DistrictId, filter.Page, filter.PageSize);

        // Convert string specialty IDs to Guid and validate if provided
        List<Guid>? specialtyGuids = null;
        if (filter.SpecialtyIds != null && filter.SpecialtyIds.Length > 0)
        {
            _logger.LogInformation("Converting {Count} specialty IDs from string to Guid", filter.SpecialtyIds.Length);
            specialtyGuids = new List<Guid>();

            foreach (var specialtyIdStr in filter.SpecialtyIds)
            {
                if (Guid.TryParse(specialtyIdStr, out var specialtyGuid))
                {
                    specialtyGuids.Add(specialtyGuid);
                }
                else
                {
                    _logger.LogWarning("Invalid specialty ID format: {SpecialtyId}", specialtyIdStr);
                }
            }

            if (specialtyGuids.Any())
            {
                _logger.LogInformation("Validating {Count} specialty IDs with Doctor service via gRPC", specialtyGuids.Count);
                var isValid = await ValidateSpecialtyIdsAsync(specialtyGuids);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid specialty IDs provided, returning empty result");
                    return new HospitalListOptimizedPaginatedResponse
                    {
                        Hospitals = new List<HospitalListOptimizedResponse>(),
                        TotalCount = 0,
                        Page = filter.Page,
                        PageSize = filter.PageSize,
                        TotalPages = 0
                    };
                }
            }
        }

        // Always use location filtering approach (similar to Doctor service)
        // Get all hospitals first, then apply location filter if needed
        return await GetOptimizedHospitalListWithLocationFilteringAsync(filter);
    }

    /// <summary>
    /// Get optimized hospital list with location filtering applied BEFORE pagination
    /// This ensures all hospitals are considered for location filtering, not just the current page
    /// </summary>
    private async Task<HospitalListOptimizedPaginatedResponse> GetOptimizedHospitalListWithLocationFilteringAsync(HospitalListOptimizedFilterRequest filter)
    {
        // Get all hospitals without pagination for location filtering
        var queryWithoutPagination = new HospitalListOptimizedFilterRequest
        {
            Search = filter.Search,
            SpecialtyIds = filter.SpecialtyIds, // Keep as string array for repository
            ProvinceId = null, // Location filtering handled at service level
            DistrictId = null, // Location filtering handled at service level
            Page = 1,
            PageSize = int.MaxValue, // Get all records
            SortBy = filter.SortBy,
            SortOrder = filter.SortOrder
        };

        var (allHospitals, _) = await _hospitalRepository.GetOptimizedHospitalListAsync(queryWithoutPagination);
        var hospitalResponses = _mapper.Map<List<HospitalListOptimizedResponse>>(allHospitals);

        // Enrich hospitals with specialty information from Doctor service
        await EnrichHospitalsWithSpecialtyInfoAsync(hospitalResponses, allHospitals);

        // Apply location filtering to ALL hospitals (if location filters provided)
        _logger.LogInformation("Applying location filtering to {HospitalCount} hospitals with ProvinceId: {ProvinceId}, DistrictId: {DistrictId}",
            hospitalResponses.Count, filter.ProvinceId, filter.DistrictId);

        // Debug: Log some hospital addresses
        foreach (var hospital in hospitalResponses.Take(3))
        {
            _logger.LogInformation("Sample hospital address: {HospitalName} - {Address}", hospital.Name, hospital.Address);
        }

        var locationFilteredHospitals = await _dependencies.LocationApiService.ApplyLocationFilteringAsync(
            hospitalResponses,
            filter.ProvinceId,
            filter.DistrictId);

        _logger.LogInformation("Location filtering result: {FilteredCount} hospitals after filtering", locationFilteredHospitals.Count);

        // Apply sorting to filtered results
        var sortedHospitals = ApplySorting(locationFilteredHospitals, filter.SortBy, filter.SortOrder);

        // Apply pagination to the filtered results
        var totalCount = sortedHospitals.Count;
        var paginatedHospitals = sortedHospitals
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return new HospitalListOptimizedPaginatedResponse
        {
            Hospitals = paginatedHospitals,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
        };
    }

    /// <summary>
    /// Validate specialty IDs by calling Doctor service via gRPC
    /// </summary>
    private async Task<bool> ValidateSpecialtyIdsAsync(List<Guid> specialtyIds)
    {
        try
        {
            if (specialtyIds == null || !specialtyIds.Any())
            {
                return true; // Empty list is valid
            }

            _logger.LogInformation("Validating {Count} specialty IDs with Doctor service via gRPC", specialtyIds.Count);

            var validCount = 0;
            foreach (var specialtyId in specialtyIds)
            {
                try
                {
                    var request = new GetSpecialtyByIdRequest
                    {
                        Id = specialtyId.ToString()
                    };

                    var response = await _dependencies.DoctorClient.GetSpecialtyByIdAsync(request);
                    if (response != null && !string.IsNullOrEmpty(response.Id))
                    {
                        validCount++;
                        _logger.LogDebug("Specialty ID {SpecialtyId} is valid: {SpecialtyName} (ImageUrl: {ImageUrl})",
                            specialtyId, response.Name, response.ImageUrl);
                    }
                }
                catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
                {
                    _logger.LogWarning("Specialty ID {SpecialtyId} not found", specialtyId);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating specialty ID {SpecialtyId}", specialtyId);
                    return false;
                }
            }

            var isValid = validCount == specialtyIds.Count;
            _logger.LogInformation("Specialty validation result: {ValidCount}/{TotalCount} valid", validCount, specialtyIds.Count);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating specialty IDs with Doctor service");
            return false;
        }
    }

    /// <summary>
    /// Apply sorting to hospital list
    /// </summary>
    private static List<HospitalListOptimizedResponse> ApplySorting(
        List<HospitalListOptimizedResponse> hospitals,
        string? sortBy,
        string? sortOrder)
    {
        return sortBy?.ToLower() switch
        {
            "name" => sortOrder?.ToLower() == "desc"
                ? hospitals.OrderByDescending(h => h.Name).ToList()
                : hospitals.OrderBy(h => h.Name).ToList(),
            "address" => sortOrder?.ToLower() == "desc"
                ? hospitals.OrderByDescending(h => h.Address).ToList()
                : hospitals.OrderBy(h => h.Address).ToList(),
            _ => hospitals.OrderBy(h => h.Name).ToList()
        };
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

            var response = await _dependencies.AuthClient.GetAccountStatusByIdsAsync(request);

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


    /// <summary>
    /// Get multiple specialties with retry logic and circuit breaker pattern
    /// </summary>
    private async Task<SpecialtiesBatchResponse?> GetSpecialtiesBulkWithRetryAsync(
        GetSpecialtiesByIdsRequest request,
        int maxRetries = 3)
    {
        // Check circuit breaker
        if (IsCircuitBreakerOpen())
        {
            _logger.LogWarning("Circuit breaker is OPEN - skipping gRPC call to Doctor service");
            return null;
        }

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await _dependencies.DoctorClient.GetSpecialtiesByIdsAsync(request);

                // Reset circuit breaker on success
                ResetCircuitBreaker();
                return result;
            }
            catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable && attempt < maxRetries)
            {
                // Shorter delays for bulk calls: 500ms, 1s, 2s
                var delay = Math.Pow(2, attempt) * 500;
                _logger.LogWarning("Bulk gRPC call failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms. Error: {Error}",
                    attempt, maxRetries, delay, ex.Message);
                await Task.Delay((int)delay);
            }
            catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable && attempt == maxRetries)
            {
                RecordFailure();
                _logger.LogError("Bulk gRPC call failed after {MaxRetries} attempts. Error: {Error}",
                    maxRetries, ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                RecordFailure();
                _logger.LogError(ex, "Unexpected error in bulk specialty retrieval");
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Check if circuit breaker is open
    /// </summary>
    private static bool IsCircuitBreakerOpen()
    {
        lock (_circuitBreakerLock)
        {
            if (_consecutiveFailures >= MaxFailures)
            {
                var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTime;
                if (timeSinceLastFailure.TotalMinutes < CircuitBreakerTimeoutMinutes)
                {
                    return true; // Circuit breaker is open
                }
                else
                {
                    // Reset circuit breaker after timeout
                    _consecutiveFailures = 0;
                    return false;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Record a failure for circuit breaker
    /// </summary>
    private static void RecordFailure()
    {
        lock (_circuitBreakerLock)
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Reset circuit breaker on success
    /// </summary>
    private static void ResetCircuitBreaker()
    {
        lock (_circuitBreakerLock)
        {
            _consecutiveFailures = 0;
        }
    }

    /// <summary>
    /// Enrich hospital responses with specialty information from Doctor service via gRPC
    /// </summary>
    private async Task EnrichHospitalsWithSpecialtyInfoAsync(
        List<HospitalListOptimizedResponse> hospitalResponses,
        List<HospitalEntity> hospitalEntities)
    {
        try
        {
            _logger.LogInformation("Enriching {HospitalCount} hospitals with specialty information", hospitalResponses.Count);

            // Create a map of hospital entities for quick lookup
            var hospitalEntityMap = hospitalEntities.ToDictionary(h => h.Id, h => h);

            // Process hospitals in smaller batches to avoid overwhelming the gRPC service
            const int batchSize = 5;
            var batches = hospitalResponses
                .Select((hospital, index) => new { hospital, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.hospital).ToList())
                .ToList();

            // Process batches in parallel with controlled concurrency
            const int maxConcurrentBatches = 2; // Limit concurrent batches to avoid overwhelming gRPC service
            var semaphore = new SemaphoreSlim(maxConcurrentBatches, maxConcurrentBatches);

            var batchTasks = batches.Select(async batch =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await ProcessHospitalBatchAsync(batch, hospitalEntityMap);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(batchTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching hospitals with specialty information");
            // Set default values for all hospitals in case of error
            foreach (var hospitalResponse in hospitalResponses)
            {
                hospitalResponse.Specialties = new List<HospitalSpecialtyOptimizedResponse>();
                hospitalResponse.TotalSpecialties = 0;
            }
        }
    }

    /// <summary>
    /// Process a batch of hospitals for specialty enrichment using bulk gRPC calls
    /// </summary>
    private async Task ProcessHospitalBatchAsync(
        List<HospitalListOptimizedResponse> hospitalBatch,
        Dictionary<Guid, HospitalEntity> hospitalEntityMap)
    {
        // Collect specialty information from hospitals
        var (allSpecialtyIds, hospitalSpecialtyMap) = CollectSpecialtyInformation(hospitalBatch, hospitalEntityMap);

        // Bulk fetch all specialties with caching
        var specialtyCache = await FetchSpecialtiesWithCachingAsync(allSpecialtyIds);

        // Assign specialties to each hospital
        AssignSpecialtiesToHospitals(hospitalBatch, hospitalSpecialtyMap, specialtyCache);
    }

    private (HashSet<Guid> allSpecialtyIds, Dictionary<Guid, List<Guid>> hospitalSpecialtyMap) CollectSpecialtyInformation(
        List<HospitalListOptimizedResponse> hospitalBatch,
        Dictionary<Guid, HospitalEntity> hospitalEntityMap)
    {
        var allSpecialtyIds = new HashSet<Guid>();
        var hospitalSpecialtyMap = new Dictionary<Guid, List<Guid>>();

        foreach (var hospitalResponse in hospitalBatch)
        {
            if (hospitalEntityMap.TryGetValue(hospitalResponse.Id, out var hospitalEntity))
            {
                var specialtyIds = hospitalEntity.HospitalSpecialties?.Select(hs => hs.SpecialtyId).ToList() ?? new List<Guid>();
                hospitalSpecialtyMap[hospitalResponse.Id] = specialtyIds;

                foreach (var specialtyId in specialtyIds)
                {
                    allSpecialtyIds.Add(specialtyId);
                }
            }
        }

        return (allSpecialtyIds, hospitalSpecialtyMap);
    }

    private async Task<Dictionary<Guid, SpecialtySimpleResponse>> FetchSpecialtiesWithCachingAsync(HashSet<Guid> allSpecialtyIds)
    {
        var specialtyCache = new Dictionary<Guid, SpecialtySimpleResponse>();

        if (!allSpecialtyIds.Any())
            return specialtyCache;

        try
        {
            // Check cache first and identify uncached IDs
            var uncachedIds = GetUncachedSpecialtyIds(allSpecialtyIds, specialtyCache);

            // Fetch uncached specialties
            if (uncachedIds.Any())
            {
                await FetchAndCacheUncachedSpecialtiesAsync(uncachedIds, specialtyCache);
            }

            _logger.LogInformation("Bulk retrieved {RetrievedCount}/{RequestedCount} specialties for batch (cached: {CachedCount}, fetched: {FetchedCount})",
                specialtyCache.Count, allSpecialtyIds.Count, specialtyCache.Count - uncachedIds.Count, uncachedIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk specialty retrieval for batch");
        }

        return specialtyCache;
    }

    private List<Guid> GetUncachedSpecialtyIds(HashSet<Guid> allSpecialtyIds, Dictionary<Guid, SpecialtySimpleResponse> specialtyCache)
    {
        var uncachedIds = new List<Guid>();

        foreach (var specialtyId in allSpecialtyIds)
        {
            var cacheKey = $"specialty_{specialtyId}";
            if (_cache.TryGetValue(cacheKey, out SpecialtySimpleResponse? cachedSpecialty) && cachedSpecialty != null)
            {
                specialtyCache[specialtyId] = cachedSpecialty;
            }
            else
            {
                uncachedIds.Add(specialtyId);
            }
        }

        return uncachedIds;
    }

    private async Task FetchAndCacheUncachedSpecialtiesAsync(List<Guid> uncachedIds, Dictionary<Guid, SpecialtySimpleResponse> specialtyCache)
    {
        var bulkRequest = new GetSpecialtiesByIdsRequest();
        bulkRequest.Ids.AddRange(uncachedIds.Select(id => id.ToString()));

        var bulkResponse = await GetSpecialtiesBulkWithRetryAsync(bulkRequest);
        if (bulkResponse?.Specialties == null)
            return;

        foreach (var specialty in bulkResponse.Specialties)
        {
            if (Guid.TryParse(specialty.Id, out var specialtyId))
            {
                specialtyCache[specialtyId] = specialty;
                CacheSpecialty(specialtyId, specialty);
            }
        }
    }

    private void CacheSpecialty(Guid specialtyId, SpecialtySimpleResponse specialty)
    {
        var cacheKey = $"specialty_{specialtyId}";
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Priority = CacheItemPriority.Normal
        };
        _cache.Set(cacheKey, specialty, cacheOptions);
    }

    private void AssignSpecialtiesToHospitals(
        List<HospitalListOptimizedResponse> hospitalBatch,
        Dictionary<Guid, List<Guid>> hospitalSpecialtyMap,
        Dictionary<Guid, SpecialtySimpleResponse> specialtyCache)
    {
        foreach (var hospitalResponse in hospitalBatch)
        {
            if (hospitalSpecialtyMap.TryGetValue(hospitalResponse.Id, out var specialtyIds))
            {
                var specialties = BuildHospitalSpecialties(specialtyIds, specialtyCache);
                hospitalResponse.Specialties = specialties;
                hospitalResponse.TotalSpecialties = specialtyIds.Count;

                _logger.LogDebug("Hospital {HospitalId} enriched with {RetrievedCount}/{TotalCount} specialties",
                    hospitalResponse.Id, specialties.Count, specialtyIds.Count);
            }
        }
    }

    private List<HospitalSpecialtyOptimizedResponse> BuildHospitalSpecialties(
        List<Guid> specialtyIds,
        Dictionary<Guid, SpecialtySimpleResponse> specialtyCache)
    {
        if (!specialtyIds.Any())
        {
            return new List<HospitalSpecialtyOptimizedResponse>();
        }

        var specialties = new List<HospitalSpecialtyOptimizedResponse>();
        foreach (var specialtyId in specialtyIds)
        {
            if (specialtyCache.TryGetValue(specialtyId, out var specialty))
            {
                specialties.Add(new HospitalSpecialtyOptimizedResponse
                {
                    Id = specialtyId,
                    Name = specialty.Name,
                    ImageUrl = specialty.ImageUrl
                });
            }
        }

        return specialties;
    }

    #endregion

    #region Doctor Count Operations

    private async Task<Dictionary<Guid, int>> GetDoctorCountsBySpecialtyAndHospitalAsync(Guid hospitalId, List<Guid> specialtyIds)
    {
        try
        {
            if (!specialtyIds.Any() || _dependencies.DoctorClient == null)
            {
                return new Dictionary<Guid, int>();
            }

            var request = new GetDoctorCountsBySpecialtyAndHospitalRequest
            {
                HospitalId = hospitalId.ToString()
            };
            request.SpecialtyIds.AddRange(specialtyIds.Select(x => x.ToString()));

            var response = await _dependencies.DoctorClient.GetDoctorCountsBySpecialtyAndHospitalAsync(request);

            var result = new Dictionary<Guid, int>();
            foreach (var count in response.SpecialtyCounts)
            {
                if (Guid.TryParse(count.Key, out var specialtyId))
                {
                    result[specialtyId] = count.Value;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get doctor counts for hospital {HospitalId}, returning empty counts", hospitalId);
            return new Dictionary<Guid, int>();
        }
    }

    #endregion

    #region Hospital Image Management

    public async Task<HospitalImageResponse?> AddHospitalImageAsync(CreateHospitalImageRequest request)
    {
        try
        {
            // Verify hospital exists
            var hospital = await _hospitalRepository.GetByIdAsync(request.HospitalId);
            if (hospital == null)
            {
                throw new HospitalNotFoundException(request.HospitalId);
            }

            var imageEntity = _mapper.Map<HospitalImageEntity>(request);
            var createdImage = await _hospitalImageRepository.CreateAsync(imageEntity);
            return _mapper.Map<HospitalImageResponse>(createdImage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding hospital image for hospital {HospitalId}", request.HospitalId);
            throw new HospitalOperationException($"Failed to add hospital image for hospital {request.HospitalId}", ex);
        }
    }

    public async Task<bool> DeleteHospitalImageAsync(Guid hospitalId, Guid imageId)
    {
        try
        {
            // Verify hospital exists
            var hospital = await _hospitalRepository.GetByIdAsync(hospitalId);
            if (hospital == null)
            {
                throw new HospitalNotFoundException(hospitalId);
            }

            // Verify image exists and belongs to hospital
            var image = await _hospitalImageRepository.GetByIdAsync(imageId);
            if (image == null || image.HospitalId != hospitalId)
            {
                return false;
            }

            return await _hospitalImageRepository.DeleteAsync(imageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hospital image {ImageId} for hospital {HospitalId}", imageId, hospitalId);
            throw new HospitalOperationException($"Failed to delete hospital image {imageId} for hospital {hospitalId}", ex);
        }
    }

    #endregion
}
