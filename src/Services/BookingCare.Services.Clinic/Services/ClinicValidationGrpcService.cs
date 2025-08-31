using BookingCare.Services.Clinic.Protos;
using Grpc.Core;

namespace BookingCare.Services.Clinic.Services;

/// <summary>
/// gRPC service implementation for clinic validation
/// </summary>
public class ClinicValidationGrpcService : ClinicValidationService.ClinicValidationServiceBase
{
    private readonly ILogger<ClinicValidationGrpcService> _logger;

    public ClinicValidationGrpcService(ILogger<ClinicValidationGrpcService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates if a clinic exists and is active
    /// </summary>
    public override async Task<ValidateClinicResponse> ValidateClinic(
        ValidateClinicRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC ValidateClinic called for clinic ID: {ClinicId}", request.ClinicId);

            // TODO: Replace with actual database lookup
            // For now, we'll simulate clinic validation
            var isValidClinic = await SimulateClinicValidation(request.ClinicId);

            var response = new ValidateClinicResponse
            {
                IsValid = isValidClinic.IsValid,
                IsActive = isValidClinic.IsActive,
                Message = isValidClinic.Message
            };

            if (isValidClinic.IsValid)
            {
                response.ClinicInfo = new ClinicInfo
                {
                    Id = request.ClinicId,
                    Name = $"Clinic {request.ClinicId}",
                    Address = "123 Medical Center St",
                    Phone = "+1-555-0100",
                    Email = $"clinic{request.ClinicId}@example.com",
                    IsActive = isValidClinic.IsActive,
                    Status = isValidClinic.IsActive ? "ACTIVE" : "INACTIVE"
                };
            }

            _logger.LogInformation("Clinic validation result - ID: {ClinicId}, Valid: {IsValid}, Active: {IsActive}", 
                request.ClinicId, response.IsValid, response.IsActive);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC ValidateClinic for clinic ID: {ClinicId}", request.ClinicId);
            
            return new ValidateClinicResponse
            {
                IsValid = false,
                IsActive = false,
                Message = "Internal server error during clinic validation"
            };
        }
    }

    /// <summary>
    /// Gets basic clinic information
    /// </summary>
    public override async Task<GetClinicInfoResponse> GetClinicInfo(
        GetClinicInfoRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetClinicInfo called for clinic ID: {ClinicId}", request.ClinicId);

            // TODO: Replace with actual database lookup
            var clinicInfo = await SimulateGetClinicInfo(request.ClinicId);

            var response = new GetClinicInfoResponse
            {
                Found = clinicInfo.Found,
                Message = clinicInfo.Message
            };

            if (clinicInfo.Found)
            {
                response.ClinicInfo = new ClinicInfo
                {
                    Id = request.ClinicId,
                    Name = $"Clinic {request.ClinicId}",
                    Address = "123 Medical Center St",
                    Phone = "+1-555-0100",
                    Email = $"clinic{request.ClinicId}@example.com",
                    IsActive = true,
                    Status = "ACTIVE"
                };
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetClinicInfo for clinic ID: {ClinicId}", request.ClinicId);
            
            return new GetClinicInfoResponse
            {
                Found = false,
                Message = "Internal server error while getting clinic information"
            };
        }
    }

    /// <summary>
    /// Simulates clinic validation - replace with actual database logic
    /// </summary>
    private async Task<(bool IsValid, bool IsActive, string Message)> SimulateClinicValidation(long clinicId)
    {
        await Task.Delay(10); // Simulate async database call

        // For demonstration purposes:
        // - Clinic IDs 1-100 are valid and active
        // - Clinic IDs 101-200 are valid but inactive
        // - Any other ID is invalid
        
        if (clinicId >= 1 && clinicId <= 100)
        {
            return (true, true, "Clinic is valid and active");
        }
        else if (clinicId >= 101 && clinicId <= 200)
        {
            return (true, false, "Clinic is valid but inactive");
        }
        else
        {
            return (false, false, "Clinic not found");
        }
    }

    /// <summary>
    /// Simulates getting clinic information - replace with actual database logic
    /// </summary>
    private async Task<(bool Found, string Message)> SimulateGetClinicInfo(long clinicId)
    {
        await Task.Delay(10); // Simulate async database call

        if (clinicId >= 1 && clinicId <= 200)
        {
            return (true, "Clinic information retrieved successfully");
        }
        else
        {
            return (false, "Clinic not found");
        }
    }
}
