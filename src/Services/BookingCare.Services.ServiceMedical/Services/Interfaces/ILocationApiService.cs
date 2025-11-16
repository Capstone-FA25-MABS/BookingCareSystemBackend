using BookingCare.Services.ServiceMedical.Models.ApiModels;
using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.ServiceMedical.Services.Interfaces;

/// <summary>
/// Service for handling location-related data from local JSON files
/// </summary>
public interface ILocationApiService
{
    /// <summary>
    /// Get province name from ID using local JSON file
    /// </summary>
    /// <param name="provinceId">Province ID</param>
    /// <returns>Province name or null if not found</returns>
    Task<string?> GetProvinceNameByIdAsync(string? provinceId);

    /// <summary>
    /// Get district name from ID using local JSON file
    /// </summary>
    /// <param name="districtId">District ID</param>
    /// <returns>District name or null if not found</returns>
    Task<string?> GetDistrictNameByIdAsync(string? districtId);

    /// <summary>
    /// Get province ID from district ID using local JSON file
    /// </summary>
    /// <param name="districtId">District ID</param>
    /// <returns>Province ID or null if not found</returns>
    Task<string?> GetProvinceIdFromDistrictIdAsync(string? districtId);

    /// <summary>
    /// Get all provinces from local JSON file
    /// </summary>
    /// <returns>List of provinces</returns>
    Task<List<ProvinceApiModel>> GetAllProvincesAsync();

    /// <summary>
    /// Get all districts from local JSON file
    /// </summary>
    /// <returns>List of districts</returns>
    Task<List<ProvinceWithDistrictsApiModel>> GetAllDistrictsAsync();

    /// <summary>
    /// Get location information for filtering
    /// </summary>
    /// <param name="provinceId">Province ID</param>
    /// <param name="districtId">District ID</param>
    /// <returns>Location information</returns>
    Task<LocationInfo?> GetLocationInfoAsync(string? provinceId, string? districtId);
}

/// <summary>
/// Location information for filtering
/// </summary>
public class LocationInfo
{
    public string ProvinceName { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
    public bool HasDistrict { get; set; }
}

