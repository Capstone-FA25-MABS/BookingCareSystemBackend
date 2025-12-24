using BookingCare.Services.Doctor.Models.ApiModels;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using DoctorLocationInfo = BookingCare.Services.Doctor.Services.Interfaces.LocationInfo;
using SharedLocationInfo = BookingCare.Shared.Common.Models.LocationInfo;
using DoctorProvinceApiModel = BookingCare.Services.Doctor.Models.ApiModels.ProvinceApiModel;
using DoctorProvinceWithDistrictsApiModel = BookingCare.Services.Doctor.Models.ApiModels.ProvinceWithDistrictsApiModel;
using DoctorDistrictApiModel = BookingCare.Services.Doctor.Models.ApiModels.DistrictApiModel;
using DoctorILocationApiService = BookingCare.Services.Doctor.Services.Interfaces.ILocationApiService;

namespace BookingCare.Services.Doctor.Services.Implementations;

/// <summary>
/// Service for handling location data from local JSON files with Doctor-specific extensions
/// </summary>
public class LocationApiService : BaseService, DoctorILocationApiService
{
    private readonly BookingCare.Shared.Common.Interfaces.ILocationApiService _sharedLocationService;

    public LocationApiService(
        BookingCare.Shared.Common.Interfaces.ILocationApiService sharedLocationService,
        ILogger<LocationApiService> logger) : base(logger)
    {
        _sharedLocationService = sharedLocationService;
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
    /// Get all provinces from local JSON file
    /// </summary>
    public async Task<List<DoctorProvinceApiModel>> GetAllProvincesAsync()
    {
        var sharedProvinces = await _sharedLocationService.GetAllProvincesAsync();
        return sharedProvinces.Select(p => new DoctorProvinceApiModel
        {
            Code = p.Code,
            Name = p.Name
        }).ToList();
    }

    /// <summary>
    /// Get all districts from local JSON file
    /// </summary>
    public async Task<List<DoctorProvinceWithDistrictsApiModel>> GetAllDistrictsAsync()
    {
        var sharedProvinces = await _sharedLocationService.GetAllDistrictsAsync();
        return sharedProvinces.Select(p => new DoctorProvinceWithDistrictsApiModel
        {
            Code = p.Code,
            Name = p.Name,
            Districts = p.Districts?.Select(d => new DoctorDistrictApiModel
            {
                Code = d.Code,
                Name = d.Name
            }).ToList()
        }).ToList();
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
    public async Task<DoctorLocationInfo?> GetLocationInfoAsync(string? provinceId, string? districtId)
    {
        var sharedLocationInfo = await _sharedLocationService.GetLocationInfoAsync(provinceId, districtId);
        if (sharedLocationInfo == null)
            return null;

        return new DoctorLocationInfo
        {
            ProvinceName = sharedLocationInfo.ProvinceName,
            DistrictName = sharedLocationInfo.DistrictName,
            HasDistrict = sharedLocationInfo.HasDistrict
        };
    }

    /// <summary>
    /// Check if doctor is in specified location
    /// </summary>
    public bool IsDoctorInLocation(DoctorResponse doctor, DoctorLocationInfo locationInfo)
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
