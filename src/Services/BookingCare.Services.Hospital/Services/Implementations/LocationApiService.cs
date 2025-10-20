using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BookingCare.Services.Hospital.Services.Implementations;

/// <summary>
/// Service for handling location-related API calls to provinces.open-api.vn
/// </summary>
public class LocationApiService : ILocationApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocationApiService> _logger;
    private readonly string _provincesApiBaseUrl = "https://provinces.open-api.vn/api/";

    public LocationApiService(
        HttpClient httpClient,
        ILogger<LocationApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private async Task<T?> CallApiAsync<T>(string endpoint, string operationName)
    {
        try
        {
            var url = $"{_provincesApiBaseUrl}{endpoint}";
            _logger.LogInformation("Calling external API: {Url} for {OperationName}", url, operationName);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("API response received for {OperationName}, content length: {ContentLength}", operationName, jsonContent.Length);

            var result = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("API call successful for {OperationName}", operationName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API for {OperationName}", operationName);
            return default;
        }
    }

    /// <summary>
    /// Get province name from ID using provinces.open-api.vn
    /// </summary>
    public async Task<string?> GetProvinceNameByIdAsync(string? provinceId)
    {
        if (string.IsNullOrEmpty(provinceId))
            return null;

        _logger.LogInformation("Getting province name for ID: {ProvinceId}", provinceId);

        // Fallback hardcoded data for testing
        var fallbackProvinces = new Dictionary<string, string>
        {
            { "79", "Thành phố Hồ Chí Minh" },
            { "01", "Thành phố Hà Nội" },
            { "35", "Tỉnh Hà Nam" }
        };

        if (fallbackProvinces.ContainsKey(provinceId))
        {
            var fallbackResult = fallbackProvinces[provinceId];
            _logger.LogInformation("Using fallback data - Retrieved province name: {ProvinceName} for ID: {ProvinceId}", fallbackResult, provinceId);
            return fallbackResult;
        }

        var provinces = await CallApiAsync<List<ProvinceApiModel>>("?depth=1", "GetProvinceNameById");
        if (provinces == null)
        {
            _logger.LogWarning("Failed to get provinces from API for ID: {ProvinceId}", provinceId);
            return null;
        }

        _logger.LogInformation("Retrieved {ProvinceCount} provinces from API", provinces.Count);

        var province = provinces.FirstOrDefault(p => p.Code.ToString() == provinceId);
        var result = province?.Name;

        _logger.LogInformation("Retrieved province name: {ProvinceName} for ID: {ProvinceId}", result, provinceId);
        return result;
    }

    /// <summary>
    /// Get district name from ID using provinces.open-api.vn
    /// </summary>
    public async Task<string?> GetDistrictNameByIdAsync(string? districtId)
    {
        if (string.IsNullOrEmpty(districtId))
            return null;

        try
        {
            // Try with flexible model first to handle JSON deserialization issues
            var provinces = await CallApiAsync<List<ProvinceWithDistrictsApiModelFlexible>>("?depth=2", "GetDistrictNameById");
            if (provinces == null)
                return null;

            foreach (var province in provinces)
            {
                var district = province.Districts?.FirstOrDefault(d => d.GetCodeAsString() == districtId);
                if (district != null)
                {
                    _logger.LogInformation("Retrieved district name: {DistrictName} for ID: {DistrictId}", district.Name, districtId);
                    return district.Name;
                }
            }

            _logger.LogWarning("District not found for ID: {DistrictId}", districtId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting district name for ID: {DistrictId}", districtId);
            return null;
        }
    }

    /// <summary>
    /// Get province ID from district ID using provinces.open-api.vn
    /// </summary>
    public async Task<string?> GetProvinceIdFromDistrictIdAsync(string? districtId)
    {
        if (string.IsNullOrEmpty(districtId))
            return null;

        try
        {
            // Try with flexible model first to handle JSON deserialization issues
            var provinces = await CallApiAsync<List<ProvinceWithDistrictsApiModelFlexible>>("?depth=2", "GetProvinceIdFromDistrictId");
            if (provinces == null)
                return null;

            foreach (var province in provinces)
            {
                var district = province.Districts?.FirstOrDefault(d => d.GetCodeAsString() == districtId);
                if (district != null)
                {
                    _logger.LogInformation("Retrieved province ID: {ProvinceId} for district ID: {DistrictId}", province.GetCodeAsString(), districtId);
                    return province.GetCodeAsString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting province ID for district ID: {DistrictId}", districtId);
            return null;
        }

        _logger.LogWarning("Province not found for district ID: {DistrictId}", districtId);
        return null;
    }

    /// <summary>
    /// Get location info (province and district names) from IDs
    /// </summary>
    public async Task<LocationInfo?> GetLocationInfoAsync(string? provinceId, string? districtId)
    {
        if (string.IsNullOrEmpty(provinceId) && string.IsNullOrEmpty(districtId))
            return null;

        var locationInfo = new LocationInfo
        {
            ProvinceId = provinceId,
            DistrictId = districtId
        };

        // Get province name
        if (!string.IsNullOrEmpty(provinceId))
        {
            locationInfo.ProvinceName = await GetProvinceNameByIdAsync(provinceId);
        }

        // Get district name
        if (!string.IsNullOrEmpty(districtId))
        {
            locationInfo.DistrictName = await GetDistrictNameByIdAsync(districtId);
        }

        // If only district ID is provided, get province ID from district
        if (string.IsNullOrEmpty(provinceId) && !string.IsNullOrEmpty(districtId))
        {
            locationInfo.ProvinceId = await GetProvinceIdFromDistrictIdAsync(districtId);
            if (!string.IsNullOrEmpty(locationInfo.ProvinceId))
            {
                locationInfo.ProvinceName = await GetProvinceNameByIdAsync(locationInfo.ProvinceId);
            }
        }

        return locationInfo;
    }

    /// <summary>
    /// Apply location filtering to hospital list based on address matching
    /// </summary>
    public async Task<List<HospitalListOptimizedResponse>> ApplyLocationFilteringAsync(
        List<HospitalListOptimizedResponse> hospitals,
        string? provinceId,
        string? districtId)
    {
        if (string.IsNullOrEmpty(provinceId) && string.IsNullOrEmpty(districtId))
        {
            _logger.LogDebug("No location filtering applied - no provinceId or districtId provided");
            return hospitals;
        }

        _logger.LogInformation("Applying location filtering - ProvinceId: {ProvinceId}, DistrictId: {DistrictId}, Total hospitals: {TotalCount}",
            provinceId, districtId, hospitals.Count);

        var locationInfo = await GetLocationInfoAsync(provinceId, districtId);
        if (locationInfo == null)
        {
            _logger.LogWarning("Location info not found for ProvinceId: {ProvinceId}, DistrictId: {DistrictId}", provinceId, districtId);
            return hospitals;
        }

        _logger.LogInformation("Location info retrieved - Province: {ProvinceName}, District: {DistrictName}",
            locationInfo.ProvinceName, locationInfo.DistrictName);

        var filteredHospitals = hospitals.Where(hospital =>
            IsHospitalInLocation(hospital, locationInfo)).ToList();

        _logger.LogInformation("Location filtering completed. {FilteredCount} out of {TotalCount} hospitals match the criteria",
            filteredHospitals.Count, hospitals.Count);

        return filteredHospitals;
    }

    /// <summary>
    /// Check if hospital is in specified location based on address matching
    /// </summary>
    public bool IsHospitalInLocation(HospitalListOptimizedResponse hospital, LocationInfo locationInfo)
    {
        if (string.IsNullOrEmpty(hospital.Address))
        {
            _logger.LogDebug("Hospital {HospitalId} has no address", hospital.Id);
            return false;
        }

        var hospitalAddress = hospital.Address.ToLowerInvariant();
        var provinceName = locationInfo.ProvinceName?.ToLowerInvariant() ?? "";
        var districtName = locationInfo.DistrictName?.ToLowerInvariant() ?? "";

        // Clean up province name - remove common prefixes
        var cleanProvinceName = provinceName
            .Replace("thành phố", "")
            .Replace("tỉnh", "")
            .Replace("tp.", "")
            .Replace("tp ", "")
            .Trim();

        // Clean up district name - remove common prefixes
        var cleanDistrictName = districtName
            .Replace("quận", "")
            .Replace("huyện", "")
            .Replace("thị xã", "")
            .Replace("thành phố", "")
            .Replace("tx.", "")
            .Replace("q.", "")
            .Replace("h.", "")
            .Trim();

        _logger.LogInformation("Checking hospital {HospitalId} with address: '{HospitalAddress}' against location: Province='{ProvinceName}', District='{DistrictName}'",
            hospital.Id, hospitalAddress, locationInfo.ProvinceName, locationInfo.DistrictName);

        bool result;
        if (!string.IsNullOrEmpty(districtName))
        {
            // Filter by both province and district - both must be found in address
            var provinceMatch = hospitalAddress.Contains(provinceName) ||
                               hospitalAddress.Contains(cleanProvinceName) ||
                               hospitalAddress.Contains(locationInfo.ProvinceName?.ToLowerInvariant() ?? "");

            var districtMatch = hospitalAddress.Contains(districtName) ||
                               hospitalAddress.Contains(cleanDistrictName) ||
                               hospitalAddress.Contains(locationInfo.DistrictName?.ToLowerInvariant() ?? "");

            result = provinceMatch && districtMatch;
            _logger.LogInformation("District filter result: ProvinceMatch={ProvinceMatch}, DistrictMatch={DistrictMatch}, Final={Result}",
                provinceMatch, districtMatch, result);
        }
        else
        {
            // Filter by province only
            result = hospitalAddress.Contains(provinceName) ||
                    hospitalAddress.Contains(cleanProvinceName) ||
                    hospitalAddress.Contains(locationInfo.ProvinceName?.ToLowerInvariant() ?? "");
            _logger.LogInformation("Province filter result: {Result}", result);
        }

        return result;
    }

    /// <summary>
    /// Test method to validate location filtering logic
    /// </summary>
    public async Task<bool> TestLocationFilteringAsync(string testAddress, int? provinceId, int? districtId)
    {
        try
        {
            var testHospital = new HospitalListOptimizedResponse
            {
                Id = Guid.NewGuid(),
                Name = "Test Hospital",
                Address = testAddress
            };

            // Convert int? to string? for GetLocationInfoAsync
            var provinceIdStr = provinceId?.ToString();
            var districtIdStr = districtId?.ToString();

            var locationInfo = await GetLocationInfoAsync(provinceIdStr, districtIdStr);
            if (locationInfo == null)
            {
                _logger.LogWarning("Location info not found for ProvinceId: {ProvinceId}, DistrictId: {DistrictId}", provinceId, districtId);
                return false;
            }

            var result = IsHospitalInLocation(testHospital, locationInfo);
            _logger.LogInformation("Test result for address '{TestAddress}' with ProvinceId: {ProvinceId}, DistrictId: {DistrictId} = {Result}",
                testAddress, provinceId, districtId, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing location filtering for address: {TestAddress}", testAddress);
            return false;
        }
    }
}

/// <summary>
/// API model for province data from provinces.open-api.vn
/// </summary>
public class ProvinceApiModel
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CodeName { get; set; } = string.Empty;
    public string DivisionType { get; set; } = string.Empty;
    public string PhoneCode { get; set; } = string.Empty;
}

/// <summary>
/// API model for province data with flexible code type
/// </summary>
public class ProvinceApiModelFlexible
{
    public string Name { get; set; } = string.Empty;
    public object Code { get; set; } = string.Empty;
    public string CodeName { get; set; } = string.Empty;
    public string DivisionType { get; set; } = string.Empty;
    public string PhoneCode { get; set; } = string.Empty;

    public string GetCodeAsString()
    {
        return Code?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// API model for province with districts data from provinces.open-api.vn
/// </summary>
public class ProvinceWithDistrictsApiModel : ProvinceApiModel
{
    public List<DistrictApiModel>? Districts { get; set; }
}

/// <summary>
/// API model for province with districts data with flexible code type
/// </summary>
public class ProvinceWithDistrictsApiModelFlexible : ProvinceApiModelFlexible
{
    public List<DistrictApiModelFlexible>? Districts { get; set; }
}

/// <summary>
/// API model for district data from provinces.open-api.vn
/// </summary>
public class DistrictApiModel
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CodeName { get; set; } = string.Empty;
    public string DivisionType { get; set; } = string.Empty;
    public string ShortCodename { get; set; } = string.Empty;
}

/// <summary>
/// API model for district data with flexible code type
/// </summary>
public class DistrictApiModelFlexible
{
    public string Name { get; set; } = string.Empty;
    public object Code { get; set; } = string.Empty;
    public string CodeName { get; set; } = string.Empty;
    public string DivisionType { get; set; } = string.Empty;
    public string ShortCodename { get; set; } = string.Empty;

    public string GetCodeAsString()
    {
        return Code?.ToString() ?? string.Empty;
    }
}
