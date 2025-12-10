using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text;
using DoctorServiceOptionDto = BookingCare.Services.AI.Models.DTOs.Responses.DoctorServiceOption;

namespace BookingCare.Services.AI.Helpers;

/// <summary>
/// Helper service để centralize và parallelize doctor/hospital recommendation logic
/// Shared helper to centralize and parallelize doctor/hospital recommendation logic
/// Enhanced with caching and shared scoring methods
/// </summary>
public class RecommendationHelper
{
    private readonly ILogger<RecommendationHelper> _logger;
    private readonly DoctorService.DoctorServiceClient _doctorClient;
    private readonly HospitalService.HospitalServiceClient _hospitalClient;
    private readonly IMemoryCache _cache;

    private const int MAX_DOCTOR_RECOMMENDATIONS = 10;
    private const int MAX_HOSPITAL_RECOMMENDATIONS = 5;
    private const string SPECIALTY_CACHE_KEY = "AllSpecialties";
    private static readonly TimeSpan SpecialtyCacheDuration = TimeSpan.FromHours(1);

    public RecommendationHelper(
        ILogger<RecommendationHelper> logger,
        DoctorService.DoctorServiceClient doctorClient,
        HospitalService.HospitalServiceClient hospitalClient,
        IMemoryCache cache)
    {
        _logger = logger;
        _doctorClient = doctorClient;
        _hospitalClient = hospitalClient;
        _cache = cache;
    }

    /// <summary>
    /// Lấy cả doctor và hospital recommendations song song (parallel) để tối ưu performance
    /// Get both doctor and hospital recommendations in parallel for optimal performance
    /// </summary>
    public async Task<(List<DoctorRecommendation> Doctors, List<HospitalRecommendation> Hospitals)>
        GetRecommendationsAsync(
            List<string> specialties,
            LocationContext? location)
    {
        // Run both operations in parallel for better performance
        var doctorTask = GetDoctorRecommendationsAsync(specialties, location);
        var hospitalTask = GetHospitalRecommendationsAsync(specialties, location);

        await Task.WhenAll(doctorTask, hospitalTask);

        return (await doctorTask, await hospitalTask);
    }

    /// <summary>
    /// Lấy doctor recommendations theo specialties và location
    /// Get doctor recommendations by specialties and location
    /// </summary>
    public async Task<List<DoctorRecommendation>> GetDoctorRecommendationsAsync(
        List<string> specialties,
        LocationContext? location)
    {
        if (specialties.Count == 0) return new List<DoctorRecommendation>();

        try
        {
            var specialtyIds = await MatchSpecialtiesToIdsAsync(specialties);
            if (specialtyIds.Count == 0) return new List<DoctorRecommendation>();

            var request = new FilterDoctorsForRecommendationRequest
            {
                MaxResults = MAX_DOCTOR_RECOMMENDATIONS * 2
            };
            request.SpecialtyIds.AddRange(specialtyIds.Select(id => id.ToString()));

            if (location != null && !string.IsNullOrEmpty(location.ProvinceId))
            {
                request.ProvinceId = location.ProvinceId;
            }

            var response = await _doctorClient.FilterDoctorsForRecommendationAsync(request);

            return response.Doctors
                .Select(x => new { Doctor = x, Score = CalculateDoctorScore(x, location) })
                .OrderByDescending(x => x.Score)
                .Take(MAX_DOCTOR_RECOMMENDATIONS)
                .Select(x => MapToDoctorRecommendation(x.Doctor, x.Score))
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting doctor recommendations");
            return new List<DoctorRecommendation>();
        }
    }

    /// <summary>
    /// Lấy hospital recommendations theo specialties và location
    /// Get hospital recommendations by specialties and location
    /// </summary>
    public async Task<List<HospitalRecommendation>> GetHospitalRecommendationsAsync(
        List<string> specialties,
        LocationContext? location)
    {
        if (specialties.Count == 0) return new List<HospitalRecommendation>();

        try
        {
            var mappedSpecialties = NormalizeAndMapSpecialties(specialties);
            var specialtyIds = await MatchSpecialtiesToIdsAsync(mappedSpecialties);
            if (specialtyIds.Count == 0) return new List<HospitalRecommendation>();
            var specialtyNames = await GetSpecialtyNamesByIdsAsync(specialtyIds);
            var filteredSpecialtyNames = FilterSpecialtyNames(specialtyNames);

            // Run hospital queries in parallel for better performance
            var hospitalTasks = specialtyIds.Take(3).Select<Guid, Task<List<HospitalReply>>>(async specialtyId =>
            {
                try
                {
                    var request = new GetHospitalsBySpecialtyRequest
                    {
                        SpecialtyId = specialtyId.ToString()
                    };

                    var response = await _hospitalClient.GetHospitalsBySpecialtyAsync(request);
                    return response.Hospitals.ToList();
                }
                catch (RpcException ex)
                {
                    _logger.LogWarning(ex, "Failed to get hospitals for specialty {SpecialtyId}", specialtyId);
                    return new List<HospitalReply>();
                }
            });

            var hospitalResults = await Task.WhenAll(hospitalTasks);
            var allHospitals = hospitalResults.SelectMany(h => h).ToList();

            return allHospitals
                    .GroupBy(h => h.Id)
                    .Select(g => g.First())
                    .Select(h => new HospitalRecommendation
                    {
                        Id = h.Id,
                        Name = h.Name,
                        Address = h.Address,
                        SpecialtyNames = filteredSpecialtyNames,
                        ImageUrl = h.AvatarUrl,
                        RecommendationScore = CalculateHospitalScore(h, location)
                    })
                    .OrderByDescending(h => h.RecommendationScore)
                    .Take(MAX_HOSPITAL_RECOMMENDATIONS)
                    .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hospital recommendations");
            return new List<HospitalRecommendation>();
        }
    }

    /// <summary>
    /// Get specialty list as comma-separated string for prompts (with caching)
    /// Used to provide Gemini AI with available specialty names
    /// </summary>
    public async Task<string> GetSpecialtyListTextAsync()
    {
        var specialtyNames = await GetAllSpecialtyNamesAsync();
        return string.Join(", ", specialtyNames);
    }

    /// <summary>
    /// Get all specialty names as list (with caching)
    /// </summary>
    public async Task<List<string>> GetAllSpecialtyNamesAsync()
    {
        try
        {
            // Try to get from cache first
            if (!_cache.TryGetValue(SPECIALTY_CACHE_KEY, out GetAllSpecialtiesResponse? cachedResponse))
            {
                // Not in cache, fetch from gRPC
                var request = new GetAllSpecialtiesRequest();
                cachedResponse = await _doctorClient.GetAllSpecialtiesAsync(request);

                // Cache for 1 hour
                _cache.Set(SPECIALTY_CACHE_KEY, cachedResponse, SpecialtyCacheDuration);
                _logger.LogInformation("Cached specialty list for {Duration}", SpecialtyCacheDuration);
            }

            var specialtyNames = cachedResponse.Specialties.Select(s => s.Name).ToList();

            if (specialtyNames.Count > 0)
            {
                return specialtyNames;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch specialties from database, using fallback list");
        }

        // Fallback to common specialties if database call fails
        return new List<string>
        {
            "Nội khoa", "Ngoại khoa", "Sản phụ khoa", "Nhi khoa", "Tim mạch",
            "Hô hấp", "Tiêu hóa", "Thần kinh", "Cơ xương khớp", "Da liễu",
            "Tai mũi họng", "Mắt", "Răng hàm mặt", "Tâm thần", "Nội tiết",
            "Thận - Tiết niệu", "Ung bướu", "Chấn thương chỉnh hình", "Y học cổ truyền",
            "Huyết học", "Dị ứng - Miễn dịch", "Phục hồi chức năng", "Gây mê hồi sức", "Dinh dưỡng"
        };
    }

    /// <summary>
    /// Match specialty names to IDs with caching to reduce gRPC calls
    /// Cache specialty list for 1 hour to optimize performance
    /// </summary>
    public async Task<List<Guid>> MatchSpecialtiesToIdsAsync(List<string> specialtyNames)
    {
        var specialtyIds = new List<Guid>();

        try
        {
            // Try to get from cache first
            if (!_cache.TryGetValue(SPECIALTY_CACHE_KEY, out GetAllSpecialtiesResponse? cachedResponse))
            {
                // Not in cache, fetch from gRPC
                var request = new GetAllSpecialtiesRequest();
                cachedResponse = await _doctorClient.GetAllSpecialtiesAsync(request);

                // Cache for 1 hour
                _cache.Set(SPECIALTY_CACHE_KEY, cachedResponse, SpecialtyCacheDuration);
                _logger.LogInformation("Cached specialty list for {Duration}", SpecialtyCacheDuration);
            }

            foreach (var specialtyName in specialtyNames)
            {
                var match = cachedResponse.Specialties.FirstOrDefault(s =>
                    s.Name.Equals(specialtyName, StringComparison.OrdinalIgnoreCase) ||
                    s.Name.Contains(specialtyName, StringComparison.OrdinalIgnoreCase) ||
                    specialtyName.Contains(s.Name, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    specialtyIds.Add(Guid.Parse(match.Id));
                }
            }

            _logger.LogInformation("Matched {Count} specialties to IDs", specialtyIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching specialties to IDs");
        }

        return specialtyIds;
    }

    /// <summary>
    /// Calculate doctor recommendation score
    /// Priority: Location (40%) > Rating (30%) > Experience (10%)
    /// </summary>
    public double CalculateDoctorScore(DoctorRecommendationInfo doctor, LocationContext? location)
    {
        double score = 0;

        // Location match (40% weight)
        if (location != null && !string.IsNullOrEmpty(location.ProvinceId))
        {
            score += 0.4;
        }

        // Rating (30% weight)
        score += (doctor.Rating / 5.0) * 0.3;

        // Experience (10% weight)
        score += Math.Min(doctor.YearsOfExperience / 20.0, 1.0) * 0.1;

        return score;
    }

    /// <summary>
    /// Calculate hospital recommendation score
    /// </summary>
    public double CalculateHospitalScore(HospitalReply hospital, LocationContext? location)
    {
        double score = 0.5; // Base score

        // Location matching based on address string
        if (location != null && !string.IsNullOrEmpty(location.DisplayName))
        {
            var address = hospital.Address?.ToLowerInvariant() ?? "";
            var locationName = location.DisplayName.ToLowerInvariant();

            if (address.Contains(locationName))
            {
                score += 0.5;
            }
        }

        return score;
    }

    private DoctorRecommendation? MapToDoctorRecommendation(DoctorRecommendationInfo doctor, double score)
    {
        var serviceOptions = ProcessServiceOptions(doctor.ServiceOptions.AsEnumerable());

        if (serviceOptions.Count == 0)
        {
            return null;
        }

        var preferredOption = FindPreferredServiceOption(serviceOptions);

        return new DoctorRecommendation
        {
            Id = doctor.Id,
            Name = doctor.FullName,
            SpecialtyName = doctor.SpecialtyName,
            HospitalName = doctor.HospitalName,
            Rating = doctor.Rating,
            YearOfExperience = doctor.YearsOfExperience,
            ServiceTypeName = preferredOption?.ServiceTypeName ?? doctor.ServiceTypeName,
            Price = preferredOption?.Price ?? FormatPrice(doctor.ConsultationFee),
            AvatarUrl = doctor.AvatarUrl,
            RecommendationScore = score,
            ServiceOptions = serviceOptions
        };
    }

    private List<DoctorServiceOptionDto> ProcessServiceOptions(IEnumerable<BookingCare.Services.Doctor.Protos.DoctorServiceOption> serviceOptions)
    {
        var mappedOptions = serviceOptions
            .Select(o => new DoctorServiceOptionDto
            {
                ServiceTypeId = string.IsNullOrWhiteSpace(o.ServiceTypeId) ? null : o.ServiceTypeId,
                ServiceTypeName = o.ServiceTypeName,
                Price = FormatPrice(o.ConsultationFee)
            })
            .ToList();

        return FilterAllowedServiceTypes(mappedOptions);
    }

    private List<DoctorServiceOptionDto> FilterAllowedServiceTypes(List<DoctorServiceOptionDto> serviceOptions)
    {
        return serviceOptions
            .Where(o => IsAllowedServiceType(o.ServiceTypeName))
            .ToList();
    }

    private static bool IsAllowedServiceType(string? serviceTypeName)
    {
        if (string.IsNullOrWhiteSpace(serviceTypeName))
        {
            return false;
        }

        var name = serviceTypeName.Trim().ToLowerInvariant();
        return name == "in_person"
            || name.Contains("trực tiếp")
            || name == "telehealth"
            || name.Contains("tư vấn trực tuyến");
    }

    private static DoctorServiceOptionDto? FindPreferredServiceOption(List<DoctorServiceOptionDto> serviceOptions)
    {
        var preferredOption = serviceOptions.FirstOrDefault(o =>
            o.ServiceTypeName.Equals("IN_PERSON", StringComparison.OrdinalIgnoreCase) ||
            o.ServiceTypeName.Contains("trực tiếp", StringComparison.OrdinalIgnoreCase));

        return preferredOption ?? serviceOptions.FirstOrDefault();
    }

    private static string? FormatPrice(double consultationFee)
    {
        return consultationFee > 0 ? $"{consultationFee:N0} VNĐ" : null;
    }

    /// <summary>
    /// Lấy tên chuyên khoa từ DB (cache) theo danh sách ID. Fallback về tên AI trả ra nếu không match.
    /// </summary>
    /// <summary>
    /// Chuẩn hóa và map specialty từ AI sang tên chuẩn (tiếng Việt) nếu có từ điển.
    /// </summary>
    private List<string> NormalizeAndMapSpecialties(List<string> specialties)
    {
        if (specialties.Count == 0) return specialties;

        return specialties
            .Select(MapToKnownSpecialtyName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string MapToKnownSpecialtyName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var norm = NormalizeText(name);

        // Map các tên tiếng Anh phổ biến về tiếng Việt để match DB
        if (norm.Contains("dermatology")) return "Da liễu";
        if (norm.Contains("cardiology")) return "Tim mạch";
        if (norm.Contains("neurology")) return "Thần kinh";

        return name.Trim();
    }

    private async Task<List<string>> GetSpecialtyNamesByIdsAsync(List<Guid> specialtyIds)
    {
        if (specialtyIds.Count == 0)
        {
            return new List<string>();
        }

        try
        {
            if (!_cache.TryGetValue(SPECIALTY_CACHE_KEY, out GetAllSpecialtiesResponse? cachedResponse))
            {
                var request = new GetAllSpecialtiesRequest();
                cachedResponse = await _doctorClient.GetAllSpecialtiesAsync(request);
                _cache.Set(SPECIALTY_CACHE_KEY, cachedResponse, SpecialtyCacheDuration);
                _logger.LogInformation("Cached specialty list for {Duration}", SpecialtyCacheDuration);
            }

            var lookup = cachedResponse.Specialties.ToDictionary(s => s.Id, s => s.Name, StringComparer.OrdinalIgnoreCase);
            var names = specialtyIds
                .Select(id => lookup.TryGetValue(id.ToString(), out var name) ? name : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return names;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to map specialty IDs to names");
            return new List<string>();
        }
    }

    /// <summary>
    /// Loại bỏ bản dịch trùng (ví dụ Dermatology) nếu đã có bản tiếng Việt (Da liễu)
    /// </summary>
    private List<string> FilterSpecialtyNames(List<string> specialties)
    {
        if (specialties.Count == 0) return specialties;

        var normalized = specialties.Select(s => new
        {
            Original = s,
            Normalized = NormalizeText(s)
        }).ToList();

        bool hasDaLieu = normalized.Any(n => n.Normalized.Contains("da lieu"));

        return normalized
            .Where(n => !(hasDaLieu && n.Normalized.Contains("dermatology")))
            .Select(n => n.Original)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var formD = value.Normalize(NormalizationForm.FormD);
        Span<char> buffer = stackalloc char[formD.Length];
        int idx = 0;

        foreach (var ch in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                buffer[idx++] = ch;
            }
        }

        return new string(buffer[..idx]).ToLowerInvariant().Trim();
    }
}

