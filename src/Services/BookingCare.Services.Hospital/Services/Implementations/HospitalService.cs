using AutoMapper;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Doctor.Protos;
using Microsoft.Extensions.Caching.Memory;

namespace BookingCare.Services.Hospital.Services.Implementations;

public class HospitalService : IHospitalService
{
    private readonly IHospitalRepository _hospitalRepository;
    private readonly IMapper _mapper;
    private readonly AuthService.AuthServiceClient _authClient;
    private readonly ILocationApiService _locationApiService;
    private readonly DoctorService.DoctorServiceClient _doctorClient;
    private readonly ILogger<HospitalService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly object _circuitBreakerLock = new object();
    private static int _consecutiveFailures = 0;
    private static DateTime _lastFailureTime = DateTime.MinValue;
    private const int MaxFailures = 5;
    private const int CircuitBreakerTimeoutMinutes = 2;

    public HospitalService(
        IHospitalRepository hospitalRepository,
        IMapper mapper,
        AuthService.AuthServiceClient authClient,
        ILocationApiService locationApiService,
        DoctorService.DoctorServiceClient doctorClient,
        ILogger<HospitalService> logger,
        IMemoryCache cache)
    {
        _hospitalRepository = hospitalRepository;
        _mapper = mapper;
        _authClient = authClient;
        _locationApiService = locationApiService;
        _doctorClient = doctorClient;
        _logger = logger;
        _cache = cache;
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

        var locationFilteredHospitals = await _locationApiService.ApplyLocationFilteringAsync(
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

                    var response = await _doctorClient.GetSpecialtyByIdAsync(request);
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

    /// <summary>
    /// Get specialty with retry logic to handle rate limiting
    /// </summary>
    private async Task<SpecialtySimpleResponse?> GetSpecialtyWithRetryAsync(
        GetSpecialtyByIdRequest request,
        Guid specialtyId,
        Guid hospitalId,
        int maxRetries = 5)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await _doctorClient.GetSpecialtyByIdAsync(request);
            }
            catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable && attempt < maxRetries)
            {
                // Much longer delays for rate limiting: 1s, 2s, 4s, 8s, 16s
                var delay = Math.Pow(2, attempt) * 1000;
                _logger.LogWarning("gRPC call failed for specialty {SpecialtyId} (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms. Error: {Error}",
                    specialtyId, attempt, maxRetries, delay, ex.Message);
                await Task.Delay((int)delay);
            }
            catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable && attempt == maxRetries)
            {
                _logger.LogError("gRPC call failed for specialty {SpecialtyId} after {MaxRetries} attempts. Skipping this specialty. Error: {Error}",
                    specialtyId, maxRetries, ex.Message);
                return null; // Return null instead of throwing to continue processing other specialties
            }
            catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                _logger.LogWarning("Specialty {SpecialtyId} not found", specialtyId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving specialty {SpecialtyId}", specialtyId);
                return null;
            }
        }
        return null;
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
                var result = await _doctorClient.GetSpecialtiesByIdsAsync(request);

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
        // Collect all unique specialty IDs from all hospitals in this batch
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

        // Bulk fetch all specialties with caching
        Dictionary<Guid, SpecialtySimpleResponse> specialtyCache = new();
        if (allSpecialtyIds.Any())
        {
            try
            {
                // Check cache first
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

                // Fetch uncached specialties
                if (uncachedIds.Any())
                {
                    var bulkRequest = new GetSpecialtiesByIdsRequest();
                    bulkRequest.Ids.AddRange(uncachedIds.Select(id => id.ToString()));

                    var bulkResponse = await GetSpecialtiesBulkWithRetryAsync(bulkRequest);
                    if (bulkResponse?.Specialties != null)
                    {
                        foreach (var specialty in bulkResponse.Specialties)
                        {
                            if (Guid.TryParse(specialty.Id, out var specialtyId))
                            {
                                specialtyCache[specialtyId] = specialty;

                                // Cache for 30 minutes
                                var cacheKey = $"specialty_{specialtyId}";
                                var cacheOptions = new MemoryCacheEntryOptions
                                {
                                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                                    SlidingExpiration = TimeSpan.FromMinutes(10),
                                    Priority = CacheItemPriority.Normal
                                };
                                _cache.Set(cacheKey, specialty, cacheOptions);
                            }
                        }
                    }
                }

                _logger.LogInformation("Bulk retrieved {RetrievedCount}/{RequestedCount} specialties for batch (cached: {CachedCount}, fetched: {FetchedCount})",
                    specialtyCache.Count, allSpecialtyIds.Count, specialtyCache.Count - uncachedIds.Count, uncachedIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk specialty retrieval for batch");
            }
        }

        // Assign specialties to each hospital
        foreach (var hospitalResponse in hospitalBatch)
        {
            if (hospitalSpecialtyMap.TryGetValue(hospitalResponse.Id, out var specialtyIds))
            {
                if (specialtyIds.Any())
                {
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

                    hospitalResponse.Specialties = specialties;
                    hospitalResponse.TotalSpecialties = specialtyIds.Count; // Use original count from DB

                    _logger.LogDebug("Hospital {HospitalId} enriched with {RetrievedCount}/{TotalCount} specialties",
                        hospitalResponse.Id, specialties.Count, specialtyIds.Count);
                }
                else
                {
                    hospitalResponse.Specialties = new List<HospitalSpecialtyOptimizedResponse>();
                    hospitalResponse.TotalSpecialties = 0;
                    _logger.LogDebug("Hospital {HospitalId} has no specialties", hospitalResponse.Id);
                }
            }
        }
    }

    #endregion
}
