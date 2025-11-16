using System.Text.Json.Serialization;

namespace BookingCare.Services.ServiceMedical.Models.ApiModels;

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

