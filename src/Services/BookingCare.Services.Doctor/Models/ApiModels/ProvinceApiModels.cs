using System.Text.Json.Serialization;

namespace BookingCare.Services.Doctor.Models.ApiModels;

/// <summary>
/// Province API model for provinces.open-api.vn
/// </summary>
public class ProvinceApiModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// District API model for provinces.open-api.vn
/// </summary>
public class DistrictApiModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Ward API model for provinces.open-api.vn
/// </summary>
public class WardApiModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Province with districts API model for provinces.open-api.vn
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
/// District with wards API model for provinces.open-api.vn
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
