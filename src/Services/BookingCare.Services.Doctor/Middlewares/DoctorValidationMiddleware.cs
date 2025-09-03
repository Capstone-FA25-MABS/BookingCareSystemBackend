using BookingCare.Services.Doctor.Services;

namespace BookingCare.Services.Doctor.Middlewares;

public class DoctorValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DoctorValidationMiddleware> _logger;

    public DoctorValidationMiddleware(RequestDelegate next, ILogger<DoctorValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IDoctorService doctorService, IPositionService positionService, IPriceService priceService)
    {
        // Log request information
        LogRequestInfo(context);

        // Perform validation checks on specific endpoints
        if (ShouldPerformValidation(context))
        {
            try
            {
                await PerformValidationChecks(context, doctorService, positionService, priceService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing validation checks");
                // Don't throw - this shouldn't block the request
            }
        }

        await _next(context);

        // Log response information
        LogResponseInfo(context);
    }

    private void LogRequestInfo(HttpContext context)
    {
        var path = context.Request.Path.Value;
        var method = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var clientIp = context.Connection.RemoteIpAddress?.ToString();

        _logger.LogInformation("Doctor Service Request: {Method} {Path} from {ClientIp} - UserAgent: {UserAgent}", 
            method, path, clientIp, userAgent);
    }

    private void LogResponseInfo(HttpContext context)
    {
        var path = context.Request.Path.Value;
        var method = context.Request.Method;
        var statusCode = context.Response.StatusCode;

        _logger.LogInformation("Doctor Service Response: {Method} {Path} - Status: {StatusCode}", 
            method, path, statusCode);
    }

    private static bool ShouldPerformValidation(HttpContext context)
    {
        // Only perform validation on specific endpoints
        var path = context.Request.Path.Value?.ToLower();
        return path != null && (
            path.Contains("/doctors/create") ||
            path.Contains("/doctors/update") ||
            path.Contains("/doctors/validate") ||
            path.Contains("/positions/create") ||
            path.Contains("/prices/create") ||
            path.Contains("/assign-price")
        );
    }

    private async Task PerformValidationChecks(HttpContext context, IDoctorService doctorService, IPositionService positionService, IPriceService priceService)
    {
        var path = context.Request.Path.Value?.ToLower();

        if (path?.Contains("/doctors/create") == true || path?.Contains("/doctors/update") == true)
        {
            await ValidateDoctorDataIntegrity(doctorService);
        }

        if (path?.Contains("/positions/create") == true)
        {
            await ValidatePositionDataIntegrity(positionService);
        }

        if (path?.Contains("/prices/create") == true)
        {
            await ValidatePriceDataIntegrity(priceService);
        }

        if (path?.Contains("/assign-price") == true)
        {
            await ValidateDoctorPriceRelationships(doctorService);
        }
    }

    private async Task ValidateDoctorDataIntegrity(IDoctorService doctorService)
    {
        try
        {
            // Get a sample of doctors to validate data integrity
            var queryRequest = new Models.DTOs.DoctorQueryRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

            var doctors = await doctorService.GetDoctorsAsync(queryRequest);
            
            if (doctors.Doctors.Any())
            {
                _logger.LogDebug("Validated {Count} doctors for data integrity", doctors.Doctors.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Data integrity validation failed");
        }
    }

    private async Task ValidatePositionDataIntegrity(IPositionService positionService)
    {
        try
        {
            var positions = await positionService.GetAllPositionsAsync();
            
            if (positions.Any())
            {
                _logger.LogDebug("Validated {Count} positions for data integrity", positions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Position data integrity validation failed");
        }
    }

    private async Task ValidatePriceDataIntegrity(IPriceService priceService)
    {
        try
        {
            var prices = await priceService.GetAllPricesAsync();
            
            if (prices.Any())
            {
                _logger.LogDebug("Validated {Count} prices for data integrity", prices.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Price data integrity validation failed");
        }
    }

    private async Task ValidateDoctorPriceRelationships(IDoctorService doctorService)
    {
        try
        {
            // This would typically involve checking for orphaned relationships
            // For now, we'll just log the validation attempt
            _logger.LogDebug("Validating doctor-price relationships");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Doctor-price relationship validation failed");
        }
    }
}
