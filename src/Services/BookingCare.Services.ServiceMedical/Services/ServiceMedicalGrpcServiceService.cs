using Grpc.Core;
using BookingCare.Services.ServiceMedical.Protos;

namespace BookingCare.Services.ServiceMedical.Services;

public class ServiceMedicalGrpcService : ServiceMedicalService.ServiceMedicalServiceBase
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";
    private readonly ILogger<ServiceMedicalGrpcService> _logger;

    public ServiceMedicalGrpcService(ILogger<ServiceMedicalGrpcService> logger)
    {
        _logger = logger;
    }

    public override Task<ServiceMedicalResponse> GetServiceMedical(GetServiceMedicalRequest request, ServerCallContext context)
    {
        // NOTE: Currently returning mock data. Implement database lookup when service repository is available.
        _logger.LogInformation("Getting service medical with ID: {ServiceId}", request.Id);

        return Task.FromResult(new ServiceMedicalResponse
        {
            Id = request.Id,
            Name = "Sample Medical Service",
            Description = "This is a sample medical service",
            Category = "General",
            Price = 100.00,
            DurationMinutes = 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.ToString(DateTimeFormat),
            UpdatedAt = DateTime.UtcNow.ToString(DateTimeFormat)
        });
    }

    public override Task<ValidateServiceMedicalResponse> ValidateServiceMedical(ValidateServiceMedicalRequest request, ServerCallContext context)
    {
        // NOTE: Currently using simple GUID validation. Implement database validation when service repository is available.
        _logger.LogInformation("Validating service medical with ID: {ServiceId}", request.Id);

        // For now, return valid for any GUID format
        var isValidGuid = Guid.TryParse(request.Id, out _);

        return Task.FromResult(new ValidateServiceMedicalResponse
        {
            IsValid = isValidGuid,
            IsActive = isValidGuid, // Assume active if valid GUID
            Message = isValidGuid ? "Service is valid and active" : "Invalid service ID format"
        });
    }

    public override Task<ServiceMedicalBatchResponse> GetServiceMedicalsByIds(GetServiceMedicalsByIdsRequest request, ServerCallContext context)
    {
        // NOTE: Currently returning mock data for batch requests. Implement database batch lookup when service repository is available.
        _logger.LogInformation("Getting multiple service medicals for {Count} IDs", request.Ids.Count);

        var response = new ServiceMedicalBatchResponse();

        foreach (var id in request.Ids)
        {
            if (Guid.TryParse(id, out _))
            {
                response.Services.Add(new ServiceMedicalResponse
                {
                    Id = id,
                    Name = $"Medical Service {id[..8]}",
                    Description = "Sample medical service",
                    Category = "General",
                    Price = 100.00,
                    DurationMinutes = 30,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.ToString(DateTimeFormat),
                    UpdatedAt = DateTime.UtcNow.ToString(DateTimeFormat)
                });
            }
        }

        return Task.FromResult(response);
    }
}
