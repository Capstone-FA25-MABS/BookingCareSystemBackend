using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service for general Gemini AI text generation tasks
/// </summary>
public class GeminiService : IGeminiService
{
    private readonly ILogger<GeminiService> _logger;
    private readonly HttpClient _httpClient;
    private readonly GeminiConfiguration _config;
    private readonly IMemoryCache _cache;

    private const string CACHE_KEY_PREFIX = "gemini_disease_translation_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(30); // Cache translations for 30 days

    public GeminiService(
        ILogger<GeminiService> logger,
        HttpClient httpClient,
        IOptions<GeminiConfiguration> config,
        IMemoryCache cache)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = config.Value;
        _cache = cache;

        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<string> TranslateDiseaseNameAsync(
        string englishDiseaseName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"{CACHE_KEY_PREFIX}{englishDiseaseName.ToLower()}";
            if (_cache.TryGetValue<string>(cacheKey, out var cachedTranslation))
            {
                _logger.LogInformation("Using cached translation for: {DiseaseName}", englishDiseaseName);
                return cachedTranslation!;
            }

            _logger.LogInformation("Translating disease name: {DiseaseName}", englishDiseaseName);

            var prompt = $@"Bạn là một chuyên gia y khoa. Hãy dịch tên bệnh da liễu sau từ tiếng Anh sang tiếng Việt.
Chỉ trả về tên bệnh bằng tiếng Việt, không giải thích thêm.

Tên bệnh (tiếng Anh): {englishDiseaseName}

Lưu ý:
- Nếu là tên bệnh chuyên môn, hãy dùng thuật ngữ y khoa tiếng Việt chính xác
- Nếu không có thuật ngữ tiếng Việt phổ biến, hãy giữ nguyên tên tiếng Anh
- Trả về ngắn gọn, chỉ tên bệnh

Tên bệnh (tiếng Việt):";

            var translation = await GenerateTextAsync(prompt, temperature: 0.1, cancellationToken);

            // Clean up the translation (remove quotes, extra whitespace, etc.)
            translation = translation.Trim().Trim('"', '\'', '.', ',');

            // Cache the translation
            _cache.Set(cacheKey, translation, CacheDuration);

            _logger.LogInformation("Translated '{English}' to '{Vietnamese}'", englishDiseaseName, translation);

            return translation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating disease name: {DiseaseName}", englishDiseaseName);
            
            // Fallback: return the English name with proper capitalization
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                englishDiseaseName.ToLower().Replace("_", " ")
            );
        }
    }

    public async Task<string> GenerateDermatologyConclusionAsync(
        string diseaseName,
        double confidence,
        string severity,
        string riskCategory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"gemini_conclusion_{diseaseName.ToLower()}_{severity}_{riskCategory}";
            if (_cache.TryGetValue<string>(cacheKey, out var cachedConclusion))
            {
                _logger.LogInformation("Using cached conclusion for: {DiseaseName}", diseaseName);
                return cachedConclusion!;
            }

            _logger.LogInformation("Generating detailed conclusion for: {DiseaseName}", diseaseName);

            var confidencePercent = (confidence * 100).ToString("F0");
            
            var prompt = $@"Bạn là một bác sĩ da liễu chuyên nghiệp. Hãy viết một kết luận y khoa chi tiết về bệnh da liễu sau đây.

THÔNG TIN CHẨN ĐOÁN:
- Tên bệnh: {diseaseName}
- Độ tin cậy: {confidencePercent}%
- Mức độ nghiêm trọng: {severity}
- Nguy cơ ác tính: {riskCategory}

YÊU CẦU:
1. Viết bằng tiếng Việt, dùng thuật ngữ y khoa chính xác nhưng dễ hiểu
2. Độ dài: 500-800 từ
3. Chia thành các sections sau (dùng markdown headers):

## Mô tả bệnh
- Giải thích bệnh là gì
- Đặc điểm nhận dạng trên da
- Tần suất gặp

## Nguyên nhân
- Các nguyên nhân chính gây bệnh
- Yếu tố nguy cơ
- Cơ chế bệnh sinh (nếu có)

## Triệu chứng
- Các triệu chứng điển hình
- Dấu hiệu cần chú ý
- Biến chứng có thể xảy ra

## Điều trị
- Phương pháp điều trị chính
- Thuốc thường dùng (nếu có)
- Thời gian điều trị dự kiến
- Lưu ý khi điều trị

## Tiên lượng
- Khả năng khỏi bệnh
- Nguy cơ tái phát
- Các biện pháp phòng ngừa

LƯU Ý:
- Không đưa ra chẩn đoán chắc chắn, chỉ cung cấp thông tin tham khảo
- Nhấn mạnh cần đến gặp bác sĩ da liễu để được thăm khám trực tiếp
- Viết theo phong cách chuyên nghiệp nhưng dễ hiểu cho người bệnh
- Không dùng bullet points quá nhiều, ưu tiên viết thành đoạn văn

Hãy viết kết luận chi tiết:";

            var conclusion = await GenerateTextAsync(prompt, temperature: 0.4, cancellationToken);

            // Cache the conclusion for 7 days
            _cache.Set(cacheKey, conclusion, TimeSpan.FromDays(7));

            _logger.LogInformation("Generated conclusion for '{DiseaseName}': Length={Length} characters", 
                diseaseName, conclusion.Length);

            return conclusion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dermatology conclusion for: {DiseaseName}", diseaseName);
            
            // Fallback: return a basic conclusion
            return $@"## Thông tin về {diseaseName}

Dựa trên phân tích hình ảnh, tổn thương da có khả năng là {diseaseName} với độ tin cậy {(confidence * 100):F0}%.

**Lưu ý quan trọng:** Đây chỉ là kết quả phân tích sơ bộ từ hình ảnh. Để có chẩn đoán chính xác và phương pháp điều trị phù hợp, bạn cần đến gặp bác sĩ da liễu để được thăm khám trực tiếp.

Bác sĩ sẽ:
- Khám lâm sàng chi tiết
- Đánh giá toàn diện tình trạng da
- Có thể chỉ định các xét nghiệm cần thiết
- Đưa ra phương án điều trị phù hợp với tình trạng cụ thể của bạn

Vui lòng không tự ý điều trị mà hãy tìm đến các cơ sở y tế uy tín để được tư vấn và điều trị đúng cách.";
        }
    }

    public async Task<string> GenerateGeneralAdviceAsync(
        string diseaseName,
        string severity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"gemini_advice_{diseaseName.ToLower()}_{severity}";
            if (_cache.TryGetValue<string>(cacheKey, out var cachedAdvice))
            {
                _logger.LogInformation("Using cached advice for: {DiseaseName}", diseaseName);
                return cachedAdvice!;
            }

            _logger.LogInformation("Generating general advice for: {DiseaseName}", diseaseName);

            var prompt = $@"Bạn là một bác sĩ da liễu. Hãy đưa ra 3-5 lời khuyên chăm sóc da cụ thể cho bệnh nhân bị {diseaseName} (mức độ: {severity}).

YÊU CẦU:
- Viết bằng tiếng Việt
- Mỗi lời khuyên là một câu ngắn gọn, dễ hiểu
- Tập trung vào: vệ sinh da, chế độ ăn uống, sinh hoạt, điều cần tránh
- Phù hợp với mức độ nghiêm trọng {severity}
- Không đưa ra lời khuyên về thuốc cụ thể
- Mỗi lời khuyên trên một dòng, bắt đầu bằng dấu gạch ngang (-)

Ví dụ format:
- Giữ vệ sinh da sạch sẽ, rửa mặt 2 lần/ngày
- Tránh chạm tay vào vùng da bị tổn thương
- Sử dụng kem chống nắng SPF 30+ khi ra ngoài

Hãy đưa ra 3-5 lời khuyên cho {diseaseName}:";

            var advice = await GenerateTextAsync(prompt, temperature: 0.3, cancellationToken);

            // Cache the advice for 7 days
            _cache.Set(cacheKey, advice, TimeSpan.FromDays(7));

            _logger.LogInformation("Generated advice for '{DiseaseName}'", diseaseName);

            return advice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating general advice for: {DiseaseName}", diseaseName);
            
            // Fallback: return generic advice
            return @"- Theo dõi tổn thương da và đến gặp bác sĩ nếu có thay đổi
- Giữ vệ sinh da sạch sẽ
- Tránh tiếp xúc trực tiếp với ánh nắng mặt trời";
        }
    }

    public async Task<string> GenerateTextAsync(
        string prompt,
        double temperature = 0.3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.ApiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured");
            }

            // Build request URL
            var url = $"{_config.ApiEndpoint}/v1/models/{_config.Model}:generateContent?key={_config.ApiKey}";

            // Build request payload
            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = temperature,
                    maxOutputTokens = _config.MaxTokens
                }
            };

            var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogDebug("Calling Gemini API: {Url}", url);

            // Make API request
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);
                throw new InvalidOperationException($"Gemini API returned error: {response.StatusCode}");
            }

            // Parse response
            var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
            {
                _logger.LogWarning("No candidates in Gemini response");
                throw new InvalidOperationException("Gemini API returned no candidates");
            }

            var firstCandidate = geminiResponse.Candidates[0];
            var generatedText = firstCandidate.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                _logger.LogWarning("Empty text from Gemini API");
                throw new InvalidOperationException("Gemini API returned empty text");
            }

            _logger.LogInformation("Gemini text generation successful: Length={Length} characters", generatedText.Length);

            return generatedText.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            throw;
        }
    }

    #region Response Models

    private class GeminiApiResponse
    {
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        public Content? Content { get; set; }
    }

    private class Content
    {
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        public string? Text { get; set; }
    }

    #endregion
}
