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
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Services.Implementations;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _repository;
    private readonly IPositionRepository _positionRepository;
    private readonly IMapper _mapper;
    private readonly FavoritesService.FavoritesServiceClient _favoritesClient;
    private readonly AuthService.AuthServiceClient _authClient;
    private readonly ILogger<DoctorService> _logger;

    public DoctorService(IDoctorRepository repository, IPositionRepository positionRepository, IMapper mapper, FavoritesService.FavoritesServiceClient favoritesClient, AuthService.AuthServiceClient authClient, ILogger<DoctorService> logger)
    {
        _repository = repository;
        _positionRepository = positionRepository;
        _mapper = mapper;
        _favoritesClient = favoritesClient;
        _authClient = authClient;
        _logger = logger;
    }

    #region Doctor CRUD Operations

    /// <summary>
    /// Tạo doctor mới với giá cơ bản
    /// </summary>
    public async Task<DoctorResponse> CreateDoctorAsync(CreateDoctorRequest request)
    {
        // Validate unique constraints
        if (await _repository.DoctorEmailExistsAsync(request.Email))
        {
            throw DoctorConflictException.WithEmail(request.Email);
        }

        if (await _repository.DoctorAccountExistsAsync(request.AccountId))
        {
            throw DoctorConflictException.WithAccountId(request.AccountId);
        }

        // Validate PositionId exists if provided
        if (request.PositionId.HasValue)
        {
            if (!await _positionRepository.PositionExistsAsync(request.PositionId.Value))
            {
                throw PositionNotFoundException.WithId(request.PositionId.Value);
            }
        }

        // Create doctor entity
        var doctor = _mapper.Map<DoctorEntity>(request);
        doctor.Id = Guid.NewGuid();

        var createdDoctor = await _repository.CreateDoctorAsync(doctor);

        // Create doctor prices if provided
        if (request.Prices != null && request.Prices.Any())
        {
            foreach (var priceRequest in request.Prices)
            {
                // Validate service type exists
                var serviceType = await _repository.GetServiceTypeByIdAsync(priceRequest.ServiceTypeId);
                if (serviceType == null)
                {
                    throw new ArgumentException($"Service type with ID {priceRequest.ServiceTypeId} not found");
                }

                var doctorPrice = new DoctorPriceEntity
                {
                    Id = Guid.NewGuid(),
                    DoctorId = createdDoctor.Id,
                    ServiceTypeId = priceRequest.ServiceTypeId,
                    Amount = priceRequest.Amount
                };
                await _repository.CreateDoctorPriceAsync(doctorPrice);
            }
        }

        // Create doctor languages if provided
        if (request.LanguageIds != null && request.LanguageIds.Any())
        {
            foreach (var languageId in request.LanguageIds)
            {
                // Validate language exists
                var language = await _repository.GetLanguageByIdAsync(languageId);
                if (language == null)
                {
                    throw new ArgumentException($"Language with ID {languageId} not found");
                }

                var doctorLanguage = new DoctorLanguageEntity
                {
                    Id = Guid.NewGuid(),
                    DoctorId = createdDoctor.Id,
                    LanguageId = languageId
                };
                await _repository.CreateDoctorLanguageAsync(doctorLanguage);
            }
        }

        var response = _mapper.Map<DoctorResponse>(createdDoctor);
        return response;
    }

    public async Task<DoctorResponse?> GetDoctorByIdAsync(Guid id)
    {
        var doctor = await _repository.GetDoctorByIdAsync(id);
        if (doctor == null) return null;
        var response = _mapper.Map<DoctorResponse>(doctor);
        return response;
    }

    public async Task<DoctorResponse?> GetDoctorByEmailAsync(string email)
    {
        var doctor = await _repository.GetDoctorByEmailAsync(email);
        if (doctor == null) return null;

        var response = _mapper.Map<DoctorResponse>(doctor);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(new List<DoctorResponse> { response });

        return response;
    }

    public async Task<DoctorResponse?> GetDoctorByAccountIdAsync(Guid accountId)
    {
        var doctor = await _repository.GetDoctorByAccountIdAsync(accountId);
        if (doctor == null) return null;

        var response = _mapper.Map<DoctorResponse>(doctor);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(new List<DoctorResponse> { response });

        return response;
    }

    /// <summary>
    /// Cập nhật thông tin doctor
    /// </summary>
    public async Task<DoctorResponse> UpdateDoctorAsync(UpdateDoctorRequest request)
    {
        // Check if doctor exists
        var existingDoctor = await _repository.GetDoctorByIdAsync(request.Id);
        if (existingDoctor == null)
        {
            throw DoctorNotFoundException.WithId(request.Id);
        }

        // Validate PositionId exists if provided
        if (request.PositionId.HasValue)
        {
            if (!await _positionRepository.PositionExistsAsync(request.PositionId.Value))
            {
                throw PositionNotFoundException.WithId(request.PositionId.Value);
            }
        }

        // Update doctor entity
        _mapper.Map(request, existingDoctor);
        existingDoctor.UpdatedAt = DateTime.UtcNow;

        // Update doctor prices if provided
        if (request.Prices != null && request.Prices.Any())
        {
            // Delete existing prices
            await _repository.DeleteAllDoctorPricesAsync(existingDoctor.Id);

            // Add new prices
            foreach (var priceRequest in request.Prices)
            {
                // Validate service type exists
                var serviceType = await _repository.GetServiceTypeByIdAsync(priceRequest.ServiceTypeId);
                if (serviceType == null)
                {
                    throw new ArgumentException($"Service type with ID {priceRequest.ServiceTypeId} not found");
                }

                var doctorPrice = new DoctorPriceEntity
                {
                    Id = Guid.NewGuid(),
                    DoctorId = existingDoctor.Id,
                    ServiceTypeId = priceRequest.ServiceTypeId,
                    Amount = priceRequest.Amount
                };
                await _repository.CreateDoctorPriceAsync(doctorPrice);
            }
        }

        // Update doctor languages if provided
        if (request.LanguageIds != null)
        {
            // Delete existing languages
            await _repository.DeleteAllDoctorLanguagesAsync(existingDoctor.Id);

            // Add new languages
            if (request.LanguageIds.Any())
            {
                foreach (var languageId in request.LanguageIds)
                {
                    // Validate language exists
                    var language = await _repository.GetLanguageByIdAsync(languageId);
                    if (language == null)
                    {
                        throw new ArgumentException($"Language with ID {languageId} not found");
                    }

                    var doctorLanguage = new DoctorLanguageEntity
                    {
                        Id = Guid.NewGuid(),
                        DoctorId = existingDoctor.Id,
                        LanguageId = languageId
                    };
                    await _repository.CreateDoctorLanguageAsync(doctorLanguage);
                }
            }
        }

        var updatedDoctor = await _repository.UpdateDoctorAsync(existingDoctor);
        var response = _mapper.Map<DoctorResponse>(updatedDoctor);
        return response;
    }

    public async Task<bool> DeleteDoctorAsync(Guid id)
    {
        return await _repository.DeleteDoctorAsync(id);
    }

    #endregion

    #region Doctor Query Operations

    public async Task<DoctorListResponse> GetDoctorsAsync(DoctorQueryRequest query)
    {
        var (doctors, totalCount) = await _repository.GetDoctorsAsync(query);
        var response = _mapper.Map<DoctorListResponse>((doctors, totalCount));

        // Set pagination info
        response.PageNumber = query.PageNumber;
        response.PageSize = query.PageSize;
        response.TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(response.Doctors);

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

        return response;
    }

    public async Task<List<DoctorResponse>> GetDoctorsBySpecialtyAsync(Guid specialtyId)
    {
        var doctors = await _repository.GetDoctorsBySpecialtyAsync(specialtyId);
        var response = _mapper.Map<List<DoctorResponse>>(doctors);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(response);

        return response;
    }

    public async Task<List<DoctorResponse>> GetDoctorsByPositionAsync(Guid positionId)
    {
        var doctors = await _repository.GetDoctorsByPositionAsync(positionId);
        var response = _mapper.Map<List<DoctorResponse>>(doctors);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(response);

        return response;
    }

    public async Task<List<DoctorResponse>> GetActiveDoctorsAsync()
    {
        var doctors = await _repository.GetActiveDoctorsAsync();
        var response = _mapper.Map<List<DoctorResponse>>(doctors);

        // Enrich with account status
        await EnrichDoctorsWithStatusAsync(response);

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
            // If searching, fetch all favorites to avoid missing matches due to pagination
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var countReq = new GetPatientFavoriteCountRequest { PatientId = patientId.ToString() };
                var countRes = await _favoritesClient.GetPatientFavoriteCountAsync(countReq);
                var totalFavorites = (int)countRes.FavoriteCount;

                if (totalFavorites == 0)
                {
                    return new DoctorListResponse
                    {
                        Doctors = new List<DoctorResponse>(),
                        TotalCount = 0,
                        PageNumber = 1,
                        PageSize = 0,
                        TotalPages = 0
                    };
                }

                var allReq = new GetPatientFavoritesRequest
                {
                    PatientId = patientId.ToString(),
                    Page = 1,
                    PageSize = totalFavorites
                };
                var allRes = await _favoritesClient.GetPatientFavoritesAsync(allReq);
                var allDoctorIds = allRes.Items.Select(i => Guid.Parse(i.DoctorId)).ToList();

                var doctors = await _repository.GetDoctorsByIdsAsync(allDoctorIds);

                var searchLower = searchTerm.ToLower();
                doctors = doctors.Where(d =>
                    d.FirstName.ToLower().Contains(searchLower) ||
                    d.LastName.ToLower().Contains(searchLower) ||
                    d.Email.ToLower().Contains(searchLower) ||
                    (d.Bio != null && d.Bio.ToLower().Contains(searchLower))
                ).ToList();

                var mapped = _mapper.Map<List<DoctorResponse>>(doctors);
                var favoritedSet = allDoctorIds.ToHashSet();
                foreach (var d in mapped)
                {
                    d.IsFavorited = favoritedSet.Contains(d.Id);
                }

                // Enrich with account status
                await EnrichDoctorsWithStatusAsync(mapped);

                var filteredCount = mapped.Count;
                return new DoctorListResponse
                {
                    Doctors = mapped,
                    TotalCount = filteredCount,
                    PageNumber = 1,
                    PageSize = filteredCount,
                    TotalPages = filteredCount == 0 ? 0 : 1
                };
            }

            // Default paginated path (no search)
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
                return new DoctorListResponse
                {
                    Doctors = new List<DoctorResponse>(),
                    TotalCount = 0,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalPages = 0
                };
            }

            var pageDoctors = await _repository.GetDoctorsByIdsAsync(doctorIds);
            var mappedPage = _mapper.Map<List<DoctorResponse>>(pageDoctors);

            var favoritedSetPage = doctorIds.ToHashSet();
            foreach (var d in mappedPage)
            {
                d.IsFavorited = favoritedSetPage.Contains(d.Id);
            }

            // Enrich with account status
            await EnrichDoctorsWithStatusAsync(mappedPage);

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
        catch (global::Grpc.Core.RpcException ex)
        {
            _logger.LogWarning(ex, "Favorites gRPC GetPatientFavorites failed for patient {PatientId}", patientId);
            // Favorites service unavailable; return empty list gracefully
            return new DoctorListResponse
            {
                Doctors = new List<DoctorResponse>(),
                TotalCount = 0,
                PageNumber = string.IsNullOrWhiteSpace(searchTerm) ? page : 1,
                PageSize = string.IsNullOrWhiteSpace(searchTerm) ? pageSize : 0,
                TotalPages = 0
            };
        }
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
            _logger.LogWarning(ex, "Favorites gRPC CheckMultipleFavorites failed for patient {PatientId}", patientId);
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
            _logger.LogWarning(ex, "Auth gRPC GetAccountStatusByIds failed");
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

    private async Task UpdateDoctorPriceAsync(DoctorEntity doctor, decimal amount, bool isOverride)
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

    #endregion
}