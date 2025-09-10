using BookingCare.Services.Favorite;
using BookingCare.Services.Favorites.Services.Interfaces;
using BookingCare.Services.Favorites.Models.DTOs;
using Grpc.Core;
using GrpcModels = BookingCare.Services.Favorite;

namespace BookingCare.Services.Favorites.Services.Grpc;

/// <summary>
/// gRPC service implementation for Favorites inter-service communication
/// </summary>
public class FavoritesGrpcService : FavoritesService.FavoritesServiceBase
{
    private readonly IFavoriteService _favoriteService;
    private readonly ILogger<FavoritesGrpcService> _logger;

    public FavoritesGrpcService(IFavoriteService favoriteService, ILogger<FavoritesGrpcService> logger)
    {
        _favoriteService = favoriteService;
        _logger = logger;
    }

    /// <summary>
    /// Check multiple doctors favorite status for a patient via gRPC
    /// </summary>
    public override async Task<GrpcModels.CheckMultipleFavoritesResponse> CheckMultipleFavorites(
        GrpcModels.CheckMultipleFavoritesRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC CheckMultipleFavorites called for Patient {PatientId} with {DoctorCount} doctors",
                request.PatientId, request.DoctorIds.Count);

            // Convert gRPC request to service DTO
            var serviceRequest = new Models.DTOs.CheckMultipleFavoritesRequest
            {
                PatientId = Guid.Parse(request.PatientId),
                DoctorIds = request.DoctorIds.Select(Guid.Parse).ToList()
            };

            // Call service
            var serviceResponse = await _favoriteService.CheckMultipleFavoritesAsync(serviceRequest);

            // Convert service response to gRPC response
            var grpcResponse = new GrpcModels.CheckMultipleFavoritesResponse
            {
                PatientId = serviceResponse.PatientId.ToString(),
                TotalChecked = serviceResponse.TotalChecked,
                TotalFavorited = serviceResponse.TotalFavorited,
                Success = true,
                Message = "Favorites checked successfully"
            };

            grpcResponse.FavoritedDoctorIds.AddRange(
                serviceResponse.FavoritedDoctorIds.Select(id => id.ToString()));

            _logger.LogInformation("gRPC CheckMultipleFavorites completed for Patient {PatientId}, found {FavoritedCount} favorites",
                request.PatientId, serviceResponse.TotalFavorited);

            return grpcResponse;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in gRPC CheckMultipleFavorites for Patient {PatientId}", request.PatientId);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CheckMultipleFavorites for Patient {PatientId}", request.PatientId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Check single doctor favorite status for a patient via gRPC
    /// </summary>
    public override async Task<GrpcModels.CheckSingleFavoriteResponse> CheckSingleFavorite(
        GrpcModels.CheckSingleFavoriteRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC CheckSingleFavorite called for Patient {PatientId} and Doctor {DoctorId}",
                request.PatientId, request.DoctorId);

            var patientId = Guid.Parse(request.PatientId);
            var doctorId = Guid.Parse(request.DoctorId);

            var isFavorited = await _favoriteService.IsFavoritedAsync(patientId, doctorId);

            var response = new GrpcModels.CheckSingleFavoriteResponse
            {
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                IsFavorited = isFavorited,
                Success = true,
                Message = "Favorite status checked successfully"
            };

            _logger.LogInformation("gRPC CheckSingleFavorite completed for Patient {PatientId} and Doctor {DoctorId}, result: {IsFavorited}",
                request.PatientId, request.DoctorId, isFavorited);

            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in gRPC CheckSingleFavorite for Patient {PatientId} and Doctor {DoctorId}",
                request.PatientId, request.DoctorId);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CheckSingleFavorite for Patient {PatientId} and Doctor {DoctorId}",
                request.PatientId, request.DoctorId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Get patient's favorite count via gRPC
    /// </summary>
    public override async Task<GrpcModels.GetPatientFavoriteCountResponse> GetPatientFavoriteCount(
        GrpcModels.GetPatientFavoriteCountRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetPatientFavoriteCount called for Patient {PatientId}", request.PatientId);

            var patientId = Guid.Parse(request.PatientId);

            // Get patient's favorites and count them
            var favoritesRequest = new Models.DTOs.GetPatientFavoritesRequest
            {
                PatientId = patientId,
                Page = 1,
                PageSize = 1 // We only need the count
            };

            var favorites = await _favoriteService.GetPatientFavoritesAsync(favoritesRequest);

            var response = new GrpcModels.GetPatientFavoriteCountResponse
            {
                PatientId = request.PatientId,
                FavoriteCount = favorites.TotalCount,
                Success = true,
                Message = "Patient favorite count retrieved successfully"
            };

            _logger.LogInformation("gRPC GetPatientFavoriteCount completed for Patient {PatientId}, count: {Count}",
                request.PatientId, favorites.TotalCount);

            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in gRPC GetPatientFavoriteCount for Patient {PatientId}", request.PatientId);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetPatientFavoriteCount for Patient {PatientId}", request.PatientId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Get patient's favorites with pagination via gRPC
    /// </summary>
    public override async Task<GrpcModels.GetPatientFavoritesResponse> GetPatientFavorites(
        GrpcModels.GetPatientFavoritesRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetPatientFavorites called for Patient {PatientId}, Page {Page}, PageSize {PageSize}",
                request.PatientId, request.Page, request.PageSize);

            var patientId = Guid.Parse(request.PatientId);

            // Convert gRPC request to service DTO
            var serviceRequest = new Models.DTOs.GetPatientFavoritesRequest
            {
                PatientId = patientId,
                Page = request.Page > 0 ? request.Page : 1,
                PageSize = request.PageSize > 0 ? request.PageSize : 20
            };

            // Call service
            var serviceResponse = await _favoriteService.GetPatientFavoritesAsync(serviceRequest);

            // Convert service response to gRPC response
            var grpcResponse = new GrpcModels.GetPatientFavoritesResponse
            {
                PatientId = request.PatientId,
                TotalCount = serviceResponse.TotalCount,
                PageNumber = serviceResponse.PageNumber,
                PageSize = serviceResponse.PageSize,
                TotalPages = serviceResponse.TotalPages,
                HasNextPage = serviceResponse.HasNextPage,
                HasPreviousPage = serviceResponse.HasPreviousPage,
                Success = true,
                Message = "Patient favorites retrieved successfully"
            };

            // Convert items to gRPC format (without redundant patient_id)
            foreach (var item in serviceResponse.Items)
            {
                grpcResponse.Items.Add(new GrpcModels.FavoriteItem
                {
                    Id = item.Id.ToString(),
                    DoctorId = item.DoctorId.ToString(),
                    CreatedAt = item.CreatedAt.ToString("O") // ISO 8601 format
                });
            }

            _logger.LogInformation("gRPC GetPatientFavorites completed for Patient {PatientId}, returned {ItemCount} items of {TotalCount} total",
                request.PatientId, serviceResponse.Items.Count, serviceResponse.TotalCount);

            return grpcResponse;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in gRPC GetPatientFavorites for Patient {PatientId}", request.PatientId);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetPatientFavorites for Patient {PatientId}", request.PatientId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }

    }
}