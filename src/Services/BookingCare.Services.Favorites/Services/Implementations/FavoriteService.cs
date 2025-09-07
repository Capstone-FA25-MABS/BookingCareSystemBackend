using AutoMapper;
using BookingCare.Services.Favorites.Models.DTOs;
using BookingCare.Services.Favorites.Models.Entities;
using BookingCare.Services.Favorites.Repositories.Interfaces;
using BookingCare.Services.Favorites.Services.Interfaces;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Favorites.Services.Implementations;

/// <summary>
/// Service implementation for Favorite business logic
/// </summary>
public class FavoriteService : BaseService, IFavoriteService
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IMapper _mapper;

    public FavoriteService(
        IFavoriteRepository favoriteRepository,
        IMapper mapper,
        ILogger<FavoriteService> logger) : base(logger)
    {
        _favoriteRepository = favoriteRepository;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<FavoriteResponse?> GetByIdAsync(Guid id)
    {
        ValidateGuid(id, nameof(id));

        var entity = await _favoriteRepository.GetByIdAsync(id);
        return entity != null ? _mapper.Map<FavoriteResponse>(entity) : null;
    }

    /// <inheritdoc />
    public async Task<FavoriteResponse> AddFavoriteAsync(CreateFavoriteRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Adding favorite for Patient {PatientId} and Doctor {DoctorId}",
                null, request.PatientId, request.DoctorId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.PatientId, nameof(request.PatientId));
            ValidateGuid(request.DoctorId, nameof(request.DoctorId));

            // Check if favorite already exists
            var existingFavorite = await _favoriteRepository.GetByPatientAndDoctorAsync(
                request.PatientId, request.DoctorId);

            if (existingFavorite != null)
            {
                LogWarning("Favorite already exists for Patient {PatientId} and Doctor {DoctorId}",
                    null, request.PatientId, request.DoctorId);
                return _mapper.Map<FavoriteResponse>(existingFavorite);
            }

            // Create new favorite
            var favoriteEntity = _mapper.Map<FavoriteEntity>(request);
            var createdFavorite = await _favoriteRepository.CreateAsync(favoriteEntity);

            LogInfo("Successfully added favorite with ID {FavoriteId} for Patient {PatientId} and Doctor {DoctorId}",
                null, createdFavorite.Id, request.PatientId, request.DoctorId);

            return _mapper.Map<FavoriteResponse>(createdFavorite);
        }, "AddFavorite");
    }

    /// <inheritdoc />
    public async Task<ToggleFavoriteResponse> ToggleFavoriteAsync(ToggleFavoriteRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Toggling favorite for Patient {PatientId} and Doctor {DoctorId}",
                null, request.PatientId, request.DoctorId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.PatientId, nameof(request.PatientId));
            ValidateGuid(request.DoctorId, nameof(request.DoctorId));

            // Check if favorite already exists
            var existingFavorite = await _favoriteRepository.GetByPatientAndDoctorAsync(
                request.PatientId, request.DoctorId);

            if (existingFavorite != null)
            {
                // Favorite exists - remove it
                var removeResult = await _favoriteRepository.RemoveAsync(request.PatientId, request.DoctorId);

                LogInfo("Toggled (removed) favorite for Patient {PatientId} and Doctor {DoctorId}",
                    null, request.PatientId, request.DoctorId);

                return new ToggleFavoriteResponse
                {
                    PatientId = request.PatientId,
                    DoctorId = request.DoctorId,
                    IsFavorited = false,
                    Action = "Removed",
                    Favorite = null,
                    Timestamp = DateTime.UtcNow
                };
            }
            else
            {
                // Favorite doesn't exist - add it
                var favoriteEntity = _mapper.Map<FavoriteEntity>(request);
                var createdFavorite = await _favoriteRepository.CreateAsync(favoriteEntity);

                LogInfo("Toggled (added) favorite with ID {FavoriteId} for Patient {PatientId} and Doctor {DoctorId}",
                    null, createdFavorite.Id, request.PatientId, request.DoctorId);

                return new ToggleFavoriteResponse
                {
                    PatientId = request.PatientId,
                    DoctorId = request.DoctorId,
                    IsFavorited = true,
                    Action = "Added",
                    Favorite = _mapper.Map<FavoriteResponse>(createdFavorite),
                    Timestamp = DateTime.UtcNow
                };
            }
        }, "ToggleFavorite");
    }

    /// <inheritdoc />
    public async Task<bool> RemoveFavoriteAsync(RemoveFavoriteRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Removing favorite for Patient {PatientId} and Doctor {DoctorId}",
                null, request.PatientId, request.DoctorId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.PatientId, nameof(request.PatientId));
            ValidateGuid(request.DoctorId, nameof(request.DoctorId));

            var result = await _favoriteRepository.RemoveAsync(request.PatientId, request.DoctorId);

            if (result)
            {
                LogInfo("Successfully removed favorite for Patient {PatientId} and Doctor {DoctorId}",
                    null, request.PatientId, request.DoctorId);
            }
            else
            {
                LogWarning("Favorite not found for Patient {PatientId} and Doctor {DoctorId}",
                    null, request.PatientId, request.DoctorId);
            }

            return result;
        }, "RemoveFavorite");
    }

    /// <inheritdoc />
    public async Task<PagedResult<FavoriteResponse>> GetPatientFavoritesAsync(GetPatientFavoritesRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting favorites for Patient {PatientId}, Page {Page}, PageSize {PageSize}",
                null, request.PatientId, request.Page, request.PageSize);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.PatientId, nameof(request.PatientId));

            if (request.Page < 1) request.Page = 1;
            if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 20;

            var pagedResult = await _favoriteRepository.GetPatientFavoritesAsync(
                request.PatientId, request.Page, request.PageSize);

            var mappedItems = _mapper.Map<List<FavoriteResponse>>(pagedResult.Items);

            LogInfo("Retrieved {Count} favorites for Patient {PatientId}",
                null, mappedItems.Count, request.PatientId);

            return new PagedResult<FavoriteResponse>
            {
                Items = mappedItems,
                TotalCount = pagedResult.TotalCount,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            };
        }, "GetPatientFavorites");
    }

    /// <inheritdoc />
    public async Task<bool> IsFavoritedAsync(Guid patientId, Guid doctorId)
    {
        ValidateGuid(patientId, nameof(patientId));
        ValidateGuid(doctorId, nameof(doctorId));

        return await _favoriteRepository.IsFavoritedAsync(patientId, doctorId);
    }

    /// <inheritdoc />
    public async Task<long> GetDoctorFavoriteCountAsync(Guid doctorId)
    {
        ValidateGuid(doctorId, nameof(doctorId));
        return await _favoriteRepository.GetDoctorFavoriteCountAsync(doctorId);
    }

    /// <inheritdoc />
    public async Task<List<FavoriteResponse>> GetRecentFavoritesAsync(Guid patientId, int limit = 5)
    {
        ValidateGuid(patientId, nameof(patientId));

        if (limit < 1 || limit > 50) limit = 5;

        var recentFavorites = await _favoriteRepository.GetRecentFavoritesAsync(patientId, limit);
        return _mapper.Map<List<FavoriteResponse>>(recentFavorites);
    }

    /// <inheritdoc />
    public async Task<PagedResult<FavoriteResponse>> GetDoctorFavoritesAsync(Guid doctorId, int page = 1, int pageSize = 20)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting favorites for Doctor {DoctorId}, Page {Page}, PageSize {PageSize}",
                null, doctorId, page, pageSize);

            // Validation
            ValidateGuid(doctorId, nameof(doctorId));

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var pagedResult = await _favoriteRepository.GetDoctorFavoritesAsync(doctorId, page, pageSize);
            var mappedItems = _mapper.Map<List<FavoriteResponse>>(pagedResult.Items);

            LogInfo("Retrieved {Count} favorites for Doctor {DoctorId}",
                null, mappedItems.Count, doctorId);

            return new PagedResult<FavoriteResponse>
            {
                Items = mappedItems,
                TotalCount = pagedResult.TotalCount,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            };
        }, "GetDoctorFavorites");
    }

    /// <inheritdoc />
    public async Task<CheckMultipleFavoritesResponse> CheckMultipleFavoritesAsync(CheckMultipleFavoritesRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Checking multiple favorites for Patient {PatientId}, Doctor count: {DoctorCount}",
                null, request.PatientId, request.DoctorIds.Count);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.PatientId, nameof(request.PatientId));

            if (request.DoctorIds == null || !request.DoctorIds.Any())
            {
                LogWarning("Empty doctor IDs list provided for Patient {PatientId}", null, request.PatientId);
                return new CheckMultipleFavoritesResponse
                {
                    PatientId = request.PatientId,
                    FavoritedDoctorIds = new List<Guid>(),
                    TotalChecked = 0,
                    TotalFavorited = 0,
                    CheckedAt = DateTime.UtcNow
                };
            }

            // Validate each doctor ID
            foreach (var doctorId in request.DoctorIds)
            {
                ValidateGuid(doctorId, nameof(doctorId));
            }

            // Remove duplicates and validate
            var uniqueDoctorIds = request.DoctorIds.Distinct().ToList();

            if (uniqueDoctorIds.Count > 100)
            {
                LogWarning("Too many doctor IDs ({Count}) provided for Patient {PatientId}, limiting to 100",
                    null, uniqueDoctorIds.Count, request.PatientId);
                uniqueDoctorIds = uniqueDoctorIds.Take(100).ToList();
            }

            // Get favorited doctor IDs
            var favoritedDoctorIds = await _favoriteRepository.CheckMultipleFavoritesAsync(
                request.PatientId, uniqueDoctorIds);

            LogInfo("Found {FavoritedCount} favorites out of {TotalChecked} doctors for Patient {PatientId}",
                null, favoritedDoctorIds.Count, uniqueDoctorIds.Count, request.PatientId);

            return new CheckMultipleFavoritesResponse
            {
                PatientId = request.PatientId,
                FavoritedDoctorIds = favoritedDoctorIds,
                TotalChecked = uniqueDoctorIds.Count,
                TotalFavorited = favoritedDoctorIds.Count,
                CheckedAt = DateTime.UtcNow
            };
        }, "CheckMultipleFavorites");
    }
}