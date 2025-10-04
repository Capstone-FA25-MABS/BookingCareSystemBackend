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

    private async Task<T?> CallApiAsync<T>(string endpoint, string operationName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_provincesApiBaseUrl}{endpoint}");
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Logger.LogDebug("API call successful for {OperationName}", operationName);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling API for {OperationName}", operationName);
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

        var provinces = await CallApiAsync<List<ProvinceApiModel>>("?depth=1", "GetProvinceNameById");
        if (provinces == null)
            return null;

        var province = provinces.FirstOrDefault(p => p.Code.ToString() == provinceId);
        var result = province?.Name;

        Logger.LogDebug("Retrieved province name: {ProvinceName} for ID: {ProvinceId}", result, provinceId);
        return result;
    }

    /// <summary>
    /// Get district name from ID using provinces.open-api.vn
    /// </summary>
    public async Task<string?> GetDistrictNameByIdAsync(string? districtId)
    {
        if (string.IsNullOrEmpty(districtId))
            return null;

        var provinces = await CallApiAsync<List<ProvinceWithDistrictsApiModel>>("?depth=2", "GetDistrictNameById");
        if (provinces == null)
            return null;

        foreach (var province in provinces)
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
        // Simple location filtering based on hospital address
        if (doctor.Hospital?.Address == null)
        {
            Logger.LogInformation("Doctor {DoctorId} has no hospital address", doctor.Id);
            return false;
        }

        var hospitalAddress = doctor.Hospital.Address.ToLower();
        var provinceName = locationInfo.ProvinceName.ToLower();
        var districtName = locationInfo.DistrictName?.ToLower() ?? "";

        // Clean up province name - remove "thành phố" prefix
        var cleanProvinceName = provinceName.Replace("thành phố", "").Replace("tỉnh", "").Trim();

        Logger.LogInformation("Checking doctor {DoctorId} with hospital address: '{HospitalAddress}' against location: Province='{ProvinceName}', District='{DistrictName}'",
            doctor.Id, hospitalAddress, locationInfo.ProvinceName, locationInfo.DistrictName);

        Logger.LogInformation("DEBUG - Normalized values: hospitalAddress='{HospitalAddress}', provinceName='{ProvinceName}', cleanProvinceName='{CleanProvinceName}', districtName='{DistrictName}'",
            hospitalAddress, provinceName, cleanProvinceName, districtName);

        bool result;
        if (locationInfo.HasDistrict && !string.IsNullOrEmpty(districtName))
        {
            // Filter by both province and district - both must be found in address
            var provinceMatch = hospitalAddress.Contains(provinceName) || hospitalAddress.Contains(cleanProvinceName);
            var districtMatch = hospitalAddress.Contains(districtName);
            result = provinceMatch && districtMatch;
            Logger.LogInformation("District filter result: ProvinceMatch={ProvinceMatch}, DistrictMatch={DistrictMatch}, Final={Result}",
                provinceMatch, districtMatch, result);
            Logger.LogInformation("DEBUG - Contains check: '{HospitalAddress}'.Contains('{ProvinceName}') OR Contains('{CleanProvinceName}') = {ProvinceMatch}, Contains('{DistrictName}') = {DistrictMatch}",
                hospitalAddress, provinceName, cleanProvinceName, provinceMatch, districtName, districtMatch);
        }
        else
        {
            // Filter by province only
            result = hospitalAddress.Contains(provinceName) || hospitalAddress.Contains(cleanProvinceName);
            Logger.LogInformation("Province filter result: {Result}", result);
            Logger.LogInformation("DEBUG - Contains check: '{HospitalAddress}'.Contains('{ProvinceName}') OR Contains('{CleanProvinceName}') = {Result}",
                hospitalAddress, provinceName, cleanProvinceName, result);
        }

        return result;
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
        var result = variations.Any(variation => normalizedAddress.Contains(variation));

        Logger.LogInformation("ContainsLocation check: Address='{Address}' -> Normalized='{NormalizedAddress}', Location='{LocationName}' -> Normalized='{NormalizedLocation}', Variations=[{Variations}], Result={Result}",
            address, normalizedAddress, locationName, normalizedLocation, string.Join(", ", variations), result);

        return result;
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

        var result = variations.Distinct().ToList();
        Logger.LogInformation("Location variations for '{LocationName}': [{Variations}]", locationName, string.Join(", ", result));
        return result;
    }
}
