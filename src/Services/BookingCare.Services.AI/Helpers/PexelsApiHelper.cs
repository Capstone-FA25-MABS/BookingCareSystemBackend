using System.Text.Json;

namespace BookingCare.Services.AI.Helpers;

/// <summary>
/// Helper class for Pexels API to fetch food and exercise images
/// </summary>
public class PexelsApiHelper
{
    private readonly ILogger<PexelsApiHelper> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://api.pexels.com/v1";

    public PexelsApiHelper(
        ILogger<PexelsApiHelper> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = configuration["Pexels:ApiKey"] ?? "";
        
        // Set authorization header
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", _apiKey);
        }
    }

    /// <summary>
    /// Search for food images on Pexels
    /// </summary>
    public async Task<string?> GetFoodImageAsync(string foodName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Build search query - add "food" to get better results
            var query = $"{foodName} food vietnamese";
            var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}&per_page=1&orientation=landscape";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pexels API returned {StatusCode} for query: {Query}", response.StatusCode, query);
                return GetFallbackFoodImage();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PexelsSearchResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (result?.Photos != null && result.Photos.Count > 0)
            {
                // Return medium size image (350px width)
                return result.Photos[0].Src?.Medium ?? GetFallbackFoodImage();
            }

            return GetFallbackFoodImage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching food image from Pexels for: {FoodName}", foodName);
            return GetFallbackFoodImage();
        }
    }

    /// <summary>
    /// Search for exercise images on Pexels
    /// </summary>
    public async Task<string?> GetExerciseImageAsync(string exerciseName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Build search query
            var query = $"{exerciseName} exercise fitness";
            var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}&per_page=1&orientation=landscape";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pexels API returned {StatusCode} for query: {Query}", response.StatusCode, query);
                return GetFallbackExerciseImage();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PexelsSearchResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (result?.Photos != null && result.Photos.Count > 0)
            {
                // Return medium size image (350px width)
                return result.Photos[0].Src?.Medium ?? GetFallbackExerciseImage();
            }

            return GetFallbackExerciseImage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exercise image from Pexels for: {ExerciseName}", exerciseName);
            return GetFallbackExerciseImage();
        }
    }

    private string GetFallbackFoodImage()
    {
        // Fallback to Unsplash food image
        return "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=400";
    }

    private string GetFallbackExerciseImage()
    {
        // Fallback to Unsplash exercise image
        return "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=400";
    }
}

/// <summary>
/// Pexels API response models
/// </summary>
public class PexelsSearchResponse
{
    public int TotalResults { get; set; }
    public int Page { get; set; }
    public int PerPage { get; set; }
    public List<PexelsPhoto> Photos { get; set; } = new();
}

public class PexelsPhoto
{
    public int Id { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Photographer { get; set; } = string.Empty;
    public string PhotographerUrl { get; set; } = string.Empty;
    public PexelsPhotoSrc? Src { get; set; }
}

public class PexelsPhotoSrc
{
    public string Original { get; set; } = string.Empty;
    public string Large2x { get; set; } = string.Empty;
    public string Large { get; set; } = string.Empty;
    public string Medium { get; set; } = string.Empty;
    public string Small { get; set; } = string.Empty;
    public string Portrait { get; set; } = string.Empty;
    public string Landscape { get; set; } = string.Empty;
    public string Tiny { get; set; } = string.Empty;
}
