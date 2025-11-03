using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BookingCare.Services.Hospital.Services.Implementations;

/// <summary>
/// Service for handling location data from local JSON files
/// </summary>
public class LocationApiService : ILocationApiService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<LocationApiService> _logger;
    private readonly string _dataFolderPath;

    public LocationApiService(
        IHostEnvironment hostEnvironment,
        ILogger<LocationApiService> logger)
    {
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _dataFolderPath = Path.Combine(_hostEnvironment.ContentRootPath, "Data");
    }

    private async Task<T?> LoadJsonFileAsync<T>(string fileName, string operationName)
    {
        try
        {
            var filePath = Path.Combine(_dataFolderPath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath} for {OperationName}", filePath, operationName);
                return default;
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            _logger.LogInformation("File loaded for {OperationName}, content length: {ContentLength}", operationName, jsonContent.Length);

            var result = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("File loaded successfully for {OperationName}", operationName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading file for {OperationName}", operationName);
            return default;
        }
    }

    /// <summary>
    /// Get province name from ID using local JSON file
    /// </summary>
    public async Task<string?> GetProvinceNameByIdAsync(string? provinceId)
    {
        if (string.IsNullOrEmpty(provinceId))
            return null;

        _logger.LogInformation("Getting province name for ID: {ProvinceId}", provinceId);

        // Use flexible model to handle both int and string code types
        var provinces = await LoadJsonFileAsync<List<ProvinceApiModelFlexible>>("provinces.json", "GetProvinceNameById");
        if (provinces == null)
        {
            _logger.LogWarning("Failed to get provinces from local file for ID: {ProvinceId}", provinceId);
            return null;
        }

        _logger.LogInformation("Retrieved {ProvinceCount} provinces from local file", provinces.Count);

        // Try to match province by code - handle both int and string formats
        var province = provinces.FirstOrDefault(p =>
        {
            var codeStr = p.GetCodeAsString();
            // Try direct match first
            if (codeStr == provinceId)
                return true;

            // Try parsing provinceId as int and comparing
            if (int.TryParse(provinceId, out var provinceIdInt) && int.TryParse(codeStr, out var codeInt) && codeInt == provinceIdInt)
                return true;

            return false;
        });
        var result = province?.Name;

        _logger.LogInformation("Retrieved province name: {ProvinceName} for ID: {ProvinceId}", result, provinceId);
        return result;
    }

    /// <summary>
    /// Get district name from ID using local JSON file
    /// </summary>
    public async Task<string?> GetDistrictNameByIdAsync(string? districtId)
    {
        if (string.IsNullOrEmpty(districtId))
            return null;

        try
        {
            var provinces = await LoadJsonFileAsync<List<ProvinceWithDistrictsApiModelFlexible>>("districts.json", "GetDistrictNameById");
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
    /// Get province ID from district ID using local JSON file
    /// </summary>
    public async Task<string?> GetProvinceIdFromDistrictIdAsync(string? districtId)
    {
        if (string.IsNullOrEmpty(districtId))
            return null;

        try
        {
            var provinces = await LoadJsonFileAsync<List<ProvinceWithDistrictsApiModelFlexible>>("districts.json", "GetProvinceIdFromDistrictId");
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
            _logger.LogWarning("Location info not found for ProvinceId: {ProvinceId}, DistrictId: {DistrictId}. Returning all hospitals.", provinceId, districtId);
            return hospitals;
        }

        // Validate that we have at least province name when filtering by province
        if (!string.IsNullOrEmpty(provinceId) && string.IsNullOrEmpty(locationInfo.ProvinceName))
        {
            _logger.LogWarning("ProvinceId provided ({ProvinceId}) but ProvinceName is empty. Cannot filter by location.", provinceId);
            return hospitals;
        }

        // Validate that we have district name when filtering by district
        if (!string.IsNullOrEmpty(districtId) && string.IsNullOrEmpty(locationInfo.DistrictName))
        {
            _logger.LogWarning("DistrictId provided ({DistrictId}) but DistrictName is empty. Cannot filter by location.", districtId);
            return hospitals;
        }

        _logger.LogInformation("Location info retrieved - Province: {ProvinceName} (ID: {ProvinceId}), District: {DistrictName} (ID: {DistrictId})",
            locationInfo.ProvinceName, locationInfo.ProvinceId, locationInfo.DistrictName, locationInfo.DistrictId);

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
            // Ensure we have a valid province name to match against
            if (string.IsNullOrEmpty(provinceName))
            {
                _logger.LogWarning("Province name is empty, cannot filter by province. Hospital: {HospitalId}", hospital.Id);
                return false;
            }

            // Check multiple variations: full name and clean name
            result = (!string.IsNullOrEmpty(provinceName) && hospitalAddress.Contains(provinceName)) ||
                    (!string.IsNullOrEmpty(cleanProvinceName) && hospitalAddress.Contains(cleanProvinceName));

            // Additional check: try with normalized Vietnamese characters (đ -> d)
            if (!result && !string.IsNullOrEmpty(cleanProvinceName))
            {
                var normalizedAddress = hospitalAddress
                    .Replace("đ", "d")
                    .Replace("Đ", "d");
                var normalizedProvince = cleanProvinceName
                    .Replace("đ", "d")
                    .Replace("Đ", "d");
                result = normalizedAddress.Contains(normalizedProvince);
            }

            _logger.LogInformation("Province filter result: {Result} - Address='{Address}', ProvinceName='{ProvinceName}', CleanProvinceName='{CleanProvinceName}'",
                result, hospitalAddress, locationInfo.ProvinceName, cleanProvinceName);
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
