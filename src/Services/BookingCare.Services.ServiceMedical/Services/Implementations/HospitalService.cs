using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;
using BookingCare.Services.ServiceMedical.Services.Interfaces;
using BookingCare.Services.Hospital;
using Grpc.Core;

namespace BookingCare.Services.ServiceMedical.Services.Implementations
{
    /// <summary>
    /// Implementation of Hospital Service integration
    /// </summary>
    public class HospitalService : IHospitalService
    {
        private readonly BookingCare.Services.Hospital.HospitalService.HospitalServiceClient _grpcClient;
        private readonly ILogger<HospitalService> _logger;

        public HospitalService(BookingCare.Services.Hospital.HospitalService.HospitalServiceClient grpcClient, ILogger<HospitalService> logger)
        {
            _grpcClient = grpcClient;
            _logger = logger;
        }

        public async Task<HospitalInfoResponse?> GetHospitalByIdAsync(Guid hospitalId)
        {
            try
            {
                var request = new GetHospitalRequest
                {
                    Id = hospitalId.ToString()
                };

                _logger.LogInformation("Calling Hospital Service gRPC: GetHospital for {HospitalId}", hospitalId);

                var response = await _grpcClient.GetHospitalAsync(request);

                if (response != null && !string.IsNullOrEmpty(response.Id))
                {
                    var hospitalInfo = MapToHospitalInfoResponse(response);
                    _logger.LogInformation("Successfully retrieved hospital {HospitalId} via gRPC", hospitalId);
                    return hospitalInfo;
                }

                _logger.LogWarning("Hospital {HospitalId} not found via gRPC", hospitalId);
                return null;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error retrieving hospital {HospitalId}. Status: {StatusCode}", hospitalId, ex.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Hospital Service gRPC for hospital {HospitalId}", hospitalId);
                return null;
            }
        }

        public async Task<List<HospitalInfoResponse>> GetHospitalsByIdsAsync(List<Guid> hospitalIds)
        {
            try
            {
                if (hospitalIds == null || !hospitalIds.Any())
                {
                    return new List<HospitalInfoResponse>();
                }

                var request = new GetHospitalsBasicInfoRequest();
                request.Ids.AddRange(hospitalIds.Select(id => id.ToString()));

                _logger.LogInformation("Calling Hospital Service gRPC: GetHospitalsBasicInfo for {Count} hospitals", hospitalIds.Count);

                var response = await _grpcClient.GetHospitalsBasicInfoAsync(request);

                if (response?.Hospitals != null)
                {
                    var hospitals = response.Hospitals.Select(MapToHospitalInfoResponse).ToList();
                    _logger.LogInformation("Successfully retrieved {Count} hospitals via gRPC", hospitals.Count);
                    return hospitals;
                }

                _logger.LogWarning("No hospitals found via gRPC for {Count} IDs", hospitalIds.Count);
                return new List<HospitalInfoResponse>();
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error retrieving hospitals batch. Status: {StatusCode}", ex.StatusCode);
                return new List<HospitalInfoResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Hospital Service gRPC batch endpoint");
                return new List<HospitalInfoResponse>();
            }
        }

        private static HospitalInfoResponse MapToHospitalInfoResponse(HospitalReply grpcResponse)
        {
            return new HospitalInfoResponse
            {
                Id = Guid.Parse(grpcResponse.Id),
                AccountId = Guid.Parse(grpcResponse.AccountId),
                Name = grpcResponse.Name,
                Address = grpcResponse.Address,
                Phone = grpcResponse.Phone,
                Email = grpcResponse.Email,
                Description = grpcResponse.Description,
                BackgroundUrl = grpcResponse.BackgroundUrl,
                AvatarUrl = grpcResponse.AvatarUrl,
                CreatedAt = DateTime.TryParse(grpcResponse.CreatedAt, null, System.Globalization.DateTimeStyles.None, out var createdAt) ? createdAt : DateTime.MinValue,
                UpdatedAt = DateTime.TryParse(grpcResponse.UpdatedAt, null, System.Globalization.DateTimeStyles.None, out var updatedAt) ? updatedAt : DateTime.MinValue,
                Specialties = new List<HospitalSpecialtyInfoResponse>(), // Will be populated if needed
                Images = new List<HospitalImageInfoResponse>(), // Will be populated if needed
                CurrentSubscription = null // Will be populated if needed
            };
        }

        private static HospitalInfoResponse MapToHospitalInfoResponse(HospitalBasicInfoResponse grpcResponse)
        {
            return new HospitalInfoResponse
            {
                Id = Guid.Parse(grpcResponse.Id),
                AccountId = Guid.Empty, // Not available in basic info
                Name = grpcResponse.Name,
                Address = grpcResponse.Address,
                Phone = grpcResponse.Phone,
                Email = grpcResponse.Email,
                Description = string.Empty, // Not available in basic info
                BackgroundUrl = string.Empty, // Not available in basic info
                AvatarUrl = grpcResponse.AvatarUrl,
                CreatedAt = DateTime.MinValue, // Not available in basic info
                UpdatedAt = DateTime.MinValue, // Not available in basic info
                Specialties = new List<HospitalSpecialtyInfoResponse>(),
                Images = new List<HospitalImageInfoResponse>(),
                CurrentSubscription = null
            };
        }
    }
}
