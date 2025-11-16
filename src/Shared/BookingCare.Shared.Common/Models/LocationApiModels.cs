using System.Text.Json.Serialization;

namespace BookingCare.Shared.Common.Models;

/// <summary>
/// Province API model for local JSON files
/// </summary>
public class ProvinceApiModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// District API model for local JSON files
/// </summary>
public class DistrictApiModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Ward API model for local JSON files
/// </summary>
public class WardApiModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Province with districts API model for local JSON files
/// </summary>
public class ProvinceWithDistrictsApiModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("districts")]
    public List<DistrictApiModel>? Districts { get; set; }
}

/// <summary>
/// District with wards API model for local JSON files
/// </summary>
public class DistrictWithWardsApiModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("wards")]
    public List<WardApiModel>? Wards { get; set; }
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

