using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BookingCare.Shared.Common.Services;

/// <summary>
/// Service for handling location data from local JSON files
/// </summary>
public class LocationApiService : BaseService, ILocationApiService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly string _dataFolderPath;

    public LocationApiService(
        IHostEnvironment hostEnvironment,
        ILogger<LocationApiService> logger) : base(logger)
    {
        _hostEnvironment = hostEnvironment;
        _dataFolderPath = Path.Combine(_hostEnvironment.ContentRootPath, "Data");
    }

    private async Task<T?> LoadJsonFileAsync<T>(string fileName, string operationName)
    {
        try
        {
            var filePath = Path.Combine(_dataFolderPath, fileName);

            if (!File.Exists(filePath))
            {
                Logger.LogWarning("File not found: {FilePath} for {OperationName}", filePath, operationName);
                return default;
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var result = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Logger.LogDebug("File loaded successfully: {FilePath} for {OperationName}", filePath, operationName);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading file for {OperationName}", operationName);
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

        var provinces = await LoadJsonFileAsync<List<ProvinceApiModel>>("provinces.json", "GetProvinceNameById");
        if (provinces == null)
            return null;

        var province = provinces.FirstOrDefault(p => p.Code.ToString() == provinceId);
        var result = province?.Name;

        Logger.LogDebug("Retrieved province name: {ProvinceName} for ID: {ProvinceId}", result, provinceId);
        return result;
    }

    /// <summary>
    /// Get district name from ID using local JSON file
    /// </summary>
    public async Task<string?> GetDistrictNameByIdAsync(string? districtId)
    {
        if (string.IsNullOrEmpty(districtId))
            return null;

        var provinces = await LoadJsonFileAsync<List<ProvinceWithDistrictsApiModel>>("districts.json", "GetDistrictNameById");
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
    /// Get province ID from district ID using local JSON file
    /// </summary>
    public async Task<string?> GetProvinceIdFromDistrictIdAsync(string? districtId)
    {
        if (string.IsNullOrEmpty(districtId))
            return null;

        try
        {
            var provinces = await LoadJsonFileAsync<List<ProvinceWithDistrictsApiModel>>("districts.json", "GetProvinceIdFromDistrictId");
            if (provinces == null)
                return null;

            foreach (var province in provinces)
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
    /// Get all provinces from local JSON file
    /// </summary>
    public async Task<List<ProvinceApiModel>> GetAllProvincesAsync()
    {
        try
        {
            var provinces = await LoadJsonFileAsync<List<ProvinceApiModel>>("provinces.json", "GetAllProvinces");
            Logger.LogDebug("Retrieved {Count} provinces from local file", provinces?.Count ?? 0);
            return provinces ?? new List<ProvinceApiModel>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting all provinces from local file");
            return new List<ProvinceApiModel>();
        }
    }

    /// <summary>
    /// Get all districts from local JSON file
    /// </summary>
    public async Task<List<ProvinceWithDistrictsApiModel>> GetAllDistrictsAsync()
    {
        try
        {
            var provinces = await LoadJsonFileAsync<List<ProvinceWithDistrictsApiModel>>("districts.json", "GetAllDistricts");
            Logger.LogDebug("Retrieved {Count} provinces with districts from local file", provinces?.Count ?? 0);
            return provinces ?? new List<ProvinceWithDistrictsApiModel>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting all districts from local file");
            return new List<ProvinceWithDistrictsApiModel>();
        }
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
}

