using BookingCare.Services.Doctor.Models.ApiModels;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace BookingCare.Services.Doctor.Services.Implementations;

/// <summary>
/// Service for handling location-related API calls to provinces.open-api.vn
/// </summary>
public class LocationApiService : BaseService, ILocationApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _provincesApiBaseUrl = "https://provinces.open-api.vn/api/";

    public LocationApiService(
        HttpClient httpClient,
        ILogger<LocationApiService> logger) : base(logger)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get province name from ID using provinces.open-api.vn
    /// </summary>
    public async Task<string?> GetProvinceNameByIdAsync(string? provinceId)
    {
        if (string.IsNullOrEmpty(provinceId))
            return null;

        try
        {
            var response = await _httpClient.GetAsync($"{_provincesApiBaseUrl}?depth=1");
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var provinces = JsonSerializer.Deserialize<List<ProvinceApiModel>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var province = provinces?.FirstOrDefault(p => p.Code.ToString() == provinceId);
            var result = province?.Name;

            Logger.LogDebug("Retrieved province name: {ProvinceName} for ID: {ProvinceId}", result, provinceId);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting province name from API for ID: {ProvinceId}", provinceId);
            return null;
        }
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
            var response = await _httpClient.GetAsync($"{_provincesApiBaseUrl}?depth=2");
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var provinces = JsonSerializer.Deserialize<List<ProvinceWithDistrictsApiModel>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            foreach (var province in provinces ?? new List<ProvinceWithDistrictsApiModel>())
            {
                var district = province.Districts?.FirstOrDefault(d => d.Code.ToString() == districtId);
                if (district != null)
                {
                    Logger.LogDebug("Retrieved district name: {DistrictName} for ID: {DistrictId}", district.Name, districtId);
                    return district.Name;
                }
            }

            Logger.LogWarning("District not found for ID: {DistrictId}", districtId);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting district name from API for ID: {DistrictId}", districtId);
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
            var response = await _httpClient.GetAsync($"{_provincesApiBaseUrl}?depth=2");
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var provinces = JsonSerializer.Deserialize<List<ProvinceWithDistrictsApiModel>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            foreach (var province in provinces ?? new List<ProvinceWithDistrictsApiModel>())
            {
                var district = province.Districts?.FirstOrDefault(d => d.Code.ToString() == districtId);
                if (district != null)
                {
                    Logger.LogDebug("Retrieved provinceId: {ProvinceId} for districtId: {DistrictId}", province.Code, districtId);
                    return province.Code.ToString();
                }
            }

            Logger.LogWarning("Province not found for districtId: {DistrictId}", districtId);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting province ID from district ID: {DistrictId}", districtId);
            return null;
        }
    }

    /// <summary>
    /// Get all provinces from provinces.open-api.vn
    /// </summary>
    public async Task<List<ProvinceApiModel>> GetAllProvincesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_provincesApiBaseUrl}?depth=1");
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var provinces = JsonSerializer.Deserialize<List<ProvinceApiModel>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Logger.LogDebug("Retrieved {Count} provinces from API", provinces?.Count ?? 0);
            return provinces ?? new List<ProvinceApiModel>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting all provinces from API");
            return new List<ProvinceApiModel>();
        }
    }

    /// <summary>
    /// Get all districts from provinces.open-api.vn
    /// </summary>
    public async Task<List<ProvinceWithDistrictsApiModel>> GetAllDistrictsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_provincesApiBaseUrl}?depth=2");
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var provinces = JsonSerializer.Deserialize<List<ProvinceWithDistrictsApiModel>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Logger.LogDebug("Retrieved {Count} provinces with districts from API", provinces?.Count ?? 0);
            return provinces ?? new List<ProvinceWithDistrictsApiModel>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting all districts from API");
            return new List<ProvinceWithDistrictsApiModel>();
        }
    }

    /// <summary>
    /// Apply location filtering to doctor list
    /// </summary>
    public async Task<List<DoctorResponse>> ApplyLocationFilteringAsync(List<DoctorResponse> doctors, string? provinceId, string? districtId)
    {
        if (string.IsNullOrEmpty(provinceId) && string.IsNullOrEmpty(districtId))
        {
            return doctors;
        }

        Logger.LogInformation("Applying location filtering for province: {ProvinceId}, district: {DistrictId}", provinceId, districtId);

        var locationInfo = await GetLocationInfoAsync(provinceId, districtId);
        if (locationInfo == null)
        {
            Logger.LogWarning("Location info not found for province: {ProvinceId}, district: {DistrictId}", provinceId, districtId);
            return doctors;
        }

        Logger.LogInformation("Location filter - Province: {ProvinceName}, District: {DistrictName}", locationInfo.ProvinceName, locationInfo.DistrictName);

        var filteredDoctors = doctors.Where(doctor =>
            IsDoctorInLocation(doctor, locationInfo)).ToList();

        Logger.LogInformation("Location filtering: {OriginalCount} -> {FilteredCount} doctors", doctors.Count, filteredDoctors.Count);

        return filteredDoctors;
    }

    /// <summary>
    /// Get location information for filtering
    /// </summary>
    public async Task<LocationInfo?> GetLocationInfoAsync(string? provinceId, string? districtId)
    {
        if (string.IsNullOrEmpty(provinceId) && string.IsNullOrEmpty(districtId))
        {
            return null;
        }

        // If only districtId is provided, get provinceId from districtId
        if (string.IsNullOrEmpty(provinceId) && !string.IsNullOrEmpty(districtId))
        {
            provinceId = await GetProvinceIdFromDistrictIdAsync(districtId);
            if (string.IsNullOrEmpty(provinceId))
            {
                Logger.LogWarning("Could not find province for district: {DistrictId}", districtId);
                return null;
            }
        }

        var provinceName = await GetProvinceNameByIdAsync(provinceId);
        if (string.IsNullOrEmpty(provinceName))
        {
            Logger.LogWarning("Could not find province name for ID: {ProvinceId}", provinceId);
            return null;
        }

        var districtName = string.Empty;
        if (!string.IsNullOrEmpty(districtId))
        {
            districtName = await GetDistrictNameByIdAsync(districtId) ?? string.Empty;
        }

        return new LocationInfo
        {
            ProvinceName = provinceName,
            DistrictName = districtName,
            HasDistrict = !string.IsNullOrEmpty(districtId) && !string.IsNullOrEmpty(districtName)
        };
    }

    /// <summary>
    /// Check if doctor is in specified location
    /// </summary>
    public bool IsDoctorInLocation(DoctorResponse doctor, LocationInfo locationInfo)
    {
        if (doctor.Hospital?.Address == null)
            return false;

        var address = doctor.Hospital.Address;

        if (locationInfo.HasDistrict)
        {
            // Filter by both province and district
            return ContainsLocation(address, locationInfo.ProvinceName) &&
                   ContainsLocation(address, locationInfo.DistrictName);
        }
        else
        {
            // Filter by province only
            return ContainsLocation(address, locationInfo.ProvinceName);
        }
    }

    /// <summary>
    /// Check if address contains location name
    /// </summary>
    public bool ContainsLocation(string address, string locationName)
    {
        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(locationName))
            return false;

        var normalizedAddress = NormalizeLocationText(address);
        var normalizedLocation = NormalizeLocationText(locationName);

        // Check if address contains any variation of the location name
        var variations = GetLocationVariations(normalizedLocation);
        return variations.Any(variation => normalizedAddress.Contains(variation));
    }

    /// <summary>
    /// Normalize location text for comparison
    /// </summary>
    public string NormalizeLocationText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Remove accents and convert to lowercase
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().ToLowerInvariant().Trim();
    }

    /// <summary>
    /// Get location name variations for matching
    /// </summary>
    public List<string> GetLocationVariations(string locationName)
    {
        var variations = new List<string> { locationName };

        // Add variations without common prefixes
        var prefixes = new[] { "tỉnh", "thành phố", "tp.", "tp", "quận", "huyện", "xã", "phường", "thị trấn" };
        foreach (var prefix in prefixes)
        {
            if (locationName.Contains(prefix))
            {
                var withoutPrefix = locationName.Replace(prefix, "").Trim();
                if (!string.IsNullOrEmpty(withoutPrefix))
                {
                    variations.Add(withoutPrefix);
                }
            }
        }

        return variations.Distinct().ToList();
    }
}
