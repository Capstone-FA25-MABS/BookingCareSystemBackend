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

namespace BookingCare.Services.Doctor.Services.Implementations;

public class DoctorService : BaseService, IDoctorService
{
    private readonly IDoctorRepository _repository;
    private readonly IPositionRepository _positionRepository;
    private readonly ISpecialtyRepository _specialtyRepository;
    private readonly IMapper _mapper;
    private readonly FavoritesService.FavoritesServiceClient _favoritesClient;
    private readonly AuthService.AuthServiceClient _authClient;
    private readonly ReviewService.ReviewServiceClient _reviewClient;

    public DoctorService(IDoctorRepository repository, IPositionRepository positionRepository, ISpecialtyRepository specialtyRepository, IMapper mapper, FavoritesService.FavoritesServiceClient favoritesClient, AuthService.AuthServiceClient authClient, ReviewService.ReviewServiceClient reviewClient, ILogger<DoctorService> logger) : base(logger)
    {
        _repository = repository;
        _positionRepository = positionRepository;
        _specialtyRepository = specialtyRepository;
        _mapper = mapper;
        _favoritesClient = favoritesClient;
        _authClient = authClient;
        _reviewClient = reviewClient;
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
            var createdDoctor = await _repository.CreateDoctorAsync(doctor);

            await CreateDoctorPricesAsync(createdDoctor.Id, request.Prices);
            await CreateDoctorLanguagesAsync(createdDoctor.Id, request.LanguageIds);

            return _mapper.Map<DoctorResponse>(createdDoctor);
        }, nameof(CreateDoctorAsync));
    }

    private async Task ValidateCreateDoctorRequest(CreateDoctorRequest request)
    {
        if (await _repository.DoctorEmailExistsAsync(request.Email))
        {
            throw DoctorConflictException.WithEmail(request.Email);
        }

        if (await _repository.DoctorAccountExistsAsync(request.AccountId))
        {
            throw DoctorConflictException.WithAccountId(request.AccountId);
        }

        if (request.PositionId.HasValue && !await _positionRepository.PositionExistsAsync(request.PositionId.Value))
        {
            throw PositionNotFoundException.WithId(request.PositionId.Value);
        }
    }

    private DoctorEntity CreateDoctorEntity(CreateDoctorRequest request)
    {
        var doctor = _mapper.Map<DoctorEntity>(request);
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
        var serviceType = await _repository.GetServiceTypeByIdAsync(priceRequest.ServiceTypeId);
        if (serviceType == null)
        {
            throw new ArgumentException($"Service type with ID {priceRequest.ServiceTypeId} not found");
        }

        var doctorPrice = new DoctorPriceEntity
        {
            Id = Guid.NewGuid(),
            DoctorId = doctorId,
            ServiceTypeId = priceRequest.ServiceTypeId,
            Amount = priceRequest.Amount
        };
        await _repository.CreateDoctorPriceAsync(doctorPrice);
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
        var language = await _repository.GetLanguageByIdAsync(languageId);
        if (language == null)
        {
            throw new ArgumentException($"Language with ID {languageId} not found");
        }

        var doctorLanguage = new DoctorLanguageEntity
        {
            Id = Guid.NewGuid(),
            DoctorId = doctorId,
            LanguageId = languageId
        };
        await _repository.CreateDoctorLanguageAsync(doctorLanguage);
    }

    public async Task<DoctorResponse?> GetDoctorByIdAsync(Guid id)
    {
        var doctor = await _repository.GetDoctorByIdAsync(id);
        if (doctor == null) return null;

        // Include Position và Specialty
        if (doctor.PositionId.HasValue)
        {
            doctor.Position = await _positionRepository.GetPositionByIdAsync(doctor.PositionId.Value);
        }
        if (doctor.SpecialtyId.HasValue)
        {
            doctor.Specialty = await _specialtyRepository.GetSpecialtyByIdAsync(doctor.SpecialtyId.Value);
        }

        var response = _mapper.Map<DoctorResponse>(doctor);

        // Enrich with review statistics
        await EnrichDoctorWithReviewStatisticsAsync(response);

        return response;
    }

    public async Task<DoctorResponse?> GetDoctorByEmailAsync(string email)
    {
        var doctor = await _repository.GetDoctorByEmailAsync(email);
        if (doctor == null) return null;

        // Include Position và Specialty
        if (doctor.PositionId.HasValue)
        {
            doctor.Position = await _positionRepository.GetPositionByIdAsync(doctor.PositionId.Value);
        }
        if (doctor.SpecialtyId.HasValue)
        {
            doctor.Specialty = await _specialtyRepository.GetSpecialtyByIdAsync(doctor.SpecialtyId.Value);
        }

        var response = _mapper.Map<DoctorResponse>(doctor);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(new List<DoctorResponse> { response });

        // Enrich with review statistics
        await EnrichDoctorWithReviewStatisticsAsync(response);

        return response;
    }

    public async Task<DoctorResponse?> GetDoctorByAccountIdAsync(Guid accountId)
    {
        var doctor = await _repository.GetDoctorByAccountIdAsync(accountId);
        if (doctor == null) return null;

        // Include Position và Specialty
        if (doctor.PositionId.HasValue)
        {
            doctor.Position = await _positionRepository.GetPositionByIdAsync(doctor.PositionId.Value);
        }
        if (doctor.SpecialtyId.HasValue)
        {
            doctor.Specialty = await _specialtyRepository.GetSpecialtyByIdAsync(doctor.SpecialtyId.Value);
        }

        var response = _mapper.Map<DoctorResponse>(doctor);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(new List<DoctorResponse> { response });

        // Enrich with review statistics
        await EnrichDoctorWithReviewStatisticsAsync(response);

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

            var updatedDoctor = await _repository.UpdateDoctorAsync(existingDoctor);
            return _mapper.Map<DoctorResponse>(updatedDoctor);
        }, nameof(UpdateDoctorAsync));
    }

    private async Task<DoctorEntity> ValidateAndGetExistingDoctor(Guid id)
    {
        var existingDoctor = await _repository.GetDoctorByIdAsync(id);
        if (existingDoctor == null)
        {
            throw DoctorNotFoundException.WithId(id);
        }
        return existingDoctor;
    }

    private async Task ValidateUpdateDoctorRequest(UpdateDoctorRequest request)
    {
        if (request.PositionId.HasValue && !await _positionRepository.PositionExistsAsync(request.PositionId.Value))
        {
            throw PositionNotFoundException.WithId(request.PositionId.Value);
        }
    }

    private void UpdateDoctorEntity(DoctorEntity existingDoctor, UpdateDoctorRequest request)
    {
        _mapper.Map(request, existingDoctor);
        existingDoctor.UpdatedAt = DateTime.UtcNow;
    }

    private async Task UpdateDoctorPricesAsync(Guid doctorId, IEnumerable<DoctorPriceRequest>? prices)
    {
        if (prices == null || !prices.Any()) return;

        await _repository.DeleteAllDoctorPricesAsync(doctorId);

        foreach (var priceRequest in prices)
        {
            await ValidateAndCreateDoctorPrice(doctorId, priceRequest);
        }
    }

    private async Task UpdateDoctorLanguagesAsync(Guid doctorId, IEnumerable<Guid>? languageIds)
    {
        await _repository.DeleteAllDoctorLanguagesAsync(doctorId);

        if (languageIds != null && languageIds.Any())
        {
            foreach (var languageId in languageIds)
            {
                await ValidateAndCreateDoctorLanguage(doctorId, languageId);
            }
        }
    }

    public async Task<bool> DeleteDoctorAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            return await _repository.DeleteDoctorAsync(id);
        }, nameof(DeleteDoctorAsync));
    }

    public async Task<bool> ToggleDoctorStatusAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var doctor = await _repository.GetDoctorByIdAsync(id);
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
        var (doctors, totalCount) = await _repository.GetDoctorsAsync(query);

        // Enrich with Position và Specialty
        await EnrichDoctorsWithPositionAndSpecialtyAsync(doctors);

        var response = _mapper.Map<DoctorListResponse>((doctors, totalCount));

        // Set pagination info
        response.PageNumber = query.PageNumber;
        response.PageSize = query.PageSize;
        response.TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(response.Doctors);

        // Enrich with review statistics
        await EnrichDoctorsWithReviewStatisticsAsync(response.Doctors);

        // Filter by status if specified (after getting status from Auth service)
        if (query.Status.HasValue)
        {
            response.Doctors = response.Doctors.Where(d => d.Status == query.Status.Value).ToList();
            response.TotalCount = response.Doctors.Count; // Update total count after filtering
            response.TotalPages = (int)Math.Ceiling((double)response.TotalCount / query.PageSize);
        }

        return response;
    }

    public async Task<DoctorListResponse> FilterDoctorsAsync(DoctorAdvancedFilterRequest filter)
    {
        // Convert advanced filter to basic query
        var query = new DoctorQueryRequest
        {
            SpecialtyId = filter.SpecialtyId,
            Gender = filter.Gender,
            MinYearsOfExperience = filter.MinYearsOfExperience,
            MaxYearsOfExperience = filter.MaxYearsOfExperience,
            MinPrice = filter.MinPrice,
            MaxPrice = filter.MaxPrice,
            HospitalId = filter.HospitalId,
            ServiceType = filter.ServiceType,
            Language = filter.Language,
            MinRating = filter.MinRating,
            Address = filter.Address,
            SortBy = filter.SortBy,
            SortOrder = filter.SortOrder,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        return await GetDoctorsAsync(query);
    }

    public async Task<List<DoctorResponse>> GetDoctorsByHospitalAsync(Guid hospitalId)
    {
        var doctors = await _repository.GetDoctorsByHospitalAsync(hospitalId);
        var response = _mapper.Map<List<DoctorResponse>>(doctors);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(response);

        // Enrich with review statistics
        await EnrichDoctorsWithReviewStatisticsAsync(response);

        return response;
    }

    public async Task<List<DoctorResponse>> GetDoctorsBySpecialtyAsync(Guid specialtyId)
    {
        var doctors = await _repository.GetDoctorsBySpecialtyAsync(specialtyId);
        var response = _mapper.Map<List<DoctorResponse>>(doctors);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(response);

        // Enrich with review statistics
        await EnrichDoctorsWithReviewStatisticsAsync(response);

        return response;
    }

    public async Task<List<DoctorResponse>> GetDoctorsByPositionAsync(Guid positionId)
    {
        var doctors = await _repository.GetDoctorsByPositionAsync(positionId);
        var response = _mapper.Map<List<DoctorResponse>>(doctors);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(response);

        // Enrich with review statistics
        await EnrichDoctorsWithReviewStatisticsAsync(response);

        return response;
    }

    public async Task<List<DoctorResponse>> GetActiveDoctorsAsync()
    {
        var doctors = await _repository.GetActiveDoctorsAsync();
        var response = _mapper.Map<List<DoctorResponse>>(doctors);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(response);

        // Enrich with review statistics
        await EnrichDoctorsWithReviewStatisticsAsync(response);

        return response;
    }

    public async Task<List<DoctorBasicInfoResponse>> GetDoctorsByAccountIdsAsync(IEnumerable<Guid> accountIds)
    {
        var doctors = await _repository.GetDoctorsByAccountIdsAsync(accountIds);
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
        var doctors = await _repository.GetDoctorsByIdsAsync(allDoctorIds);

        var filteredDoctors = FilterDoctorsBySearchTerm(doctors, searchTerm);
        var mappedDoctors = _mapper.Map<List<DoctorResponse>>(filteredDoctors);

        SetFavoriteStatus(mappedDoctors, allDoctorIds);
        await EnrichDoctorsWithStatusAsync(mappedDoctors);
        await EnrichDoctorsWithReviewStatisticsAsync(mappedDoctors);

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

        var response = await _favoritesClient.GetPatientFavoritesAsync(request);
        var doctorIds = response.Items.Select(i => Guid.Parse(i.DoctorId)).ToList();

        if (!doctorIds.Any())
        {
            return CreateEmptyDoctorListResponse(page, pageSize, null);
        }

        var pageDoctors = await _repository.GetDoctorsByIdsAsync(doctorIds);
        var mappedPage = _mapper.Map<List<DoctorResponse>>(pageDoctors);

        SetFavoriteStatus(mappedPage, doctorIds);
        await EnrichDoctorsWithStatusAsync(mappedPage);
        await EnrichDoctorsWithReviewStatisticsAsync(mappedPage);

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
        var countRes = await _favoritesClient.GetPatientFavoriteCountAsync(countReq);
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
        var allRes = await _favoritesClient.GetPatientFavoritesAsync(allReq);
        return allRes.Items.Select(i => Guid.Parse(i.DoctorId)).ToList();
    }

    private List<DoctorEntity> FilterDoctorsBySearchTerm(List<DoctorEntity> doctors, string searchTerm)
    {
        var searchLower = searchTerm.ToLower();
        return doctors.Where(d =>
            d.FirstName.ToLower().Contains(searchLower) ||
            d.LastName.ToLower().Contains(searchLower) ||
            d.Email.ToLower().Contains(searchLower) ||
            (d.Bio != null && d.Bio.ToLower().Contains(searchLower))
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

            var check = await _favoritesClient.CheckMultipleFavoritesAsync(request);
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
        var prices = await _repository.GetDoctorPricesAsync(doctorId);
        return _mapper.Map<List<DoctorPriceResponse>>(prices);
    }

    public async Task<DoctorPriceResponse> AssignPriceToDoctorAsync(AssignPriceToDoctorRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Validate doctor exists
            if (!await _repository.DoctorExistsAsync(request.DoctorId))
            {
                throw DoctorNotFoundException.WithId(request.DoctorId);
            }

            // Get doctor
            var doctor = await _repository.GetDoctorByIdAsync(request.DoctorId);
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

            var createdDoctorPrice = await _repository.CreateDoctorPriceAsync(doctorPrice);
            return _mapper.Map<DoctorPriceResponse>(createdDoctorPrice);
        }, nameof(AssignPriceToDoctorAsync));
    }

    public async Task<bool> RemovePriceFromDoctorAsync(Guid doctorId, Guid priceId)
    {
        return await _repository.DeleteDoctorPriceAsync(doctorId, priceId);
    }

    #endregion

    #region Validation Operations

    public async Task<bool> DoctorExistsAsync(Guid id)
    {
        return await _repository.DoctorExistsAsync(id);
    }

    public async Task<bool> DoctorEmailExistsAsync(string email, Guid? excludeId = null)
    {
        return await _repository.DoctorEmailExistsAsync(email, excludeId);
    }

    public async Task<bool> DoctorAccountExistsAsync(Guid accountId, Guid? excludeId = null)
    {
        return await _repository.DoctorAccountExistsAsync(accountId, excludeId);
    }

    public async Task<bool> DoctorPriceExistsAsync(Guid doctorId, Guid priceId)
    {
        return await _repository.DoctorPriceExistsAsync(doctorId, priceId);
    }

    #endregion

    #region Helper Methods

    public IQueryable<DoctorEntity> GetQueryableDoctors()
    {
        return _repository.GetQueryableDoctors();
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
            var positionList = await _positionRepository.GetPositionsByIdsAsync(positionIds);
            positions = positionList.ToDictionary(p => p.Id);
        }

        if (specialtyIds.Any())
        {
            var specialtyList = await _specialtyRepository.GetSpecialtiesByIdsAsync(specialtyIds);
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
        var existingPrices = await _repository.GetDoctorPricesAsync(doctor.Id);
        var existingPrice = existingPrices.FirstOrDefault();

        if (existingPrice != null)
        {
            // Cập nhật giá hiện tại
            existingPrice.Amount = amount;
            existingPrice.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateDoctorPriceAsync(existingPrice);
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
            await _repository.CreateDoctorPriceAsync(doctorPrice);
        }
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

            var response = await _reviewClient.GetDoctorDetailedStatisticsAsync(request);

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

            var response = await _reviewClient.GetBatchDoctorsStatisticsAsync(request);

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
}