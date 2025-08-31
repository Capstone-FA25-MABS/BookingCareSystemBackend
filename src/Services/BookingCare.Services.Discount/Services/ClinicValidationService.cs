using BookingCare.Services.Clinic.Protos;
using Grpc.Core;
using Grpc.Net.Client;

namespace BookingCare.Services.Discount.Services;

/// <summary>
/// Service for validating clinics via gRPC calls to the Clinic service
/// </summary>
public class ClinicValidationService : IClinicValidationService
{
    private readonly Clinic.Protos.ClinicValidationService.ClinicValidationServiceClient _clinicClient;
    private readonly ILogger<ClinicValidationService> _logger;
    private readonly string _clinicServiceUrl;

    public ClinicValidationService(
        IConfiguration configuration,
        ILogger<ClinicValidationService> logger)
    {
        _logger = logger;
        _clinicServiceUrl = configuration.GetConnectionString("ClinicServiceUrl") 
                          ?? configuration.GetValue<string>("Services:Clinic:Url") 
                          ?? "http://localhost:6003"; // Default fallback

        var channel = GrpcChannel.ForAddress(_clinicServiceUrl);
        _clinicClient = new Clinic.Protos.ClinicValidationService.ClinicValidationServiceClient(channel);
    }

    /// <summary>
    /// Validates if a clinic exists and is active
    /// </summary>
    /// <param name="clinicId">The clinic ID to validate</param>
    /// <returns>True if clinic is valid and active, false otherwise</returns>
    public async Task<bool> ValidateClinicAsync(long clinicId)
    {
        try
        {
            _logger.LogInformation("Validating clinic ID: {ClinicId} via gRPC", clinicId);

            var request = new ValidateClinicRequest { ClinicId = clinicId };
            
            using var call = _clinicClient.ValidateClinicAsync(request, deadline: DateTime.UtcNow.AddSeconds(30));
            var response = await call.ResponseAsync;

            _logger.LogInformation("Clinic validation result - ID: {ClinicId}, Valid: {IsValid}, Active: {IsActive}", 
                clinicId, response.IsValid, response.IsActive);

            return response.IsValid && response.IsActive;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            _logger.LogWarning("Clinic validation timeout for ID: {ClinicId}", clinicId);
            return false;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogWarning("Clinic service unavailable for validation of ID: {ClinicId}", clinicId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating clinic ID: {ClinicId}", clinicId);
            return false;
        }
    }

    /// <summary>
    /// Gets clinic information including validation status
    /// </summary>
    /// <param name="clinicId">The clinic ID to get information for</param>
    /// <returns>Clinic validation response with detailed information</returns>
    public async Task<ValidateClinicResponse> GetClinicValidationAsync(long clinicId)
    {
        try
        {
            _logger.LogInformation("Getting clinic validation details for ID: {ClinicId} via gRPC", clinicId);

            var request = new ValidateClinicRequest { ClinicId = clinicId };
            
            using var call = _clinicClient.ValidateClinicAsync(request, deadline: DateTime.UtcNow.AddSeconds(30));
            var response = await call.ResponseAsync;

            _logger.LogInformation("Retrieved clinic validation details - ID: {ClinicId}, Valid: {IsValid}", 
                clinicId, response.IsValid);

            return response;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            _logger.LogWarning("Clinic validation timeout for ID: {ClinicId}", clinicId);
            return new ValidateClinicResponse
            {
                IsValid = false,
                IsActive = false,
                Message = "Clinic service timeout"
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogWarning("Clinic service unavailable for validation of ID: {ClinicId}", clinicId);
            return new ValidateClinicResponse
            {
                IsValid = false,
                IsActive = false,
                Message = "Clinic service unavailable"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clinic validation details for ID: {ClinicId}", clinicId);
            return new ValidateClinicResponse
            {
                IsValid = false,
                IsActive = false,
                Message = $"Error validating clinic: {ex.Message}"
            };
        }
    }
}
