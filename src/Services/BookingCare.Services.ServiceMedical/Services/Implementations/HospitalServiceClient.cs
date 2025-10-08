using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;
using BookingCare.Services.ServiceMedical.Services.Interfaces;
using System.Text.Json;

namespace BookingCare.Services.ServiceMedical.Services.Implementations
{
    /// <summary>
    /// Implementation of Hospital Service Client
    /// </summary>
    public class HospitalServiceClient : IHospitalServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HospitalServiceClient> _logger;
        private readonly IConfiguration _configuration;

        public HospitalServiceClient(HttpClient httpClient, ILogger<HospitalServiceClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<HospitalInfoResponse?> GetHospitalByIdAsync(Guid hospitalId)
        {
            try
            {
                var hospitalServiceUrl = _configuration["HospitalService:BaseUrl"] ?? "http://localhost:6002";
                var endpoint = $"{hospitalServiceUrl}/api/Hospitals/{hospitalId}";
                
                _logger.LogInformation("Calling Hospital Service: {Endpoint}", endpoint);
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var hospital = JsonSerializer.Deserialize<HospitalInfoResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    _logger.LogInformation("Successfully retrieved hospital {HospitalId}", hospitalId);
                    return hospital;
                }
                else
                {
                    _logger.LogWarning("Failed to retrieve hospital {HospitalId}. Status: {StatusCode}", hospitalId, response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Hospital Service for hospital {HospitalId}", hospitalId);
                return null;
            }
        }

        public async Task<List<HospitalInfoResponse>> GetHospitalsByIdsAsync(List<Guid> hospitalIds)
        {
            try
            {
                var hospitalServiceUrl = _configuration["HospitalService:BaseUrl"] ?? "http://localhost:6002";
                var endpoint = $"{hospitalServiceUrl}/api/Hospitals/batch";
                
                _logger.LogInformation("Calling Hospital Service batch endpoint: {Endpoint}", endpoint);
                
                var requestBody = new { ids = hospitalIds };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var hospitals = JsonSerializer.Deserialize<List<HospitalInfoResponse>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    _logger.LogInformation("Successfully retrieved {Count} hospitals", hospitals?.Count ?? 0);
                    return hospitals ?? new List<HospitalInfoResponse>();
                }
                else
                {
                    _logger.LogWarning("Failed to retrieve hospitals batch. Status: {StatusCode}", response.StatusCode);
                    return new List<HospitalInfoResponse>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Hospital Service batch endpoint");
                return new List<HospitalInfoResponse>();
            }
        }
    }
}
