using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.Extensions.Options;
using Tesseract;

namespace BookingCare.Services.AI.Services.Implementations;

public class LabResultAnalysisService : ILabResultAnalysisService
{
    private readonly ILogger<LabResultAnalysisService> _logger;
    private readonly GeminiApiHelper _geminiApiHelper;
    private readonly ServiceGeminiConfiguration _serviceConfig;
    private readonly IConversationSessionService _sessionService;
    private readonly RecommendationHelper _recommendationHelper;
    private readonly FileUploadHelper _fileUploadHelper;
    private readonly string _tesseractDataPath;
    private readonly string _tesseractLanguage;

    public LabResultAnalysisService(
        ILogger<LabResultAnalysisService> logger,
        GeminiApiHelper geminiApiHelper,
        IOptions<GeminiServicesConfiguration> geminiServicesConfig,
        IConversationSessionService sessionService,
        RecommendationHelper recommendationHelper,
        FileUploadHelper fileUploadHelper,
        IConfiguration configuration)
    {
        _logger = logger;
        _geminiApiHelper = geminiApiHelper;
        _serviceConfig = geminiServicesConfig.Value.LabResultAnalysis;
        _sessionService = sessionService;
        _recommendationHelper = recommendationHelper;
        _fileUploadHelper = fileUploadHelper;
        _tesseractDataPath = configuration["Tesseract:DataPath"] ?? "tessdata";
        _tesseractLanguage = configuration["Tesseract:Language"] ?? "vie+eng";
    }

    public async Task<LabResultAnalysisResponse> AnalyzeLabResultAsync(
        IFormFile file,
        LocationContext? location,
        Guid? userId,
        Guid? sessionId)
    {
        try
        {
            _logger.LogInformation("Starting lab result analysis for user {UserId}", userId);

            var actualSessionId = await _sessionService.GetOrCreateSessionAsync(sessionId, userId ?? Guid.Empty, location);
            _logger.LogInformation("Using session {SessionId}", actualSessionId);

            // Check if lab result already exists in this session
            var labResultExists = await _sessionService.CheckIfLabResultExistsAsync(actualSessionId);
            if (labResultExists)
            {
                _logger.LogWarning("Lab result already exists in session {SessionId}", actualSessionId);
                throw new InvalidOperationException("Mỗi cuộc trò chuyện chỉ hỗ trợ phân tích một file xét nghiệm. Vui lòng tạo cuộc trò chuyện mới để tiếp tục với file khác nhé!");
            }

            // Parallelize S3 upload and OCR extraction for better performance
            var uploadTask = _fileUploadHelper.UploadToS3Async(file, userId, "lab-results");
            var extractTask = ExtractTextFromImageAsync(file);

            await Task.WhenAll(uploadTask, extractTask);

            var imageUrl = await uploadTask;
            var extractedText = await extractTask;

            _logger.LogInformation("Uploaded image to {ImageUrl}", imageUrl);
            _logger.LogInformation("Extracted {Length} characters from image", extractedText.Length);

            var aiAnalysis = await AnalyzeWithGeminiAsync(extractedText);
            _logger.LogInformation("Gemini analysis completed");

            var (doctors, hospitals) = await _recommendationHelper.GetRecommendationsAsync(aiAnalysis.Specialties, location);

            var response = new LabResultAnalysisResponse
            {
                SessionId = actualSessionId,
                ImageUrl = imageUrl,
                ExtractedText = extractedText,
                NormalIndicators = aiAnalysis.NormalIndicators,
                AbnormalIndicators = aiAnalysis.AbnormalIndicators,
                RecommendedDoctors = doctors,
                RecommendedHospitals = hospitals,
                Disclaimer = aiAnalysis.Disclaimer ?? "Lưu ý: Đây chỉ là gợi ý định hướng y tế, không thay thế chẩn đoán chính thức của bác sĩ.",
                Timestamp = DateTime.UtcNow
            };

            await SaveLabResultAnalysisAsync(actualSessionId, userId, file.FileName, imageUrl, response, location);

            _logger.LogInformation("Lab result analysis completed and saved to session {SessionId}", actualSessionId);
            return response;
        }
        catch (InvalidOperationException)
        {
            // Re-throw InvalidOperationException to be caught by controller
            // This includes upload limit errors
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing lab result: {Message}", ex.Message);
            throw new SymptomAnalysisException("Failed to analyze lab result", ex);
        }
    }

    private async Task<string> ExtractTextFromImageAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension == ".pdf")
        {
            return await ExtractTextFromPdfAsync(file);
        }
        else
        {
            return await ExtractTextFromImageFileAsync(file);
        }
    }

    private async Task<string> ExtractTextFromPdfAsync(IFormFile file)
    {
        var tempPdfPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
        try
        {
            _logger.LogInformation("Starting PDF OCR extraction from file: {FileName}", file.FileName);

            // Save PDF to temp
            using (var stream = new FileStream(tempPdfPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return await ExtractTextFromPdfFileAsync(tempPdfPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF: {Message}", ex.Message);
            throw new InvalidOperationException($"Lỗi khi trích xuất văn bản từ PDF: {ex.Message}", ex);
        }
        finally
        {
            if (File.Exists(tempPdfPath))
            {
                File.Delete(tempPdfPath);
            }
        }
    }

    private async Task<string> ExtractTextFromPdfStreamAsync(Stream stream)
    {
        var tempPdfPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
        try
        {
            _logger.LogInformation("Starting PDF OCR extraction from stream");

            // Save stream to temp file
            using (var fileStream = new FileStream(tempPdfPath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }

            return await ExtractTextFromPdfFileAsync(tempPdfPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF stream: {Message}", ex.Message);
            throw new InvalidOperationException($"Lỗi khi trích xuất văn bản từ PDF: {ex.Message}", ex);
        }
        finally
        {
            if (File.Exists(tempPdfPath))
            {
                File.Delete(tempPdfPath);
            }
        }
    }

    private async Task<string> ExtractTextFromPdfFileAsync(string pdfPath)
    {
        var extractedTexts = new List<string>();

        // Load PDF using Docnet
        using var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(1920, 1920));

        // Process each page (limit to first 10 pages)
        var pageCount = Math.Min(docReader.GetPageCount(), 10);
        _logger.LogInformation("Processing {PageCount} pages from PDF", pageCount);

        for (int i = 0; i < pageCount; i++)
        {
            using var pageReader = docReader.GetPageReader(i);
            var rawBytes = pageReader.GetImage();
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            // Save page as PNG
            var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

            try
            {
                // Convert raw bytes to PNG
                using (var image = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    var bitmapData = image.LockBits(
                        new System.Drawing.Rectangle(0, 0, width, height),
                        System.Drawing.Imaging.ImageLockMode.WriteOnly,
                        image.PixelFormat);

                    System.Runtime.InteropServices.Marshal.Copy(rawBytes, 0, bitmapData.Scan0, rawBytes.Length);
                    image.UnlockBits(bitmapData);

                    image.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.Png);
                }

                // OCR the image
                using var engine = new TesseractEngine(_tesseractDataPath, _tesseractLanguage, EngineMode.Default);
                using var img = Pix.LoadFromFile(tempImagePath);
                using var ocrPage = engine.Process(img);

                var pageText = ocrPage.GetText();
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    extractedTexts.Add(pageText);
                    _logger.LogInformation("Extracted {Length} characters from page {PageNumber}", pageText.Length, i + 1);
                }
            }
            finally
            {
                if (File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }
            }
        }

        var combinedText = string.Join("\n\n", extractedTexts);

        if (string.IsNullOrWhiteSpace(combinedText))
        {
            throw new InvalidOperationException("Không thể trích xuất văn bản từ PDF. Vui lòng đảm bảo PDF chứa văn bản rõ ràng.");
        }

        _logger.LogInformation("PDF OCR extraction completed. Total {Length} characters from {PageCount} pages", combinedText.Length, extractedTexts.Count);
        return combinedText;
    }

    private async Task<string> ExtractTextFromImageStreamAsync(Stream stream, string fileName)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(fileName));
        try
        {
            _logger.LogInformation("Starting image OCR extraction from stream: {FileName}", fileName);

            // Save stream to temporary location
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }

            return ExtractTextFromImagePath(tempFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from image stream: {Message}", ex.Message);
            throw new InvalidOperationException($"Lỗi khi trích xuất văn bản từ ảnh: {ex.Message}", ex);
        }
        finally
        {
            // Clean up temporary file
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    private async Task<string> ExtractTextFromImageFileAsync(IFormFile file)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(file.FileName));
        try
        {
            _logger.LogInformation("Starting image OCR extraction from file: {FileName}", file.FileName);

            // Save file to temporary location
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return ExtractTextFromImagePath(tempFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from image: {Message}", ex.Message);
            throw new InvalidOperationException($"Lỗi khi trích xuất văn bản từ ảnh: {ex.Message}", ex);
        }
        finally
        {
            // Clean up temporary file
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    private string ExtractTextFromImagePath(string imagePath)
    {
        // Perform OCR using Tesseract
        using var engine = new TesseractEngine(_tesseractDataPath, _tesseractLanguage, EngineMode.Default);
        using var img = Pix.LoadFromFile(imagePath);
        using var page = engine.Process(img);

        var extractedText = page.GetText();

        _logger.LogInformation("Image OCR extraction completed. Extracted {Length} characters", extractedText.Length);

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            _logger.LogWarning("OCR extracted empty text from image");
            throw new InvalidOperationException("Không thể trích xuất văn bản từ ảnh. Vui lòng đảm bảo ảnh chứa văn bản rõ ràng.");
        }

        return extractedText;
    }

    private async Task<GeminiLabAnalysis> AnalyzeWithGeminiAsync(string extractedText)
    {
        var prompt = BuildAnalysisPrompt(extractedText);
        var geminiResponse = await CallGeminiApiAsync(prompt);
        return await ParseGeminiResponseAsync(geminiResponse);
    }

    private string BuildAnalysisPrompt(string extractedText)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("Bạn là bác sĩ AI chuyên phân tích kết quả xét nghiệm. Phân tích kết quả sau:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**KẾT QUẢ XÉT NGHIỆM:**");
        promptBuilder.AppendLine(extractedText);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**YÊU CẦU:**");
        promptBuilder.AppendLine("1. Phân loại các chỉ số thành bình thường và bất thường");
        promptBuilder.AppendLine("2. Với mỗi chỉ số bất thường:");
        promptBuilder.AppendLine("   - Giải thích ngắn gọn tại sao bất thường");
        promptBuilder.AppendLine("   - Đưa ra lời khuyên");
        promptBuilder.AppendLine("   - Chẩn đoán bệnh có thể");
        promptBuilder.AppendLine("   - **BẮT BUỘC**: Đề xuất 1 chuyên khoa phù hợp với confidence (0.0-1.0), urgency (NORMAL/URGENT), và reasons");
        promptBuilder.AppendLine("3. Đề xuất 1-3 chuyên khoa tổng quát");
        promptBuilder.AppendLine("4. Tạo disclaimer ngắn gọn");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**TRẢ VỀ JSON (ngắn gọn, chỉ các chỉ số quan trọng):**");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"normalIndicators\": [{\"name\": \"Hemoglobin\", \"value\": \"140\", \"unit\": \"g/L\", \"referenceRange\": \"130-170\"}],");
        promptBuilder.AppendLine("  \"abnormalIndicators\": [");
        promptBuilder.AppendLine("    {");
        promptBuilder.AppendLine("      \"name\": \"Bạch cầu (WBC)\",");
        promptBuilder.AppendLine("      \"value\": \"12.5\",");
        promptBuilder.AppendLine("      \"unit\": \"x10^9/L\",");
        promptBuilder.AppendLine("      \"referenceRange\": \"4.0-10.0\",");
        promptBuilder.AppendLine("      \"explanation\": \"Cao hơn bình thường, có thể nhiễm trùng\",");
        promptBuilder.AppendLine("      \"advice\": \"Cần khám bác sĩ để xác định nguyên nhân\",");
        promptBuilder.AppendLine("      \"possibleDiagnosis\": \"Nhiễm trùng cấp tính\",");
        promptBuilder.AppendLine("      \"recommendedSpecialties\": [");
        promptBuilder.AppendLine("        {");
        promptBuilder.AppendLine("          \"specialtyName\": \"Nội tổng quát\",");
        promptBuilder.AppendLine("          \"confidence\": 0.9,");
        promptBuilder.AppendLine("          \"urgency\": \"URGENT\",");
        promptBuilder.AppendLine("          \"reasons\": [\"Dấu hiệu nhiễm trùng\", \"Cần điều trị kịp thời\"]");
        promptBuilder.AppendLine("        }");
        promptBuilder.AppendLine("      ]");
        promptBuilder.AppendLine("    }");
        promptBuilder.AppendLine("  ],");
        promptBuilder.AppendLine("  \"specialties\": [\"Nội tổng quát\", \"Huyết học\"],");
        promptBuilder.AppendLine("  \"disclaimer\": \"Đây chỉ là gợi ý, cần khám bác sĩ để chẩn đoán chính xác.\"");
        promptBuilder.AppendLine("}");

        return promptBuilder.ToString();
    }

    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        return await _geminiApiHelper.CallGeminiApiWithDefaultsAsync(
            prompt,
            _serviceConfig);
    }

    private async Task<GeminiLabAnalysis> ParseGeminiResponseAsync(string geminiResponse)
    {
        try
        {
            var root = ExtractRootJsonElement(geminiResponse);
            var analysis = CreateEmptyGeminiAnalysis();

            PopulateNormalIndicators(root, analysis);
            PopulateAbnormalIndicators(root, analysis);
            PopulateSpecialties(root, analysis);
            PopulateDisclaimer(root, analysis);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini response");
            throw new InvalidOperationException("Failed to parse AI response", ex);
        }
    }

    private static JsonElement ExtractRootJsonElement(string geminiResponse)
    {
        var jsonStart = geminiResponse.IndexOf('{');
        var jsonEnd = geminiResponse.LastIndexOf('}');

        if (jsonStart == -1 || jsonEnd == -1)
        {
            throw new InvalidOperationException("No JSON found in Gemini response");
        }

        var jsonText = geminiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
        var jsonDoc = JsonDocument.Parse(jsonText);
        return jsonDoc.RootElement;
    }

    private static GeminiLabAnalysis CreateEmptyGeminiAnalysis()
    {
        return new GeminiLabAnalysis
        {
            NormalIndicators = new List<LabIndicator>(),
            AbnormalIndicators = new List<AbnormalLabIndicator>(),
            Specialties = new List<string>()
        };
    }

    private static void PopulateNormalIndicators(JsonElement root, GeminiLabAnalysis analysis)
    {
        if (!root.TryGetProperty("normalIndicators", out var normalArray))
        {
            return;
        }

        foreach (var item in normalArray.EnumerateArray())
        {
            analysis.NormalIndicators.Add(new LabIndicator
            {
                Name = item.GetProperty("name").GetString() ?? "",
                Value = item.GetProperty("value").GetString() ?? "",
                Unit = item.GetProperty("unit").GetString() ?? "",
                ReferenceRange = item.TryGetProperty("referenceRange", out var refRange)
                    ? refRange.GetString() ?? ""
                    : ""
            });
        }
    }

    private static void PopulateAbnormalIndicators(JsonElement root, GeminiLabAnalysis analysis)
    {
        if (!root.TryGetProperty("abnormalIndicators", out var abnormalArray))
        {
            return;
        }

        foreach (var item in abnormalArray.EnumerateArray())
        {
            var specialtyMatches = ParseSpecialtyMatches(item);

            analysis.AbnormalIndicators.Add(new AbnormalLabIndicator
            {
                Name = item.GetProperty("name").GetString() ?? "",
                Value = item.GetProperty("value").GetString() ?? "",
                Unit = item.GetProperty("unit").GetString() ?? "",
                ReferenceRange = item.TryGetProperty("referenceRange", out var refRange)
                    ? refRange.GetString() ?? ""
                    : "",
                Explanation = item.GetProperty("explanation").GetString() ?? "",
                Advice = item.GetProperty("advice").GetString() ?? "",
                PossibleDiagnosis = item.TryGetProperty("possibleDiagnosis", out var diagProp)
                    ? diagProp.GetString() ?? ""
                    : "",
                RecommendedSpecialties = specialtyMatches
            });
        }
    }

    private static List<SpecialtyMatch> ParseSpecialtyMatches(JsonElement abnormalItem)
    {
        var specialtyMatches = new List<SpecialtyMatch>();

        if (!abnormalItem.TryGetProperty("recommendedSpecialties", out var specArray))
        {
            return specialtyMatches;
        }

        foreach (var spec in specArray.EnumerateArray())
        {
            switch (spec.ValueKind)
            {
                case JsonValueKind.Object:
                    AddObjectSpecialtyMatch(spec, specialtyMatches);
                    break;
                case JsonValueKind.String:
                    AddStringSpecialtyMatch(spec, specialtyMatches);
                    break;
            }
        }

        return specialtyMatches;
    }

    private static void AddObjectSpecialtyMatch(JsonElement spec, List<SpecialtyMatch> specialtyMatches)
    {
        specialtyMatches.Add(new SpecialtyMatch
        {
            SpecialtyName = spec.GetProperty("specialtyName").GetString() ?? "",
            Confidence = spec.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.8,
            Urgency = spec.TryGetProperty("urgency", out var urg) ? urg.GetString() ?? "NORMAL" : "NORMAL",
            Reasons = spec.TryGetProperty("reasons", out var reasons)
                ? reasons.EnumerateArray()
                    .Select(r => r.GetString() ?? "")
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList()
                : new List<string>()
        });
    }

    private static void AddStringSpecialtyMatch(JsonElement spec, List<SpecialtyMatch> specialtyMatches)
    {
        var specialtyName = spec.GetString();
        if (string.IsNullOrEmpty(specialtyName))
        {
            return;
        }

        specialtyMatches.Add(new SpecialtyMatch
        {
            SpecialtyName = specialtyName,
            Confidence = 0.8,
            Urgency = "NORMAL",
            Reasons = new List<string> { "Phù hợp với chẩn đoán" }
        });
    }

    private static void PopulateSpecialties(JsonElement root, GeminiLabAnalysis analysis)
    {
        if (!root.TryGetProperty("specialties", out var specialtiesArray))
        {
            return;
        }

        foreach (var item in specialtiesArray.EnumerateArray())
        {
            var specialty = item.GetString();
            if (!string.IsNullOrEmpty(specialty))
            {
                analysis.Specialties.Add(specialty);
            }
        }
    }

    private static void PopulateDisclaimer(JsonElement root, GeminiLabAnalysis analysis)
    {
        if (root.TryGetProperty("disclaimer", out var disclaimerProp))
        {
            analysis.Disclaimer = disclaimerProp.GetString();
        }
    }

    private async Task SaveLabResultAnalysisAsync(Guid sessionId, Guid? userId, string fileName, string imageUrl, LabResultAnalysisResponse response, LocationContext? location)
    {
        try
        {
            var userMessage = $"Đã gửi file xét nghiệm: {fileName}";
            var aiMessage = new StringBuilder();
            aiMessage.AppendLine("**KẾT QUẢ PHÂN TÍCH XÉT NGHIỆM:**");
            aiMessage.AppendLine();

            if (response.NormalIndicators.Count > 0)
            {
                aiMessage.AppendLine("**Các chỉ số bình thường:**");
                foreach (var indicator in response.NormalIndicators)
                {
                    aiMessage.AppendLine($"- {indicator.Name}: {indicator.Value} {indicator.Unit} (Tham chiếu: {indicator.ReferenceRange})");
                }
                aiMessage.AppendLine();
            }

            if (response.AbnormalIndicators.Count > 0)
            {
                aiMessage.AppendLine("**Các chỉ số bất thường:**");
                foreach (var indicator in response.AbnormalIndicators)
                {
                    aiMessage.AppendLine($"- **{indicator.Name}**: {indicator.Value} {indicator.Unit} (Tham chiếu: {indicator.ReferenceRange})");
                    aiMessage.AppendLine($"  - Giải thích: {indicator.Explanation}");
                    aiMessage.AppendLine($"  - Lời khuyên: {indicator.Advice}");
                    if (!string.IsNullOrEmpty(indicator.PossibleDiagnosis))
                    {
                        aiMessage.AppendLine($"  - Chẩn đoán có thể: **{indicator.PossibleDiagnosis}**");
                        if (indicator.RecommendedSpecialties != null && indicator.RecommendedSpecialties.Count > 0)
                        {
                            var specialtyNames = string.Join(", ", indicator.RecommendedSpecialties.Select(s => $"**{s.SpecialtyName}**"));
                            aiMessage.AppendLine($"    - Chuyên khoa phù hợp: {specialtyNames}");
                        }
                    }
                    aiMessage.AppendLine();
                }
            }

            // Add disclaimer (plain text format like SymptomAnalysis)
            aiMessage.AppendLine();
            aiMessage.AppendLine($"Lưu ý: {response.Disclaimer}");

            var suggestions = new
            {
                doctors = response.RecommendedDoctors,
                hospitals = response.RecommendedHospitals,
                imageUrl
            };

            await _sessionService.SaveConversationHistoryAsync(
                sessionId,
                userMessage,
                aiMessage.ToString(),
                location,
                suggestions,
                userId,
                disease: null,
                questionCount: 0,
                analysisComplete: true);

            _logger.LogInformation("Saved lab result analysis to session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving lab result analysis for session {SessionId}", sessionId);
        }
    }
}

internal class GeminiLabAnalysis
{
    public List<LabIndicator> NormalIndicators { get; set; } = new();
    public List<AbnormalLabIndicator> AbnormalIndicators { get; set; } = new();
    public List<string> Specialties { get; set; } = new();
    public string? Disclaimer { get; set; }
}
