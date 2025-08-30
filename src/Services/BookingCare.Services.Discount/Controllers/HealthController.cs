using BookingCare.Shared.Common.CircuitBreaker;
using BookingCare.Shared.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;

namespace BookingCare.Services.Discount.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : BaseApiController
{
    private readonly IDatabaseCircuitBreakerService _databaseCircuitBreaker;
    private readonly IHttpClientCircuitBreakerService _httpClientCircuitBreaker;
    private readonly IExternalApiCircuitBreakerService _externalApiCircuitBreaker;

    public HealthController(
        IDatabaseCircuitBreakerService databaseCircuitBreaker,
        IHttpClientCircuitBreakerService httpClientCircuitBreaker,
        IExternalApiCircuitBreakerService externalApiCircuitBreaker)
    {
        _databaseCircuitBreaker = databaseCircuitBreaker;
        _httpClientCircuitBreaker = httpClientCircuitBreaker;
        _externalApiCircuitBreaker = externalApiCircuitBreaker;
    }

    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "BookingCare.Services.Discount"
        });
    }

    [HttpGet("circuit-breaker")]
    public IActionResult GetCircuitBreakerStatus()
    {
        var status = new
        {
            CircuitBreakers = new
            {
                Database = new
                {
                    State = _databaseCircuitBreaker.GetCircuitBreakerState().ToString(),
                    Type = "Database Operations"
                },
                HttpClient = new
                {
                    State = _httpClientCircuitBreaker.GetCircuitBreakerState().ToString(),
                    Type = "HTTP Client Operations"
                },
                ExternalApi = new
                {
                    State = _externalApiCircuitBreaker.GetCircuitBreakerState().ToString(),
                    Type = "External API Operations"
                }
            },
            Timestamp = DateTime.UtcNow
        };

        return Ok(status);
    }

    [HttpPost("circuit-breaker/reset")]
    public IActionResult ResetCircuitBreakers()
    {
        _databaseCircuitBreaker.ResetCircuitBreaker();
        _httpClientCircuitBreaker.ResetCircuitBreaker();
        _externalApiCircuitBreaker.ResetCircuitBreaker();

        return Ok(new
        {
            Message = "All circuit breakers have been reset",
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("circuit-breaker/reset/{type}")]
    public IActionResult ResetSpecificCircuitBreaker(string type)
    {
        switch (type.ToLower())
        {
            case "database":
                _databaseCircuitBreaker.ResetCircuitBreaker();
                break;
            case "httpclient":
                _httpClientCircuitBreaker.ResetCircuitBreaker();
                break;
            case "externalapi":
                _externalApiCircuitBreaker.ResetCircuitBreaker();
                break;
            default:
                return BadRequest(new { Message = "Invalid circuit breaker type. Valid types: database, httpclient, externalapi" });
        }

        return Ok(new
        {
            Message = $"{type} circuit breaker has been reset",
            Timestamp = DateTime.UtcNow
        });
    }
}
