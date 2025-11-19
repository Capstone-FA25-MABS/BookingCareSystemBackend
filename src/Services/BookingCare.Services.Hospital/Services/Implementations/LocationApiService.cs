using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Shared.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalILocationApiService = BookingCare.Services.Hospital.Services.Interfaces.ILocationApiService;
using HospitalLocationInfo = BookingCare.Services.Hospital.Services.Interfaces.LocationInfo;

namespace BookingCare.Services.Hospital.Services.Implementations;

/// <summary>
/// Service for handling location data from local JSON files with Hospital-specific extensions
/// </summary>
public class LocationApiService : HospitalILocationApiService
{
    private readonly BookingCare.Shared.Common.Interfaces.ILocationApiService _sharedLocationService;
    private readonly ILogger<LocationApiService> _logger;

    public LocationApiService(
        BookingCare.Shared.Common.Interfaces.ILocationApiService sharedLocationService,
        ILogger<LocationApiService> logger)
    {
        _sharedLocationService = sharedLocationService;
        _logger = logger;
    }

    /// <summary>
    /// Get province name from ID using local JSON file
    /// </summary>
    public async Task<string?> GetProvinceNameByIdAsync(string? provinceId)
    {
        return await _sharedLocationService.GetProvinceNameByIdAsync(provinceId);
    }

    /// <summary>
    /// Get district name from ID using local JSON file
    /// </summary>
    public async Task<string?> GetDistrictNameByIdAsync(string? districtId)
    {
        return await _sharedLocationService.GetDistrictNameByIdAsync(districtId);
    }

    /// <summary>
    /// Get province ID from district ID using local JSON file
    /// </summary>
    public async Task<string?> GetProvinceIdFromDistrictIdAsync(string? districtId)
    {
        return await _sharedLocationService.GetProvinceIdFromDistrictIdAsync(districtId);
    }

    /// <summary>
    /// Get location info (province and district names) from IDs
    /// </summary>
    public async Task<HospitalLocationInfo?> GetLocationInfoAsync(string? provinceId, string? districtId)
    {
        if (string.IsNullOrEmpty(provinceId) && string.IsNullOrEmpty(districtId))
            return null;

        var sharedLocationInfo = await _sharedLocationService.GetLocationInfoAsync(provinceId, districtId);
        if (sharedLocationInfo == null)
            return null;

        // If only district ID is provided, get province ID from district
        var resolvedProvinceId = provinceId;
        if (string.IsNullOrEmpty(provinceId) && !string.IsNullOrEmpty(districtId))
        {
            resolvedProvinceId = await GetProvinceIdFromDistrictIdAsync(districtId);
        }

        return new HospitalLocationInfo
        {
            ProvinceId = resolvedProvinceId,
            DistrictId = districtId,
            ProvinceName = sharedLocationInfo.ProvinceName,
            DistrictName = sharedLocationInfo.DistrictName
        };
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
    public bool IsHospitalInLocation(HospitalListOptimizedResponse hospital, HospitalLocationInfo locationInfo)
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
