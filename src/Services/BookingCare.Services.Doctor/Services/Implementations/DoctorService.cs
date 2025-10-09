using AutoMapper;
using BookingCare.Services.Doctor.Exceptions;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Services.Doctor.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using BookingCare.Services.Favorite;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Review.Grpc;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Services;
using BookingCare.Services.Hospital;
using BookingCare.Services.Doctor.Models.ApiModels;

namespace BookingCare.Services.Doctor.Services.Implementations;

public class DoctorService : BaseService, IDoctorService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<IDoctorRepository> _repository;
    private readonly Lazy<IPositionRepository> _positionRepository;
    private readonly Lazy<ISpecialtyRepository> _specialtyRepository;
    private readonly Lazy<ILocationApiService> _locationApiService;
    private readonly Lazy<IMapper> _mapper;
    private readonly Lazy<FavoritesService.FavoritesServiceClient> _favoritesClient;
    private readonly Lazy<AuthService.AuthServiceClient> _authClient;
    private readonly Lazy<HospitalService.HospitalServiceClient> _hospitalClient;
    private readonly Lazy<ReviewService.ReviewServiceClient> _reviewClient;

    public DoctorService(
        IServiceProvider serviceProvider,
        ILogger<DoctorService> logger) : base(logger)
    {
        _serviceProvider = serviceProvider;
        _repository = new Lazy<IDoctorRepository>(() => _serviceProvider.GetRequiredService<IDoctorRepository>());
        _positionRepository = new Lazy<IPositionRepository>(() => _serviceProvider.GetRequiredService<IPositionRepository>());
        _specialtyRepository = new Lazy<ISpecialtyRepository>(() => _serviceProvider.GetRequiredService<ISpecialtyRepository>());
        _locationApiService = new Lazy<ILocationApiService>(() => _serviceProvider.GetRequiredService<ILocationApiService>());
        _mapper = new Lazy<IMapper>(() => _serviceProvider.GetRequiredService<IMapper>());
        _favoritesClient = new Lazy<FavoritesService.FavoritesServiceClient>(() => _serviceProvider.GetRequiredService<FavoritesService.FavoritesServiceClient>());
        _authClient = new Lazy<AuthService.AuthServiceClient>(() => _serviceProvider.GetRequiredService<AuthService.AuthServiceClient>());
        _hospitalClient = new Lazy<HospitalService.HospitalServiceClient>(() => _serviceProvider.GetRequiredService<HospitalService.HospitalServiceClient>());
        _reviewClient = new Lazy<ReviewService.ReviewServiceClient>(() => _serviceProvider.GetRequiredService<ReviewService.ReviewServiceClient>());
    }

    #region Doctor CRUD Operations

    /// <summary>
    /// Tạo doctor mới với giá cơ bản
    /// </summary>
    public async Task<DoctorResponse> CreateDoctorAsync(CreateDoctorRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            await ValidateCreateDoctorRequest(request);

            var doctor = CreateDoctorEntity(request);
            var createdDoctor = await _repository.Value.CreateDoctorAsync(doctor);

            await CreateDoctorPricesAsync(createdDoctor.Id, request.Prices);
            await CreateDoctorLanguagesAsync(createdDoctor.Id, request.LanguageIds);

            return _mapper.Value.Map<DoctorResponse>(createdDoctor);
        }, nameof(CreateDoctorAsync));
    }

    private async Task ValidateCreateDoctorRequest(CreateDoctorRequest request)
    {
        if (await _repository.Value.DoctorEmailExistsAsync(request.Email))
        {
            throw DoctorConflictException.WithEmail(request.Email);
        }

        if (await _repository.Value.DoctorAccountExistsAsync(request.AccountId))
        {
            throw DoctorConflictException.WithAccountId(request.AccountId);
        }

        if (request.PositionId.HasValue && !await _positionRepository.Value.PositionExistsAsync(request.PositionId.Value))
        {
            throw PositionNotFoundException.WithId(request.PositionId.Value);
        }

        // Validate SpecialtyId if provided
        if (request.SpecialtyId.HasValue && !await _specialtyRepository.Value.SpecialtyExistsAsync(request.SpecialtyId.Value))
        {
            throw new ArgumentException($"Specialty with ID {request.SpecialtyId.Value} not found");
        }
    }

    private DoctorEntity CreateDoctorEntity(CreateDoctorRequest request)
    {
        var doctor = _mapper.Value.Map<DoctorEntity>(request);
        doctor.Id = Guid.NewGuid();
        return doctor;
    }

    private async Task CreateDoctorPricesAsync(Guid doctorId, IEnumerable<DoctorPriceRequest>? prices)
    {
        if (prices == null || !prices.Any()) return;

        foreach (var priceRequest in prices)
        {
            await ValidateAndCreateDoctorPrice(doctorId, priceRequest);
        }
    }

    private async Task ValidateAndCreateDoctorPrice(Guid doctorId, DoctorPriceRequest priceRequest)
    {
        // Validate service type exists
        var serviceType = await _repository.Value.GetServiceTypeByIdAsync(priceRequest.ServiceTypeId);
        if (serviceType == null)
        {
            throw new ArgumentException($"Service type with ID {priceRequest.ServiceTypeId} not found");
        }

        // Check if doctor already has a price for this service type
        var existingPrices = await _repository.Value.GetDoctorPricesAsync(doctorId);
        if (existingPrices.Any(p => p.ServiceTypeId == priceRequest.ServiceTypeId))
        {
            throw new ArgumentException($"Doctor already has a price for service type {serviceType.Name}");
        }

        var doctorPrice = new DoctorPriceEntity
        {
            Id = Guid.NewGuid(),
            DoctorId = doctorId,
            ServiceTypeId = priceRequest.ServiceTypeId,
            Amount = priceRequest.Amount
        };
        await _repository.Value.CreateDoctorPriceAsync(doctorPrice);
    }

    private async Task CreateDoctorLanguagesAsync(Guid doctorId, IEnumerable<Guid>? languageIds)
    {
        if (languageIds == null || !languageIds.Any()) return;

        foreach (var languageId in languageIds)
        {
            await ValidateAndCreateDoctorLanguage(doctorId, languageId);
        }
    }

    private async Task ValidateAndCreateDoctorLanguage(Guid doctorId, Guid languageId)
    {
        // Validate language exists
        var language = await _repository.Value.GetLanguageByIdAsync(languageId);
        if (language == null)
        {
            throw new ArgumentException($"Language with ID {languageId} not found");
        }

        // Check if doctor already has this language
        var existingLanguages = await _repository.Value.GetDoctorLanguagesAsync(doctorId);
        if (existingLanguages.Any(l => l.LanguageId == languageId))
        {
            throw new ArgumentException($"Doctor already has language {language.Name}");
        }

        var doctorLanguage = new DoctorLanguageEntity
        {
            Id = Guid.NewGuid(),
            DoctorId = doctorId,
            LanguageId = languageId
        };
        await _repository.Value.CreateDoctorLanguageAsync(doctorLanguage);
    }

    public async Task<DoctorByIdResponse?> GetDoctorByIdAsync(Guid id)
    {
        var doctor = await _repository.Value.GetDoctorByIdAsync(id);
        if (doctor == null) return null;

        // Include Position và Specialty
        await IncludePositionAndSpecialtyAsync(doctor);

        var response = _mapper.Value.Map<DoctorByIdResponse>(doctor);

        // Create parallel tasks for enrichment
        var enrichmentTasks = new List<Task>();

        // Task 1: Enrich with hospital basic info
        var hospitalTask = EnrichDoctorByIdWithHospitalInfoAsync(response, doctor.HospitalId);
        enrichmentTasks.Add(hospitalTask);

        // Task 2: Enrich with review statistics
        var reviewTask = EnrichDoctorByIdWithReviewStatisticsAsync(response);
        enrichmentTasks.Add(reviewTask);

        // Execute all enrichment tasks in parallel
        await Task.WhenAll(enrichmentTasks);

        return response;
    }

    public async Task<DoctorResponse?> GetDoctorByEmailAsync(string email)
    {
        var doctor = await _repository.Value.GetDoctorByEmailAsync(email);
        if (doctor == null) return null;

        // Include Position và Specialty
        await IncludePositionAndSpecialtyAsync(doctor);

        var response = _mapper.Value.Map<DoctorResponse>(doctor);

        // Enrich with status and review statistics
        await EnrichSingleDoctorAsync(response);

        return response;
    }

    public async Task<DoctorResponse?> GetDoctorByAccountIdAsync(Guid accountId)
    {
        var doctor = await _repository.Value.GetDoctorByAccountIdAsync(accountId);
        if (doctor == null) return null;

        // Include Position và Specialty
        await IncludePositionAndSpecialtyAsync(doctor);

        var response = _mapper.Value.Map<DoctorResponse>(doctor);

        // Enrich with status and review statistics
        await EnrichSingleDoctorAsync(response);

        return response;
    }

    /// <summary>
    /// Cập nhật thông tin doctor
    /// </summary>
    public async Task<DoctorResponse> UpdateDoctorAsync(UpdateDoctorRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var existingDoctor = await ValidateAndGetExistingDoctor(request.Id);

            await ValidateUpdateDoctorRequest(request);

            UpdateDoctorEntity(existingDoctor, request);

            await UpdateDoctorPricesAsync(existingDoctor.Id, request.Prices);
            await UpdateDoctorLanguagesAsync(existingDoctor.Id, request.LanguageIds);

            var updatedDoctor = await _repository.Value.UpdateDoctorAsync(existingDoctor);
            return _mapper.Value.Map<DoctorResponse>(updatedDoctor);
        }, nameof(UpdateDoctorAsync));
    }

    private async Task<DoctorEntity> ValidateAndGetExistingDoctor(Guid id)
    {
        var existingDoctor = await _repository.Value.GetDoctorByIdAsync(id);
        if (existingDoctor == null)
        {
            throw DoctorNotFoundException.WithId(id);
        }
        return existingDoctor;
    }

    private async Task ValidateUpdateDoctorRequest(UpdateDoctorRequest request)
    {
        if (request.PositionId.HasValue && !await _positionRepository.Value.PositionExistsAsync(request.PositionId.Value))
        {
            throw PositionNotFoundException.WithId(request.PositionId.Value);
        }

        // Validate SpecialtyId if provided
        if (request.SpecialtyId.HasValue && !await _specialtyRepository.Value.SpecialtyExistsAsync(request.SpecialtyId.Value))
        {
            throw new ArgumentException($"Specialty with ID {request.SpecialtyId.Value} not found");
        }
    }

    private void UpdateDoctorEntity(DoctorEntity existingDoctor, UpdateDoctorRequest request)
    {
        _mapper.Value.Map(request, existingDoctor);
        existingDoctor.UpdatedAt = DateTime.UtcNow;
    }

    private async Task UpdateDoctorPricesAsync(Guid doctorId, IEnumerable<DoctorPriceRequest>? prices)
    {
        if (prices == null || !prices.Any())
        {
            // If no prices provided, delete all existing prices
            await _repository.Value.DeleteAllDoctorPricesAsync(doctorId);
            return;
        }

        // Get existing prices
        var existingPrices = await _repository.Value.GetDoctorPricesAsync(doctorId);
        var existingPriceMap = existingPrices.ToDictionary(p => p.ServiceTypeId, p => p);

        var newPriceMap = prices.ToDictionary(p => p.ServiceTypeId, p => p);

        // Delete prices that are no longer in the request
        var pricesToDelete = existingPriceMap.Keys.Except(newPriceMap.Keys).ToList();
        foreach (var serviceTypeId in pricesToDelete)
        {
            var priceToDelete = existingPriceMap[serviceTypeId];
            await _repository.Value.DeleteDoctorPriceAsync(doctorId, priceToDelete.Id);
        }

        // Update existing prices or create new ones
        foreach (var priceRequest in prices)
        {
            if (existingPriceMap.TryGetValue(priceRequest.ServiceTypeId, out var existingPrice))
            {
                // Update existing price
                existingPrice.Amount = priceRequest.Amount;
                await _repository.Value.UpdateDoctorPriceAsync(existingPrice);
            }
            else
            {
                // Create new price
                await ValidateAndCreateDoctorPrice(doctorId, priceRequest);
            }
        }
    }

    private async Task UpdateDoctorLanguagesAsync(Guid doctorId, IEnumerable<Guid>? languageIds)
    {
        // Get existing languages
        var existingLanguages = await _repository.Value.GetDoctorLanguagesAsync(doctorId);
        var existingLanguageIds = existingLanguages.Select(l => l.LanguageId).ToHashSet();

        var newLanguageIds = languageIds?.ToHashSet() ?? new HashSet<Guid>();

        // Delete languages that are no longer in the request
        var languagesToDelete = existingLanguageIds.Except(newLanguageIds).ToList();
        foreach (var languageId in languagesToDelete)
        {
            await _repository.Value.DeleteDoctorLanguageAsync(doctorId, languageId);
        }

        // Add new languages that are not in existing
        var languagesToAdd = newLanguageIds.Except(existingLanguageIds).ToList();
        foreach (var languageId in languagesToAdd)
        {
            await ValidateAndCreateDoctorLanguage(doctorId, languageId);
        }
    }

    public async Task<bool> DeleteDoctorAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            return await _repository.Value.DeleteDoctorAsync(id);
        }, nameof(DeleteDoctorAsync));
    }

    public async Task<bool> ToggleDoctorStatusAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var doctor = await _repository.Value.GetDoctorByIdAsync(id);
            if (doctor == null)
            {
                throw DoctorNotFoundException.WithId(id);
            }

            // Note: Doctor status is managed by Auth service, not by Doctor service
            // This method is kept for API compatibility but doesn't actually toggle status
            // The actual status toggle should be handled by calling Auth service
            Logger.LogWarning("ToggleDoctorStatusAsync called but doctor status is managed by Auth service. Doctor ID: {DoctorId}", id);

            return true;
        }, nameof(ToggleDoctorStatusAsync));
    }

    #endregion

    #region Doctor Query Operations

    public async Task<DoctorListResponse> GetDoctorsAsync(DoctorQueryRequest query)
    {
        var (doctors, totalCount) = await _repository.Value.GetDoctorsAsync(query);

        // Enrich with Position và Specialty
        await EnrichDoctorsWithPositionAndSpecialtyAsync(doctors);

        var response = _mapper.Value.Map<DoctorListResponse>((doctors, totalCount));

        // Set pagination info
        response.PageNumber = query.PageNumber;
        response.PageSize = query.PageSize;
        response.TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        // Enrich with status and review statistics
        await EnrichDoctorListAsync(response.Doctors);

        // Apply location filtering if needed
        if (!string.IsNullOrEmpty(query.ProvinceId) || !string.IsNullOrEmpty(query.DistrictId))
        {
            response.Doctors = await _locationApiService.Value.ApplyLocationFilteringAsync(response.Doctors, query.ProvinceId, query.DistrictId);
            // Update total count and pages after location filtering
            response.TotalCount = response.Doctors.Count;
            response.TotalPages = (int)Math.Ceiling((double)response.TotalCount / query.PageSize);

            // Ensure page number is valid after filtering
            if (response.PageNumber > response.TotalPages && response.TotalPages > 0)
            {
                response.PageNumber = response.TotalPages;
            }
        }

        // Apply rating filtering after getting review statistics
        if (query.MinRating.HasValue || (query.MinRatings != null && query.MinRatings.Any()))
        {
            Console.WriteLine($"Before rating filtering: {response.Doctors.Count} doctors");
            Console.WriteLine($"MinRating: {query.MinRating}, MinRatings: {string.Join(", ", query.MinRatings ?? new List<double>())}");

            response.Doctors = FilterDoctorsByRating(response.Doctors, query.MinRating, query.MinRatings);
            response.TotalCount = response.Doctors.Count;
            response.TotalPages = (int)Math.Ceiling((double)response.TotalCount / query.PageSize);

            Console.WriteLine($"After rating filtering: {response.Doctors.Count} doctors");
            Console.WriteLine($"TotalCount: {response.TotalCount}, TotalPages: {response.TotalPages}, PageNumber: {response.PageNumber}");

            // Ensure page number is valid after filtering
            if (response.PageNumber > response.TotalPages && response.TotalPages > 0)
            {
                response.PageNumber = response.TotalPages;
            }
        }

        // Note: Status filtering should be done at database level in repository
        // If needed, implement it in the repository query instead of here

        return response;
    }

    public async Task<DoctorListResponse> FilterDoctorsAsync(DoctorAdvancedFilterRequest filter)
    {
        try
        {
            Console.WriteLine($"FilterDoctorsAsync called with ExperienceRanges: {filter.ExperienceRanges?.Count ?? 0}");
            Console.WriteLine($"FilterDoctorsAsync called with ProvinceId: {filter.ProvinceId}, DistrictId: {filter.DistrictId}");
            if (filter.ExperienceRanges != null)
            {
                foreach (var range in filter.ExperienceRanges)
                {
                    Console.WriteLine($"ExperienceRange: MinYears={range.MinYears}, MaxYears={range.MaxYears}");
                }
            }

            // Convert advanced filter to basic query
            var query = ConvertAdvancedFilterToQuery(filter);

            return await GetDoctorsAsync(query);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in FilterDoctorsAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Optimized filter doctors method - uses filter logic but returns only necessary fields
    /// </summary>
    public async Task<DoctorSearchListResponse> FilterDoctorsOptimizedAsync(DoctorAdvancedFilterRequest filter)
    {
        try
        {
            Console.WriteLine($"FilterDoctorsOptimizedAsync called with filter:");
            Console.WriteLine($"- SpecialtyId: {filter.SpecialtyId}");
            Console.WriteLine($"- PositionId: {filter.PositionId}");
            Console.WriteLine($"- Gender: {filter.Gender}");
            Console.WriteLine($"- MinYearsOfExperience: {filter.MinYearsOfExperience}");
            Console.WriteLine($"- MaxYearsOfExperience: {filter.MaxYearsOfExperience}");
            Console.WriteLine($"- ProvinceId: {filter.ProvinceId}");
            Console.WriteLine($"- DistrictId: {filter.DistrictId}");
            Console.WriteLine($"- Address: {filter.Address}");

            // Convert advanced filter to basic query
            var query = ConvertAdvancedFilterToQuery(filter);

            // Use optimized repository method for complex filtering
            var (doctors, totalCount) = await _repository.Value.GetDoctorsForComplexFilterAsync(query);

            Console.WriteLine($"Repository returned {doctors.Count} doctors, totalCount: {totalCount}");

            // Build hospitalId map BEFORE mapping to DTO
            var hospitalIdMap = doctors.Where(d => d.HospitalId.HasValue)
                                       .ToDictionary(d => d.Id, d => d.HospitalId!.Value);

            // Map to optimized response DTOs
            var mappedDoctors = _mapper.Value.Map<List<DoctorSearchForPatientResponse>>(doctors);

            // Create parallel tasks for enrichment
            var enrichmentTasks = new List<Task>();

            // Task 1: Enrich with hospital info
            var hospitalTask = EnrichDoctorSearchWithHospitalInfoAsync(mappedDoctors, hospitalIdMap);
            enrichmentTasks.Add(hospitalTask);

            // Task 2: Enrich with review statistics
            var reviewTask = EnrichDoctorSearchWithReviewStatisticsAsync(mappedDoctors);
            enrichmentTasks.Add(reviewTask);

            // Execute enrichment tasks in parallel
            await Task.WhenAll(enrichmentTasks);

            // Apply location filtering if needed
            if (!string.IsNullOrEmpty(query.ProvinceId) || !string.IsNullOrEmpty(query.DistrictId))
            {
                Console.WriteLine($"Applying location filtering - ProvinceId: {query.ProvinceId}, DistrictId: {query.DistrictId}");
                var filteredDoctors = await ApplyLocationFilteringForSearchAsync(mappedDoctors, query.ProvinceId, query.DistrictId);
                mappedDoctors = filteredDoctors;
                totalCount = mappedDoctors.Count;
                Console.WriteLine($"After location filtering: {mappedDoctors.Count} doctors");
            }

            // Apply rating filtering after getting review statistics
            if (query.MinRating.HasValue || (query.MinRatings != null && query.MinRatings.Any()))
            {
                Console.WriteLine($"Before rating filtering: {mappedDoctors.Count} doctors");
                mappedDoctors = FilterDoctorsByRatingForSearch(mappedDoctors, query.MinRating, query.MinRatings);
                totalCount = mappedDoctors.Count;
                Console.WriteLine($"After rating filtering: {mappedDoctors.Count} doctors");
            }

            // Calculate pagination
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            return new DoctorSearchListResponse
            {
                Doctors = mappedDoctors,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in FilterDoctorsOptimizedAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<List<DoctorResponse>> GetDoctorsByHospitalAsync(Guid hospitalId)
    {
        var doctors = await _repository.Value.GetDoctorsByHospitalAsync(hospitalId);
        var response = _mapper.Value.Map<List<DoctorResponse>>(doctors);

        // Enrich with status, review statistics, and hospital info
        await EnrichDoctorListAsync(response);

        return response;
    }

    public async Task<List<DoctorResponse>> GetDoctorsBySpecialtyAsync(Guid specialtyId)
    {
        var doctors = await _repository.Value.GetDoctorsBySpecialtyAsync(specialtyId);
        var response = _mapper.Value.Map<List<DoctorResponse>>(doctors);

        // Enrich with status, review statistics, and hospital info
        await EnrichDoctorListAsync(response);

        return response;
    }

    public async Task<List<DoctorResponse>> GetDoctorsByPositionAsync(Guid positionId)
    {
        var doctors = await _repository.Value.GetDoctorsByPositionAsync(positionId);
        var response = _mapper.Value.Map<List<DoctorResponse>>(doctors);

        // Enrich with status, review statistics, and hospital info
        await EnrichDoctorListAsync(response);

        return response;
    }

    public async Task<List<DoctorResponse>> GetActiveDoctorsAsync()
    {
        var doctors = await _repository.Value.GetActiveDoctorsAsync();
        var response = _mapper.Value.Map<List<DoctorResponse>>(doctors);

        // Enrich with status, review statistics, and hospital info
        await EnrichDoctorListAsync(response);

        return response;
    }

    public async Task<List<DoctorBasicInfoResponse>> GetDoctorsByAccountIdsAsync(IEnumerable<Guid> accountIds)
    {
        var doctors = await _repository.Value.GetDoctorsByAccountIdsAsync(accountIds);
        var result = new List<DoctorBasicInfoResponse>();
        foreach (var d in doctors)
        {
            result.Add(new DoctorBasicInfoResponse
            {
                AccountId = d.AccountId,
                Email = d.Email,
                FullName = $"{d.FirstName} {d.LastName}".Trim(),
                AvatarUrl = d.AvatarUrl ?? string.Empty
            });
        }
        return result;
    }

    public async Task<DoctorListResponse> GetPatientFavoriteDoctorsAsync(Guid patientId, int page = 1, int pageSize = 9, string? searchTerm = null)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetPatientFavoriteDoctorsWithSearchAsync(patientId, searchTerm);
            }

            return await GetPatientFavoriteDoctorsPaginatedAsync(patientId, page, pageSize);
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Favorites gRPC GetPatientFavorites failed for patient {PatientId}", patientId);
            return CreateEmptyDoctorListResponse(page, pageSize, searchTerm);
        }
    }

    private async Task<DoctorListResponse> GetPatientFavoriteDoctorsWithSearchAsync(Guid patientId, string searchTerm)
    {
        var totalFavorites = await GetTotalFavoriteCount(patientId);
        if (totalFavorites == 0)
        {
            return CreateEmptyDoctorListResponse(1, 0, searchTerm);
        }

        var allDoctorIds = await GetAllFavoriteDoctorIds(patientId, totalFavorites);
        var doctors = await _repository.Value.GetDoctorsByIdsAsync(allDoctorIds);

        var filteredDoctors = FilterDoctorsBySearchTerm(doctors, searchTerm);
        var mappedDoctors = _mapper.Value.Map<List<DoctorResponse>>(filteredDoctors);

        SetFavoriteStatus(mappedDoctors, allDoctorIds);
        await EnrichDoctorListAsync(mappedDoctors);

        return new DoctorListResponse
        {
            Doctors = mappedDoctors,
            TotalCount = mappedDoctors.Count,
            PageNumber = 1,
            PageSize = mappedDoctors.Count,
            TotalPages = mappedDoctors.Count == 0 ? 0 : 1
        };
    }

    private async Task<DoctorListResponse> GetPatientFavoriteDoctorsPaginatedAsync(Guid patientId, int page, int pageSize)
    {
        var request = new GetPatientFavoritesRequest
        {
            PatientId = patientId.ToString(),
            Page = page,
            PageSize = pageSize
        };

        var response = await _favoritesClient.Value.GetPatientFavoritesAsync(request);
        var doctorIds = response.Items.Select(i => Guid.Parse(i.DoctorId)).ToList();

        if (!doctorIds.Any())
        {
            return CreateEmptyDoctorListResponse(page, pageSize, null);
        }

        var pageDoctors = await _repository.Value.GetDoctorsByIdsAsync(doctorIds);
        var mappedPage = _mapper.Value.Map<List<DoctorResponse>>(pageDoctors);

        SetFavoriteStatus(mappedPage, doctorIds);
        await EnrichDoctorListAsync(mappedPage);

        var totalCount = response.TotalCount;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new DoctorListResponse
        {
            Doctors = mappedPage,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private async Task<int> GetTotalFavoriteCount(Guid patientId)
    {
        var countReq = new GetPatientFavoriteCountRequest { PatientId = patientId.ToString() };
        var countRes = await _favoritesClient.Value.GetPatientFavoriteCountAsync(countReq);
        return (int)countRes.FavoriteCount;
    }

    private async Task<List<Guid>> GetAllFavoriteDoctorIds(Guid patientId, int totalFavorites)
    {
        var allReq = new GetPatientFavoritesRequest
        {
            PatientId = patientId.ToString(),
            Page = 1,
            PageSize = totalFavorites
        };
        var allRes = await _favoritesClient.Value.GetPatientFavoritesAsync(allReq);
        return allRes.Items.Select(i => Guid.Parse(i.DoctorId)).ToList();
    }

    private List<DoctorEntity> FilterDoctorsBySearchTerm(List<DoctorEntity> doctors, string searchTerm)
    {
        var searchLower = searchTerm.ToLower();
        return doctors.Where(d =>
            d.FirstName.ToLower().Contains(searchLower) ||
            d.LastName.ToLower().Contains(searchLower) ||
            (d.FirstName + " " + d.LastName).ToLower().Contains(searchLower)
        ).ToList();
    }

    private void SetFavoriteStatus(List<DoctorResponse> doctors, List<Guid> favoriteIds)
    {
        var favoritedSet = favoriteIds.ToHashSet();
        foreach (var doctor in doctors)
        {
            doctor.IsFavorited = favoritedSet.Contains(doctor.Id);
        }
    }

    private DoctorListResponse CreateEmptyDoctorListResponse(int page, int pageSize, string? searchTerm)
    {
        return new DoctorListResponse
        {
            Doctors = new List<DoctorResponse>(),
            TotalCount = 0,
            PageNumber = string.IsNullOrWhiteSpace(searchTerm) ? page : 1,
            PageSize = string.IsNullOrWhiteSpace(searchTerm) ? pageSize : 0,
            TotalPages = 0
        };
    }

    public async Task<DoctorListResponse> GetDoctorsWithFavoriteStatusAsync(DoctorQueryRequest query, Guid patientId)
    {
        var baseList = await GetDoctorsAsync(query);
        if (baseList.Doctors.Count == 0) return baseList;

        try
        {
            var request = new CheckMultipleFavoritesRequest
            {
                PatientId = patientId.ToString()
            };
            request.DoctorIds.AddRange(baseList.Doctors.Select(d => d.Id.ToString()));

            var check = await _favoritesClient.Value.CheckMultipleFavoritesAsync(request);
            var favorited = check.FavoritedDoctorIds.Select(Guid.Parse).ToHashSet();

            foreach (var doc in baseList.Doctors)
            {
                doc.IsFavorited = favorited.Contains(doc.Id);
            }
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Favorites gRPC CheckMultipleFavorites failed for patient {PatientId}", patientId);
            // Favorites service unavailable; proceed with IsFavorited default false
        }
        return baseList;
    }

    #endregion

    #region DoctorPrice Operations

    public async Task<List<DoctorPriceResponse>> GetDoctorPricesAsync(Guid doctorId)
    {
        var prices = await _repository.Value.GetDoctorPricesAsync(doctorId);
        return _mapper.Value.Map<List<DoctorPriceResponse>>(prices);
    }

    public async Task<DoctorPriceResponse> AssignPriceToDoctorAsync(AssignPriceToDoctorRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Validate doctor exists
            if (!await _repository.Value.DoctorExistsAsync(request.DoctorId))
            {
                throw DoctorNotFoundException.WithId(request.DoctorId);
            }

            // Get doctor
            var doctor = await _repository.Value.GetDoctorByIdAsync(request.DoctorId);
            if (doctor == null)
            {
                throw DoctorNotFoundException.WithId(request.DoctorId);
            }

            // Create doctor-price relationship
            var doctorPrice = new DoctorPriceEntity
            {
                Id = Guid.NewGuid(),
                DoctorId = request.DoctorId,
                Amount = request.Amount
            };

            var createdDoctorPrice = await _repository.Value.CreateDoctorPriceAsync(doctorPrice);
            return _mapper.Value.Map<DoctorPriceResponse>(createdDoctorPrice);
        }, nameof(AssignPriceToDoctorAsync));
    }

    public async Task<bool> RemovePriceFromDoctorAsync(Guid doctorId, Guid priceId)
    {
        return await _repository.Value.DeleteDoctorPriceAsync(doctorId, priceId);
    }

    #endregion

    #region Validation Operations

    public async Task<bool> DoctorExistsAsync(Guid id)
    {
        return await _repository.Value.DoctorExistsAsync(id);
    }

    public async Task<bool> DoctorEmailExistsAsync(string email, Guid? excludeId = null)
    {
        return await _repository.Value.DoctorEmailExistsAsync(email, excludeId);
    }

    public async Task<bool> DoctorAccountExistsAsync(Guid accountId, Guid? excludeId = null)
    {
        return await _repository.Value.DoctorAccountExistsAsync(accountId, excludeId);
    }

    public async Task<bool> DoctorPriceExistsAsync(Guid doctorId, Guid priceId)
    {
        return await _repository.Value.DoctorPriceExistsAsync(doctorId, priceId);
    }

    #endregion

    #region Helper Methods

    public IQueryable<DoctorEntity> GetQueryableDoctors()
    {
        return _repository.Value.GetQueryableDoctors();
    }

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

            var response = await _authClient.Value.GetAccountStatusByIdsAsync(request);

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
            Logger.LogWarning(ex, "Auth gRPC GetAccountStatusByIds failed");
            // Return empty dictionary on failure
        }

        return statusMap;
    }

    /// <summary>
    /// Enrich doctor responses with account status
    /// </summary>
    private async Task EnrichDoctorsWithStatusAsync(List<DoctorResponse> doctors)
    {
        if (!doctors.Any()) return;

        var accountIds = doctors.Select(d => d.AccountId).Distinct();
        var statusMap = await GetAccountStatusesAsync(accountIds);

        foreach (var doctor in doctors)
        {
            if (statusMap.TryGetValue(doctor.AccountId, out var status))
            {
                doctor.Status = status;
            }
            else
            {
                // Nếu không tìm thấy status từ Auth service, set mặc định là ACTIVE
                doctor.Status = Status.ACTIVE;
            }
        }
    }

    #region Hospital Enrichment Methods

    /// <summary>
    /// Enrich doctors with basic hospital information (Id, Name, Address)
    /// </summary>
    private async Task EnrichDoctorsWithHospitalBasicInfoAsync(List<DoctorResponse> doctors)
    {
        if (!doctors.Any()) return;

        var hospitalIds = doctors.Where(d => d.HospitalId.HasValue)
                                 .Select(d => d.HospitalId!.Value)
                                 .Distinct()
                                 .ToList();

        if (!hospitalIds.Any()) return;

        try
        {
            var hospitalInfoMap = await GetHospitalBasicInfoMapAsync(hospitalIds);

            foreach (var doctor in doctors.Where(d => d.HospitalId.HasValue))
            {
                if (hospitalInfoMap.TryGetValue(doctor.HospitalId!.Value, out var hospitalInfo))
                {
                    doctor.Hospital = hospitalInfo;
                }
            }
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Hospital gRPC GetHospitalsList failed");
            // Continue without hospital info on failure
        }
    }

    /// <summary>
    /// Enrich single doctor with detailed hospital information
    /// </summary>
    private async Task EnrichDoctorWithHospitalDetailInfoAsync(DoctorDetailResponse doctor)
    {
        if (!doctor.HospitalId.HasValue) return;

        try
        {
            var request = new GetHospitalRequest
            {
                Id = doctor.HospitalId.Value.ToString()
            };

            var hospitalResponse = await _hospitalClient.Value.GetHospitalAsync(request);

            doctor.Hospital = new HospitalDetailInfo
            {
                Id = Guid.Parse(hospitalResponse.Id),
                AccountId = Guid.Parse(hospitalResponse.AccountId),
                Name = hospitalResponse.Name,
                Address = hospitalResponse.Address,
                Phone = hospitalResponse.Phone,
                Email = hospitalResponse.Email,
                Description = hospitalResponse.Description,
                BackgroundUrl = hospitalResponse.BackgroundUrl,
                AvatarUrl = hospitalResponse.AvatarUrl,
                Status = hospitalResponse.Status,
                CreatedAt = DateTime.Parse(hospitalResponse.CreatedAt),
                UpdatedAt = DateTime.Parse(hospitalResponse.UpdatedAt)
            };
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Hospital gRPC GetHospital failed for hospital {HospitalId}", doctor.HospitalId);
            // Continue without hospital info on failure
        }
    }

    /// <summary>
    /// Get hospital basic info map from Hospital service - OPTIMIZED
    /// </summary>
    private async Task<Dictionary<Guid, HospitalBasicInfo>> GetHospitalBasicInfoMapAsync(List<Guid> hospitalIds)
    {
        var hospitalMap = new Dictionary<Guid, HospitalBasicInfo>();

        if (!hospitalIds.Any()) return hospitalMap;

        try
        {
            // OPTIMIZATION: Use GetHospitalsBasicInfo method which supports filtering by specific IDs
            var request = new GetHospitalsBasicInfoRequest();
            request.Ids.AddRange(hospitalIds.Select(id => id.ToString()));

            var response = await _hospitalClient.Value.GetHospitalsBasicInfoAsync(request);

            foreach (var hospital in response.Hospitals)
            {
                hospitalMap[Guid.Parse(hospital.Id)] = new HospitalBasicInfo
                {
                    Id = Guid.Parse(hospital.Id),
                    Name = hospital.Name,
                    Address = hospital.Address
                };
            }
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Hospital gRPC GetHospitalsBasicInfo failed");
            // Return empty map on failure
        }

        return hospitalMap;
    }


    #endregion

    /// <summary>
    /// Enrich doctor entities with Position và Specialty
    /// </summary>
    private async Task EnrichDoctorsWithPositionAndSpecialtyAsync(List<DoctorEntity> doctors)
    {
        if (!doctors.Any()) return;

        var positionIds = doctors.Where(d => d.PositionId.HasValue).Select(d => d.PositionId!.Value).Distinct().ToList();
        var specialtyIds = doctors.Where(d => d.SpecialtyId.HasValue).Select(d => d.SpecialtyId!.Value).Distinct().ToList();

        var positions = new Dictionary<Guid, PositionEntity>();
        var specialties = new Dictionary<Guid, SpecialtyEntity>();

        if (positionIds.Any())
        {
            var positionList = await _positionRepository.Value.GetPositionsByIdsAsync(positionIds);
            positions = positionList.ToDictionary(p => p.Id);
        }

        if (specialtyIds.Any())
        {
            var specialtyList = await _specialtyRepository.Value.GetSpecialtiesByIdsAsync(specialtyIds);
            specialties = specialtyList.ToDictionary(s => s.Id);
        }

        foreach (var doctor in doctors)
        {
            if (doctor.PositionId.HasValue && positions.TryGetValue(doctor.PositionId.Value, out var position))
            {
                doctor.Position = position;
            }
            if (doctor.SpecialtyId.HasValue && specialties.TryGetValue(doctor.SpecialtyId.Value, out var specialty))
            {
                doctor.Specialty = specialty;
            }
        }
    }

    private async Task UpdateDoctorPriceAsync(DoctorEntity doctor, decimal amount)
    {
        // Tìm giá hiện tại của doctor
        var existingPrices = await _repository.Value.GetDoctorPricesAsync(doctor.Id);
        var existingPrice = existingPrices.FirstOrDefault();

        if (existingPrice != null)
        {
            // Cập nhật giá hiện tại
            existingPrice.Amount = amount;
            existingPrice.UpdatedAt = DateTime.UtcNow;
            await _repository.Value.UpdateDoctorPriceAsync(existingPrice);
        }
        else
        {
            // Tạo mới giá
            var doctorPrice = new DoctorPriceEntity
            {
                Id = Guid.NewGuid(),
                DoctorId = doctor.Id,
                Amount = amount
            };
            await _repository.Value.CreateDoctorPriceAsync(doctorPrice);
        }
    }

    /// <summary>
    /// Include Position và Specialty for a single doctor
    /// </summary>
    private async Task IncludePositionAndSpecialtyAsync(DoctorEntity doctor)
    {
        var tasks = new List<Task>();

        if (doctor.PositionId.HasValue)
        {
            var positionTask = _positionRepository.Value.GetPositionByIdAsync(doctor.PositionId.Value)
                .ContinueWith(t => doctor.Position = t.Result);
            tasks.Add(positionTask);
        }

        if (doctor.SpecialtyId.HasValue)
        {
            var specialtyTask = _specialtyRepository.Value.GetSpecialtyByIdAsync(doctor.SpecialtyId.Value)
                .ContinueWith(t => doctor.Specialty = t.Result);
            tasks.Add(specialtyTask);
        }

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Enrich single doctor with status and review statistics
    /// </summary>
    private async Task EnrichSingleDoctorAsync(DoctorResponse doctor)
    {
        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(new List<DoctorResponse> { doctor });

        // Enrich with review statistics
        await EnrichDoctorWithReviewStatisticsAsync(doctor);
    }

    /// <summary>
    /// Enrich doctor list with status, review statistics, and hospital info
    /// </summary>
    private async Task EnrichDoctorListAsync(List<DoctorResponse> doctors)
    {
        // Create parallel tasks for enrichment
        var enrichmentTasks = new List<Task>();

        // Task 1: Enrich with account status
        var statusTask = EnrichDoctorsWithStatusAsync(doctors);
        enrichmentTasks.Add(statusTask);

        // Task 2: Enrich with review statistics
        var reviewTask = EnrichDoctorsWithReviewStatisticsAsync(doctors);
        enrichmentTasks.Add(reviewTask);

        // Task 3: Enrich with hospital basic info
        var hospitalTask = EnrichDoctorsWithHospitalBasicInfoAsync(doctors);
        enrichmentTasks.Add(hospitalTask);

        // Execute all enrichment tasks in parallel
        await Task.WhenAll(enrichmentTasks);
    }

    /// <summary>
    /// Enrich single doctor with review statistics
    /// </summary>
    private async Task EnrichDoctorWithReviewStatisticsAsync(DoctorResponse doctor)
    {
        try
        {
            var request = new GetDoctorStatisticsRequest
            {
                DoctorId = doctor.Id.ToString()
            };

            var response = await _reviewClient.Value.GetDoctorDetailedStatisticsAsync(request);

            doctor.ReviewStatistics = new DoctorReviewStatistics
            {
                AverageRating = response.AverageRating,
                TotalReviews = response.TotalReviews,
                RatingDistribution = response.RatingDistribution.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                )
            };
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Review gRPC GetDoctorDetailedStatistics failed for doctor {DoctorId}", doctor.Id);
            // Set default values on failure
            doctor.ReviewStatistics = new DoctorReviewStatistics
            {
                AverageRating = 0.0,
                TotalReviews = 0,
                RatingDistribution = new Dictionary<int, long>()
            };
        }
    }

    /// <summary>
    /// Enrich multiple doctors with review statistics (batch processing)
    /// </summary>
    private async Task EnrichDoctorsWithReviewStatisticsAsync(List<DoctorResponse> doctors)
    {
        if (!doctors.Any()) return;

        try
        {
            var request = new BatchDoctorsStatisticsRequest();
            request.DoctorIds.AddRange(doctors.Select(d => d.Id.ToString()));

            var response = await _reviewClient.Value.GetBatchDoctorsStatisticsAsync(request);

            foreach (var doctor in doctors)
            {
                if (response.DoctorStatistics.TryGetValue(doctor.Id.ToString(), out var stats))
                {
                    doctor.ReviewStatistics = new DoctorReviewStatisticsBasic
                    {
                        AverageRating = stats.AverageRating,
                        TotalReviews = stats.TotalReviews
                    };
                }
                else
                {
                    // Set default values if no statistics found
                    doctor.ReviewStatistics = new DoctorReviewStatisticsBasic
                    {
                        AverageRating = 0.0,
                        TotalReviews = 0
                    };
                }
            }
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Review gRPC GetBatchDoctorsStatistics failed");
            // Set default values for all doctors on failure
            foreach (var doctor in doctors)
            {
                doctor.ReviewStatistics = new DoctorReviewStatisticsBasic
                {
                    AverageRating = 0.0,
                    TotalReviews = 0
                };
            }
        }
    }

    #endregion

    #region Location and Rating Filtering for Search

    /// <summary>
    /// Apply location filtering to doctor search results
    /// </summary>
    private async Task<List<DoctorSearchForPatientResponse>> ApplyLocationFilteringForSearchAsync(
        List<DoctorSearchForPatientResponse> doctors,
        string? provinceId,
        string? districtId)
    {
        if (string.IsNullOrEmpty(provinceId) && string.IsNullOrEmpty(districtId))
        {
            return doctors;
        }

        Console.WriteLine($"Applying location filtering for search - ProvinceId: {provinceId}, DistrictId: {districtId}");

        var locationInfo = await _locationApiService.Value.GetLocationInfoAsync(provinceId, districtId);
        if (locationInfo == null)
        {
            Console.WriteLine($"Location info not found for province: {provinceId}, district: {districtId}");
            return doctors;
        }

        Console.WriteLine($"Location filter - Province: {locationInfo.ProvinceName}, District: {locationInfo.DistrictName}");

        var filteredDoctors = doctors.Where(doctor =>
            IsDoctorInLocationForSearch(doctor, locationInfo)).ToList();

        Console.WriteLine($"Location filtering: {doctors.Count} -> {filteredDoctors.Count} doctors");

        return filteredDoctors;
    }

    /// <summary>
    /// Check if doctor is in specified location for search results
    /// </summary>
    private bool IsDoctorInLocationForSearch(DoctorSearchForPatientResponse doctor, LocationInfo locationInfo)
    {
        // Simple location filtering based on hospital address
        if (doctor.Hospital?.Address == null)
        {
            Console.WriteLine($"Doctor {doctor.Id} has no hospital address");
            return false;
        }

        var hospitalAddress = doctor.Hospital.Address.ToLower();
        var provinceName = locationInfo.ProvinceName.ToLower();
        var districtName = locationInfo.DistrictName?.ToLower() ?? "";

        // Clean up province name - remove "thành phố" prefix
        var cleanProvinceName = provinceName.Replace("thành phố", "").Replace("tỉnh", "").Trim();

        Console.WriteLine($"Checking doctor {doctor.Id} with hospital address: '{hospitalAddress}' against location: Province='{locationInfo.ProvinceName}', District='{locationInfo.DistrictName}'");

        bool result;
        if (locationInfo.HasDistrict && !string.IsNullOrEmpty(districtName))
        {
            // Filter by both province and district
            result = hospitalAddress.Contains(cleanProvinceName) && hospitalAddress.Contains(districtName);
        }
        else
        {
            // Filter by province only
            result = hospitalAddress.Contains(cleanProvinceName);
        }

        Console.WriteLine($"Doctor {doctor.Id} location match result: {result}");
        return result;
    }

    /// <summary>
    /// Filter doctors by rating for search results
    /// </summary>
    private List<DoctorSearchForPatientResponse> FilterDoctorsByRatingForSearch(
        List<DoctorSearchForPatientResponse> doctors,
        double? minRating,
        List<double>? minRatings)
    {
        return FilterDoctorsByRatingGeneric(doctors, minRating, minRatings, d =>
            d.ReviewStatistics?.AverageRating);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Converts DoctorAdvancedFilterRequest to DoctorQueryRequest to eliminate code duplication
    /// </summary>
    private DoctorQueryRequest ConvertAdvancedFilterToQuery(DoctorAdvancedFilterRequest filter)
    {
        return new DoctorQueryRequest
        {
            SpecialtyId = filter.SpecialtyId,
            SpecialtyIds = filter.SpecialtyIds,
            PositionId = filter.PositionId,
            PositionIds = filter.PositionIds,
            Gender = filter.Gender,
            Genders = filter.Genders,
            MinYearsOfExperience = filter.MinYearsOfExperience,
            MaxYearsOfExperience = filter.MaxYearsOfExperience,
            ExperienceRanges = filter.ExperienceRanges,
            MinPrice = filter.MinPrice,
            MaxPrice = filter.MaxPrice,
            HospitalId = filter.HospitalId,
            HospitalIds = filter.HospitalIds,
            ProvinceId = filter.ProvinceId,
            DistrictId = filter.DistrictId,
            ServiceType = filter.ServiceType,
            ServiceTypes = filter.ServiceTypes,
            Language = filter.Language,
            Languages = filter.Languages,
            MinRating = filter.MinRating,
            MinRatings = filter.MinRatings,
            Address = filter.Address,
            SortBy = filter.SortBy,
            SortOrder = filter.SortOrder,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    /// <summary>
    /// Generic method to filter doctors by rating to eliminate code duplication
    /// </summary>
    private List<T> FilterDoctorsByRatingGeneric<T>(List<T> doctors, double? minRating, List<double>? minRatings, Func<T, double?> getRating)
    {
        if (minRating.HasValue)
        {
            // Filter doctors with rating >= minRating and < minRating + 1
            // Example: minRating = 3.0 means rating >= 3.0 and < 4.0
            return doctors.Where(d =>
            {
                var rating = getRating(d);
                return rating.HasValue && rating.Value >= minRating.Value && rating.Value < minRating.Value + 1.0;
            }).ToList();
        }

        if (minRatings != null && minRatings.Any())
        {
            // Filter doctors with rating in any of the specified ranges
            return doctors.Where(d =>
            {
                var rating = getRating(d);
                return rating.HasValue && minRatings.Any(r => rating.Value >= r && rating.Value < r + 1.0);
            }).ToList();
        }

        return doctors;
    }

    private List<DoctorResponse> FilterDoctorsByRating(List<DoctorResponse> doctors, double? minRating, List<double>? minRatings)
    {
        return FilterDoctorsByRatingGeneric(doctors, minRating, minRatings, d =>
            d.ReviewStatistics?.AverageRating);
    }

    #endregion

    #region Optimized Methods for gRPC Performance

    public async Task<DoctorEntity?> GetDoctorBasicInfoByIdAsync(Guid id)
    {
        return await _repository.Value.GetDoctorBasicInfoByIdAsync(id);
    }

    public async Task<List<DoctorEntity>> GetDoctorsBasicInfoByIdsAsync(IEnumerable<Guid> ids)
    {
        return await _repository.Value.GetDoctorsBasicInfoByIdsAsync(ids);
    }

    #endregion

    #region Optimized Patient Search

    /// <summary>
    /// Optimized search for patients - returns only necessary fields
    /// </summary>
    public async Task<DoctorSearchListResponse> SearchDoctorsForPatientsAsync(DoctorQueryRequest query, Guid? patientId = null)
    {
        // Use optimized repository method for complex filtering
        var (doctors, totalCount) = await _repository.Value.GetDoctorsForComplexFilterAsync(query);

        // Build hospitalId map BEFORE mapping to DTO
        var hospitalIdMap = doctors.Where(d => d.HospitalId.HasValue)
                                   .ToDictionary(d => d.Id, d => d.HospitalId!.Value);

        // Map to optimized response DTOs
        var mappedDoctors = _mapper.Value.Map<List<DoctorSearchForPatientResponse>>(doctors);

        // Create parallel tasks for enrichment
        var enrichmentTasks = new List<Task>();

        // Task 1: Enrich with hospital info
        var hospitalTask = EnrichDoctorSearchWithHospitalInfoAsync(mappedDoctors, hospitalIdMap);
        enrichmentTasks.Add(hospitalTask);

        // Task 2: Enrich with review statistics
        var reviewTask = EnrichDoctorSearchWithReviewStatisticsAsync(mappedDoctors);
        enrichmentTasks.Add(reviewTask);

        // Task 3: Set favorite status if patientId provided
        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            var favoriteTask = SetFavoriteStatusForSearchAsync(mappedDoctors, patientId.Value);
            enrichmentTasks.Add(favoriteTask);
        }

        // Execute all enrichment tasks in parallel
        await Task.WhenAll(enrichmentTasks);

        // Calculate pagination
        var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        return new DoctorSearchListResponse
        {
            Doctors = mappedDoctors,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = totalPages
        };
    }

    /// <summary>
    /// Enrich doctor search results with hospital basic info - OPTIMIZED with caching
    /// </summary>
    private async Task EnrichDoctorSearchWithHospitalInfoAsync(List<DoctorSearchForPatientResponse> doctors, Dictionary<Guid, Guid> hospitalIdMap)
    {
        if (!doctors.Any() || !hospitalIdMap.Any()) return;

        // Get unique hospital IDs
        var hospitalIds = hospitalIdMap.Values.Distinct().ToList();

        try
        {
            // OPTIMIZATION: Check if we can skip hospital enrichment for better performance
            // If hospital info is not critical for search results, we can make it optional
            if (hospitalIds.Count > 50) // If too many hospitals, skip to avoid gRPC timeout
            {
                Logger.LogWarning("Too many hospitals ({Count}) for enrichment, skipping hospital info", hospitalIds.Count);
                return;
            }

            // Call gRPC to get hospital info
            var hospitalInfoMap = await GetHospitalBasicInfoMapAsync(hospitalIds);

            // Map hospital info to doctors
            foreach (var doctor in doctors)
            {
                if (hospitalIdMap.TryGetValue(doctor.Id, out var hospitalId) &&
                    hospitalInfoMap.TryGetValue(hospitalId, out var hospitalInfo))
                {
                    doctor.Hospital = hospitalInfo;
                }
            }
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Hospital gRPC GetHospitalsList failed during patient search");
            // Continue without hospital info on failure
        }
    }

    /// <summary>
    /// Enrich doctor search results with review statistics (basic - no rating distribution) - OPTIMIZED
    /// </summary>
    private async Task EnrichDoctorSearchWithReviewStatisticsAsync(List<DoctorSearchForPatientResponse> doctors)
    {
        if (!doctors.Any()) return;

        try
        {
            // OPTIMIZATION: Add timeout and limit batch size
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5 second timeout

            var request = new BatchDoctorsStatisticsRequest();
            request.DoctorIds.AddRange(doctors.Select(d => d.Id.ToString()));

            var response = await _reviewClient.Value.GetBatchDoctorsStatisticsAsync(request, cancellationToken: cts.Token);

            foreach (var doctor in doctors)
            {
                var doctorIdStr = doctor.Id.ToString();
                if (response.DoctorStatistics.TryGetValue(doctorIdStr, out var stats))
                {
                    doctor.ReviewStatistics = new DoctorReviewStatisticsBasic
                    {
                        AverageRating = stats.AverageRating,
                        TotalReviews = stats.TotalReviews
                    };
                }
                else
                {
                    doctor.ReviewStatistics = new DoctorReviewStatisticsBasic
                    {
                        AverageRating = 0.0,
                        TotalReviews = 0
                    };
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Review gRPC GetBatchDoctorsStatistics timed out during patient search");
            SetDefaultReviewStatistics(doctors);
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Review gRPC GetBatchDoctorsStatistics failed during patient search");
            SetDefaultReviewStatistics(doctors);
        }
    }

    /// <summary>
    /// Set default review statistics for doctors
    /// </summary>
    private void SetDefaultReviewStatistics(List<DoctorSearchForPatientResponse> doctors)
    {
        foreach (var doctor in doctors)
        {
            doctor.ReviewStatistics = new DoctorReviewStatisticsBasic
            {
                AverageRating = 0.0,
                TotalReviews = 0
            };
        }
    }

    /// <summary>
    /// Set favorite status for doctor search results
    /// </summary>
    private async Task SetFavoriteStatusForSearchAsync(List<DoctorSearchForPatientResponse> doctors, Guid patientId)
    {
        if (!doctors.Any()) return;

        try
        {
            var request = new CheckMultipleFavoritesRequest
            {
                PatientId = patientId.ToString()
            };
            request.DoctorIds.AddRange(doctors.Select(d => d.Id.ToString()));

            var check = await _favoritesClient.Value.CheckMultipleFavoritesAsync(request);
            var favorited = check.FavoritedDoctorIds.Select(Guid.Parse).ToHashSet();

            foreach (var doctor in doctors)
            {
                doctor.IsFavorited = favorited.Contains(doctor.Id);
            }
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Favorites gRPC CheckMultipleFavorites failed for patient {PatientId}", patientId);
            // Favorites service unavailable; proceed with IsFavorited default false
        }
    }

    /// <summary>
    /// Enrich DoctorByIdResponse with hospital basic info
    /// </summary>
    private async Task EnrichDoctorByIdWithHospitalInfoAsync(DoctorByIdResponse doctor, Guid? hospitalId)
    {
        if (!hospitalId.HasValue) return;

        try
        {
            var request = new GetHospitalsBasicInfoRequest();
            request.Ids.Add(hospitalId.Value.ToString());

            var response = await _hospitalClient.Value.GetHospitalsBasicInfoAsync(request);

            if (response.Hospitals.Any())
            {
                var hospital = response.Hospitals.First();
                doctor.Hospital = new DoctorHospitalInfo
                {
                    Id = Guid.Parse(hospital.Id),
                    Name = hospital.Name,
                    Address = hospital.Address,
                    AvatarUrl = hospital.AvatarUrl
                };
            }
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Hospital gRPC GetHospitalsBasicInfo failed for doctor {DoctorId}", doctor.Id);
            // Continue without hospital info on failure
        }
    }

    /// <summary>
    /// Enrich DoctorByIdResponse with review statistics
    /// </summary>
    private async Task EnrichDoctorByIdWithReviewStatisticsAsync(DoctorByIdResponse doctor)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5 second timeout

            var request = new BatchDoctorsStatisticsRequest();
            request.DoctorIds.Add(doctor.Id.ToString());

            var response = await _reviewClient.Value.GetBatchDoctorsStatisticsAsync(request, cancellationToken: cts.Token);

            var doctorIdStr = doctor.Id.ToString();
            if (response.DoctorStatistics.TryGetValue(doctorIdStr, out var stats))
            {
                doctor.ReviewStatistics = new DoctorReviewInfo
                {
                    AverageRating = stats.AverageRating,
                    TotalReviews = stats.TotalReviews
                };
            }
            else
            {
                doctor.ReviewStatistics = new DoctorReviewInfo
                {
                    AverageRating = 0.0,
                    TotalReviews = 0
                };
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Review gRPC GetBatchDoctorsStatistics timed out for doctor {DoctorId}", doctor.Id);
            SetDefaultReviewStatisticsForDoctorById(doctor);
        }
        catch (global::Grpc.Core.RpcException ex)
        {
            Logger.LogWarning(ex, "Review gRPC GetBatchDoctorsStatistics failed for doctor {DoctorId}", doctor.Id);
            SetDefaultReviewStatisticsForDoctorById(doctor);
        }
    }

    /// <summary>
    /// Set default review statistics for DoctorByIdResponse
    /// </summary>
    private void SetDefaultReviewStatisticsForDoctorById(DoctorByIdResponse doctor)
    {
        doctor.ReviewStatistics = new DoctorReviewInfo
        {
            AverageRating = 0.0,
            TotalReviews = 0
        };
    }

    #endregion

}