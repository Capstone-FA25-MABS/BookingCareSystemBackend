using BookingCare.Services.Doctor.Models.ApiModels;
using BookingCare.Services.Doctor.Models.DTOs.Responses;

namespace BookingCare.Services.Doctor.Services.Interfaces;

/// <summary>
/// Service for handling location-related API calls
/// </summary>
public interface ILocationApiService
{
    /// <summary>
    /// Get province name from ID using provinces.open-api.vn
    /// </summary>
    /// <param name="provinceId">Province ID</param>
    /// <returns>Province name or null if not found</returns>
    Task<string?> GetProvinceNameByIdAsync(string? provinceId);

    /// <summary>
    /// Get district name from ID using provinces.open-api.vn
    /// </summary>
    /// <param name="districtId">District ID</param>
    /// <returns>District name or null if not found</returns>
    Task<string?> GetDistrictNameByIdAsync(string? districtId);

    /// <summary>
    /// Get province ID from district ID using provinces.open-api.vn
    /// </summary>
    /// <param name="districtId">District ID</param>
    /// <returns>Province ID or null if not found</returns>
    Task<string?> GetProvinceIdFromDistrictIdAsync(string? districtId);

    /// <summary>
    /// Get all provinces from provinces.open-api.vn
    /// </summary>
    /// <returns>List of provinces</returns>
    Task<List<ProvinceApiModel>> GetAllProvincesAsync();

    /// <summary>
    /// Get all districts from provinces.open-api.vn
    /// </summary>
    /// <returns>List of districts</returns>
    Task<List<ProvinceWithDistrictsApiModel>> GetAllDistrictsAsync();

    /// <summary>
    /// Apply location filtering to doctor list
    /// </summary>
    /// <param name="doctors">List of doctors to filter</param>
    /// <param name="provinceId">Province ID to filter by</param>
    /// <param name="districtId">District ID to filter by</param>
    /// <returns>Filtered list of doctors</returns>
    Task<List<DoctorResponse>> ApplyLocationFilteringAsync(List<DoctorResponse> doctors, string? provinceId, string? districtId);

    /// <summary>
    /// Get location information for filtering
    /// </summary>
    /// <param name="provinceId">Province ID</param>
    /// <param name="districtId">District ID</param>
    /// <returns>Location information</returns>
    Task<LocationInfo?> GetLocationInfoAsync(string? provinceId, string? districtId);

    /// <summary>
    /// Check if doctor is in specified location
    /// </summary>
    /// <param name="doctor">Doctor to check</param>
    /// <param name="locationInfo">Location information</param>
    /// <returns>True if doctor is in location</returns>
    bool IsDoctorInLocation(DoctorResponse doctor, LocationInfo locationInfo);

    /// <summary>
    /// Check if address contains location name
    /// </summary>
    /// <param name="address">Address to check</param>
    /// <param name="locationName">Location name to search for</param>
    /// <returns>True if address contains location</returns>
    bool ContainsLocation(string address, string locationName);

    /// <summary>
    /// Normalize location text for comparison
    /// </summary>
    /// <param name="text">Text to normalize</param>
    /// <returns>Normalized text</returns>
    string NormalizeLocationText(string text);

    /// <summary>
    /// Get location name variations for matching
    /// </summary>
    /// <param name="locationName">Location name</param>
    /// <returns>List of variations</returns>
    List<string> GetLocationVariations(string locationName);
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
