using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

/// <summary>
/// Service for handling location-related API calls and filtering
/// </summary>
public interface ILocationApiService
{
    /// <summary>
    /// Get province name from ID using provinces.open-api.vn
    /// </summary>
    Task<string?> GetProvinceNameByIdAsync(string? provinceId);

    /// <summary>
    /// Get district name from ID using provinces.open-api.vn
    /// </summary>
    Task<string?> GetDistrictNameByIdAsync(string? districtId);

    /// <summary>
    /// Get province ID from district ID using provinces.open-api.vn
    /// </summary>
    Task<string?> GetProvinceIdFromDistrictIdAsync(string? districtId);

    /// <summary>
    /// Get location info (province and district names) from IDs
    /// </summary>
    Task<LocationInfo?> GetLocationInfoAsync(string? provinceId, string? districtId);

    /// <summary>
    /// Apply location filtering to hospital list based on address matching
    /// </summary>
    Task<List<HospitalListOptimizedResponse>> ApplyLocationFilteringAsync(
        List<HospitalListOptimizedResponse> hospitals,
        string? provinceId,
        string? districtId);

    /// <summary>
    /// Test method to validate location filtering logic
    /// </summary>
    Task<bool> TestLocationFilteringAsync(string testAddress, int? provinceId, int? districtId);
}

/// <summary>
/// Location information model
/// </summary>
public class LocationInfo
{
    public string? ProvinceId { get; set; }
    public string? ProvinceName { get; set; }
    public string? DistrictId { get; set; }
    public string? DistrictName { get; set; }
}

