using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.FileUpload.Services;
using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.Extensions.Options;
using Tesseract;

namespace BookingCare.Services.AI.Services.Implementations;

public class LabResultAnalysisService : ILabResultAnalysisService
{
    private readonly ILogger<LabResultAnalysisService> _logger;
    private readonly HttpClient _httpClient;
    private readonly GeminiConfiguration _geminiConfig;
    private readonly DoctorService.DoctorServiceClient _doctorClient;
    private readonly BookingCare.Services.Hospital.HospitalService.HospitalServiceClient _hospitalClient;
    private readonly IConversationSessionService _sessionService;
    private readonly IFileUploadService _fileUploadService;
    private readonly string _tesseractDataPath;
    private readonly string _tesseractLanguage;

    private const int MAX_DOCTOR_RECOMMENDATIONS = 10;
    private const int MAX_HOSPITAL_RECOMMENDATIONS = 5;

    public LabResultAnalysisService(
        ILogger<LabResultAnalysisService> logger,
        HttpClient httpClient,
        IOptions<GeminiConfiguration> geminiConfig,
        DoctorService.DoctorServiceClient doctorClient,
        BookingCare.Services.Hospital.HospitalService.HospitalServiceClient hospitalClient,
        IConversationSessionService sessionService,
        IFileUploadService fileUploadService,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _geminiConfig = geminiConfig.Value;
        _doctorClient = doctorClient;
        _hospitalClient = hospitalClient;
        _sessionService = sessionService;
        _fileUploadService = fileUploadService;
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

            var imageUrl = await UploadToS3Async(file, userId);
            _logger.LogInformation("Uploaded image to {ImageUrl}", imageUrl);

            var extractedText = await ExtractTextFromImageAsync(file);
            _logger.LogInformation("Extracted {Length} characters from image", extractedText.Length);

            var aiAnalysis = await AnalyzeWithGeminiAsync(extractedText);
            _logger.LogInformation("Gemini analysis completed");

            var doctors = await GetDoctorRecommendationsAsync(aiAnalysis.Specialties, location);
            var hospitals = await GetHospitalRecommendationsAsync(aiAnalysis.Specialties, location);

            var response = new LabResultAnalysisResponse
            {
                SessionId = actualSessionId,
                ImageUrl = imageUrl,
                ExtractedText = extractedText,
                NormalIndicators = aiAnalysis.NormalIndicators,
                AbnormalIndicators = aiAnalysis.AbnormalIndicators,
                RecommendedDoctors = doctors,
                RecommendedHospitals = hospitals,
                Disclaimer = "Lưu ý: Đây chỉ là gợi ý định hướng y tế, không thay thế chẩn đoán chính thức của bác sĩ. Vui lòng đến cơ sở y tế để được khám và điều trị chính xác.",
                Timestamp = DateTime.UtcNow
            };

            await SaveLabResultAnalysisAsync(actualSessionId, userId, file.FileName, imageUrl, response, location);

            _logger.LogInformation("Lab result analysis completed and saved to session {SessionId}", actualSessionId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing lab result: {Message}", ex.Message);
            throw new SymptomAnalysisException("Failed to analyze lab result", ex);
        }
    }

    private async Task<string> UploadToS3Async(IFormFile file, Guid? userId)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var folder = $"uploads/ai/lab-results/{userId}/{timestamp}";

            using var stream = file.OpenReadStream();
            var uploadRequest = new FileUploadRequest
            {
                FileName = file.FileName,
                FileStream = stream,
                ContentType = file.ContentType,
                Folder = folder,
                GenerateUniqueFileName = true,
                Metadata = new Dictionary<string, string>
                {
                    { "user-id", userId?.ToString() ?? "anonymous" },
                    { "upload-timestamp", timestamp },
                    { "file-type", "lab-result" }
                }
            };

            var result = await _fileUploadService.UploadFileAsync(uploadRequest);

            if (result.Success)
            {
                _logger.LogInformation("File uploaded successfully to S3. CloudFront URL: {Url}", result.CloudFrontUrl);
                return result.CloudFrontUrl ?? result.FileUrl ?? string.Empty;
            }

            _logger.LogError("Failed to upload file to S3: {Error}", result.ErrorMessage);
            throw new InvalidOperationException($"Failed to upload file to S3: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3");
            throw;
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
        var extractedTexts = new List<string>();
        
        try
        {
            _logger.LogInformation("Starting PDF OCR extraction from file: {FileName}", file.FileName);
            
            // Save PDF to temp
            using (var stream = new FileStream(tempPdfPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Load PDF using Docnet
            using var docReader = DocLib.Instance.GetDocReader(tempPdfPath, new PageDimensions(1920, 1920));
            
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

    private async Task<string> ExtractTextFromImageFileAsync(IFormFile file)
    {
        try
        {
            _logger.LogInformation("Starting image OCR extraction from file: {FileName}", file.FileName);

            // Save file to temporary location
            var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(file.FileName));
            
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Perform OCR using Tesseract
                using var engine = new TesseractEngine(_tesseractDataPath, _tesseractLanguage, EngineMode.Default);
                using var img = Pix.LoadFromFile(tempFilePath);
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
            finally
            {
                // Clean up temporary file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from image: {Message}", ex.Message);
            throw new InvalidOperationException($"Lỗi khi trích xuất văn bản từ ảnh: {ex.Message}", ex);
        }
    }

    private async Task<GeminiLabAnalysis> AnalyzeWithGeminiAsync(string extractedText)
    {
        var prompt = BuildAnalysisPrompt(extractedText);
        var geminiResponse = await CallGeminiApiAsync(prompt);
        return ParseGeminiResponse(geminiResponse);
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
        promptBuilder.AppendLine("   - **BẮT BUỘC**: Đề xuất 1-2 chuyên khoa phù hợp");
        promptBuilder.AppendLine("3. Đề xuất 1-3 chuyên khoa tổng quát");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**TRẢ VỀ JSON:**");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"normalIndicators\": [{\"name\": \"Hemoglobin\", \"value\": \"140\", \"unit\": \"g/L\", \"referenceRange\": \"130-170\"}],");
        promptBuilder.AppendLine("  \"abnormalIndicators\": [");
        promptBuilder.AppendLine("    {");
        promptBuilder.AppendLine("      \"name\": \"Bạch cầu (WBC)\",");
        promptBuilder.AppendLine("      \"value\": \"12.5\",");
        promptBuilder.AppendLine("      \"unit\": \"x10^9/L\",");
        promptBuilder.AppendLine("      \"referenceRange\": \"4.0-10.0\",");
        promptBuilder.AppendLine("      \"explanation\": \"Số lượng bạch cầu cao...\",");
        promptBuilder.AppendLine("      \"advice\": \"Cần theo dõi triệu chứng...\",");
        promptBuilder.AppendLine("      \"possibleDiagnosis\": \"Nhiễm trùng\",");
        promptBuilder.AppendLine("      \"recommendedSpecialties\": [\"Nội tổng quát\", \"Huyết học\"]");
        promptBuilder.AppendLine("    }");
        promptBuilder.AppendLine("  ],");
        promptBuilder.AppendLine("  \"specialties\": [\"Nội tổng quát\", \"Nội tiết\"]");
        promptBuilder.AppendLine("}");

        return promptBuilder.ToString();
    }

    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        if (string.IsNullOrEmpty(_geminiConfig.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured");
        }

        var modelsToTry = new[] { "gemini-2.5-pro", "gemini-2.5-flash", "gemini-2.0-flash", "gemini-1.5-pro", "gemini-1.5-flash" };
        var apiVersions = new[] { "v1beta", "v1" };
        Exception? lastException = null;

        foreach (var apiVersion in apiVersions)
        {
            foreach (var model in modelsToTry)
            {
                try
                {
                    var url = $"{_geminiConfig.ApiEndpoint}/{apiVersion}/models/{model}:generateContent?key={_geminiConfig.ApiKey}";

                    var requestBody = new
                    {
                        contents = new[] { new { parts = new[] { new { text = prompt } } } },
                        generationConfig = new { temperature = 0.3, maxOutputTokens = 8192, topP = 0.95, topK = 40 }
                    };

                    var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, jsonContent);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonDocument.Parse(responseBody);

                    var text = jsonResponse.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                    _logger.LogInformation("Successfully called Gemini API with {Model}", model);
                    return text ?? string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed with {ApiVersion}/{Model}", apiVersion, model);
                    lastException = ex;
                }
            }
        }

        throw new InvalidOperationException($"Failed to call Gemini API. Last error: {lastException?.Message}", lastException);
    }

    private GeminiLabAnalysis ParseGeminiResponse(string geminiResponse)
    {
        try
        {
            var jsonStart = geminiResponse.IndexOf('{');
            var jsonEnd = geminiResponse.LastIndexOf('}');

            if (jsonStart == -1 || jsonEnd == -1)
            {
                throw new InvalidOperationException("No JSON found in Gemini response");
            }

            var jsonText = geminiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;

            var analysis = new GeminiLabAnalysis
            {
                NormalIndicators = new List<LabIndicator>(),
                AbnormalIndicators = new List<AbnormalLabIndicator>(),
                Specialties = new List<string>()
            };

            if (root.TryGetProperty("normalIndicators", out var normalArray))
            {
                foreach (var item in normalArray.EnumerateArray())
                {
                    analysis.NormalIndicators.Add(new LabIndicator
                    {
                        Name = item.GetProperty("name").GetString() ?? "",
                        Value = item.GetProperty("value").GetString() ?? "",
                        Unit = item.GetProperty("unit").GetString() ?? "",
                        ReferenceRange = item.TryGetProperty("referenceRange", out var refRange) ? refRange.GetString() ?? "" : ""
                    });
                }
            }

            if (root.TryGetProperty("abnormalIndicators", out var abnormalArray))
            {
                foreach (var item in abnormalArray.EnumerateArray())
                {
                    var specialtyNames = new List<string>();
                    if (item.TryGetProperty("recommendedSpecialties", out var specArray))
                    {
                        foreach (var spec in specArray.EnumerateArray())
                        {
                            var specialty = spec.GetString();
                            if (!string.IsNullOrEmpty(specialty))
                            {
                                specialtyNames.Add(specialty);
                            }
                        }
                    }

                    var diagnosis = item.TryGetProperty("possibleDiagnosis", out var diagProp) ? diagProp.GetString() ?? "" : "";
                    var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";

                    var specialtyMatches = specialtyNames.Count > 0
                        ? ConvertToSpecialtyMatches(specialtyNames, diagnosis, name)
                        : InferSpecialtiesFromDiagnosis(diagnosis, name);

                    analysis.AbnormalIndicators.Add(new AbnormalLabIndicator
                    {
                        Name = item.GetProperty("name").GetString() ?? "",
                        Value = item.GetProperty("value").GetString() ?? "",
                        Unit = item.GetProperty("unit").GetString() ?? "",
                        ReferenceRange = item.TryGetProperty("referenceRange", out var refRange) ? refRange.GetString() ?? "" : "",
                        Explanation = item.GetProperty("explanation").GetString() ?? "",
                        Advice = item.GetProperty("advice").GetString() ?? "",
                        PossibleDiagnosis = diagnosis,
                        RecommendedSpecialties = specialtyMatches
                    });
                }
            }

            if (root.TryGetProperty("specialties", out var specialtiesArray))
            {
                foreach (var item in specialtiesArray.EnumerateArray())
                {
                    var specialty = item.GetString();
                    if (!string.IsNullOrEmpty(specialty))
                    {
                        analysis.Specialties.Add(specialty);
                    }
                }
            }

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini response");
            throw new InvalidOperationException("Failed to parse AI response", ex);
        }
    }

    private List<SpecialtyMatch> ConvertToSpecialtyMatches(List<string> specialtyNames, string diagnosis, string indicatorName)
    {
        var matches = new List<SpecialtyMatch>();
        var combinedText = $"{diagnosis} {indicatorName}".ToLower();

        foreach (var specialtyName in specialtyNames)
        {
            var confidence = 0.8;
            var urgency = "NORMAL";
            var reasons = new List<string> { $"Phù hợp với chẩn đoán: {diagnosis}" };

            if (combinedText.Contains("nhiễm trùng") || combinedText.Contains("viêm"))
            {
                urgency = "URGENT";
                confidence = 0.9;
            }

            matches.Add(new SpecialtyMatch
            {
                SpecialtyName = specialtyName,
                Confidence = confidence,
                Urgency = urgency,
                Reasons = reasons
            });
        }

        return matches;
    }

    private List<SpecialtyMatch> InferSpecialtiesFromDiagnosis(string diagnosis, string indicatorName)
    {
        var specialties = new List<SpecialtyMatch>();
        var combinedText = $"{diagnosis} {indicatorName}".ToLower();

        // Fetch all specialties from database
        List<string> allSpecialtyNames;
        try
        {
            var request = new GetAllSpecialtiesRequest();
            var response = _doctorClient.GetAllSpecialtiesAsync(request).GetAwaiter().GetResult();
            allSpecialtyNames = response.Specialties.Select(s => s.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch specialties from database, using fallback");
            // Fallback to default specialty
            specialties.Add(new SpecialtyMatch 
            { 
                SpecialtyName = "Nội tổng quát", 
                Confidence = 0.6, 
                Urgency = "NORMAL", 
                Reasons = new List<string> { "Đánh giá tổng quát" } 
            });
            return specialties;
        }

        // Define keyword mappings for specialty inference
        var specialtyKeywords = new Dictionary<string, (List<string> Keywords, double Confidence, string Urgency, List<string> Reasons)>
        {
            ["Nội tiết"] = (
                new List<string> { "đái tháo đường", "glucose", "đường huyết", "insulin", "tuyến giáp", "hormone" },
                0.9,
                "NORMAL",
                new List<string> { "Rối loạn chuyển hóa đường", "Cần kiểm tra HbA1c" }
            ),
            ["Tim mạch"] = (
                new List<string> { "cholesterol", "triglyceride", "lipid", "tim", "huyết áp", "mạch máu" },
                0.9,
                "NORMAL",
                new List<string> { "Rối loạn lipid máu", "Nguy cơ tim mạch" }
            ),
            ["Huyết học"] = (
                new List<string> { "hồng cầu", "bạch cầu", "wbc", "rbc", "hemoglobin", "thiếu máu", "tiểu cầu" },
                0.9,
                "NORMAL",
                new List<string> { "Rối loạn hồng cầu/bạch cầu" }
            ),
            ["Tiêu hóa"] = (
                new List<string> { "gan", "alt", "ast", "bilirubin", "tiêu hóa", "dạ dày", "ruột" },
                0.85,
                "NORMAL",
                new List<string> { "Rối loạn chức năng gan" }
            ),
            ["Thận - Tiết niệu"] = (
                new List<string> { "thận", "creatinine", "ure", "urea", "protein niệu" },
                0.9,
                "NORMAL",
                new List<string> { "Rối loạn chức năng thận" }
            ),
            ["Nội khoa"] = (
                new List<string> { "nhiễm trùng", "viêm", "sốt", "infection" },
                0.85,
                "URGENT",
                new List<string> { "Dấu hiệu nhiễm trùng", "Cần xác định nguyên nhân" }
            )
        };

        // Match keywords with database specialties
        foreach (var kvp in specialtyKeywords)
        {
            var keywordSpecialtyName = kvp.Key;
            var (keywords, confidence, urgency, reasons) = kvp.Value;

            // Check if any keyword matches
            if (keywords.Any(keyword => combinedText.Contains(keyword)))
            {
                // Find matching specialty in database (exact or fuzzy match)
                var dbSpecialty = allSpecialtyNames.FirstOrDefault(s =>
                    s.Equals(keywordSpecialtyName, StringComparison.OrdinalIgnoreCase) ||
                    s.Contains(keywordSpecialtyName, StringComparison.OrdinalIgnoreCase) ||
                    keywordSpecialtyName.Contains(s, StringComparison.OrdinalIgnoreCase));

                if (dbSpecialty != null)
                {
                    specialties.Add(new SpecialtyMatch
                    {
                        SpecialtyName = dbSpecialty,
                        Confidence = confidence,
                        Urgency = urgency,
                        Reasons = reasons
                    });
                }
            }
        }

        // Always add "Nội tổng quát" or similar general specialty as fallback
        if (specialties.Count == 0)
        {
            var generalSpecialty = allSpecialtyNames.FirstOrDefault(s =>
                s.Contains("Nội", StringComparison.OrdinalIgnoreCase) &&
                (s.Contains("tổng quát", StringComparison.OrdinalIgnoreCase) || s.Contains("khoa", StringComparison.OrdinalIgnoreCase)));

            if (generalSpecialty != null)
            {
                specialties.Add(new SpecialtyMatch
                {
                    SpecialtyName = generalSpecialty,
                    Confidence = 0.6,
                    Urgency = "NORMAL",
                    Reasons = new List<string> { "Đánh giá tổng quát" }
                });
            }
            else
            {
                // Ultimate fallback - use first specialty from database
                specialties.Add(new SpecialtyMatch
                {
                    SpecialtyName = allSpecialtyNames.FirstOrDefault() ?? "Nội tổng quát",
                    Confidence = 0.5,
                    Urgency = "NORMAL",
                    Reasons = new List<string> { "Đánh giá tổng quát" }
                });
            }
        }
        else if (!specialties.Any(s => s.SpecialtyName.Contains("Nội", StringComparison.OrdinalIgnoreCase)))
        {
            // Add general internal medicine as secondary option
            var generalSpecialty = allSpecialtyNames.FirstOrDefault(s =>
                s.Contains("Nội", StringComparison.OrdinalIgnoreCase) &&
                (s.Contains("tổng quát", StringComparison.OrdinalIgnoreCase) || s.Contains("khoa", StringComparison.OrdinalIgnoreCase)));

            if (generalSpecialty != null && specialties.Count < 3)
            {
                specialties.Add(new SpecialtyMatch
                {
                    SpecialtyName = generalSpecialty,
                    Confidence = 0.7,
                    Urgency = "NORMAL",
                    Reasons = new List<string> { "Đánh giá tổng quát sức khỏe" }
                });
            }
        }

        return specialties;
    }


    private async Task<List<DoctorRecommendation>> GetDoctorRecommendationsAsync(List<string> specialties, LocationContext? location)
    {
        if (specialties.Count == 0) return new List<DoctorRecommendation>();

        try
        {
            var specialtyIds = await MatchSpecialtiesToIdsAsync(specialties);
            if (specialtyIds.Count == 0) return new List<DoctorRecommendation>();

            var request = new FilterDoctorsForRecommendationRequest { MaxResults = MAX_DOCTOR_RECOMMENDATIONS * 2 };
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
                .Select(x => new DoctorRecommendation
                {
                    Id = x.Doctor.Id,
                    Name = x.Doctor.FullName,
                    SpecialtyName = x.Doctor.SpecialtyName,
                    HospitalName = x.Doctor.HospitalName,
                    Rating = x.Doctor.Rating,
                    YearOfExperience = x.Doctor.YearsOfExperience,
                    ServiceTypeName = x.Doctor.ServiceTypeName,
                    Price = x.Doctor.ConsultationFee > 0 ? $"{x.Doctor.ConsultationFee:N0} VNĐ" : null,
                    AvatarUrl = x.Doctor.AvatarUrl,
                    RecommendationScore = x.Score
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting doctor recommendations");
            return new List<DoctorRecommendation>();
        }
    }

    private async Task<List<HospitalRecommendation>> GetHospitalRecommendationsAsync(List<string> specialties, LocationContext? location)
    {
        if (specialties.Count == 0) return new List<HospitalRecommendation>();

        try
        {
            var specialtyIds = await MatchSpecialtiesToIdsAsync(specialties);
            if (specialtyIds.Count == 0) return new List<HospitalRecommendation>();

            var allHospitals = new List<HospitalReply>();

            foreach (var specialtyId in specialtyIds.Take(3))
            {
                try
                {
                    var request = new GetHospitalsBySpecialtyRequest { SpecialtyId = specialtyId.ToString() };
                    var response = await _hospitalClient.GetHospitalsBySpecialtyAsync(request);
                    allHospitals.AddRange(response.Hospitals);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting hospitals for specialty {SpecialtyId}", specialtyId);
                }
            }

            return allHospitals
                .GroupBy(h => h.Id)
                .Select(g => g.First())
                .Select(h => new HospitalRecommendation
                {
                    Id = h.Id,
                    Name = h.Name,
                    Address = h.Address,
                    SpecialtyNames = specialties,
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

    private async Task<List<Guid>> MatchSpecialtiesToIdsAsync(List<string> specialtyNames)
    {
        var specialtyIds = new List<Guid>();

        try
        {
            var request = new GetAllSpecialtiesRequest();
            var response = await _doctorClient.GetAllSpecialtiesAsync(request);

            foreach (var specialtyName in specialtyNames)
            {
                var match = response.Specialties.FirstOrDefault(s =>
                    s.Name.Equals(specialtyName, StringComparison.OrdinalIgnoreCase) ||
                    s.Name.Contains(specialtyName, StringComparison.OrdinalIgnoreCase) ||
                    specialtyName.Contains(s.Name, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    specialtyIds.Add(Guid.Parse(match.Id));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching specialties to IDs");
        }

        return specialtyIds;
    }

    private double CalculateDoctorScore(DoctorRecommendationInfo doctor, LocationContext? location)
    {
        double score = 0;
        if (location != null && !string.IsNullOrEmpty(location.ProvinceId)) score += 0.4;
        score += (doctor.Rating / 5.0) * 0.3;
        score += Math.Min(doctor.YearsOfExperience / 20.0, 1.0) * 0.1;
        return score;
    }

    private double CalculateHospitalScore(HospitalReply hospital, LocationContext? location)
    {
        double score = 0.5;
        if (location != null && !string.IsNullOrEmpty(location.DisplayName))
        {
            var address = hospital.Address?.ToLowerInvariant() ?? "";
            var locationName = location.DisplayName.ToLowerInvariant();
            if (address.Contains(locationName)) score += 0.5;
        }
        return score;
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
                        aiMessage.AppendLine($"  - Chẩn đoán có thể: {indicator.PossibleDiagnosis}");
                        if (indicator.RecommendedSpecialties != null && indicator.RecommendedSpecialties.Count > 0)
                        {
                            var specialtyNames = string.Join(", ", indicator.RecommendedSpecialties.Select(s => s.SpecialtyName));
                            aiMessage.AppendLine($"    - Chuyên khoa phù hợp: {specialtyNames}");
                        }
                    }
                    aiMessage.AppendLine();
                }
            }

            aiMessage.AppendLine(response.Disclaimer);

            var suggestions = new { doctors = response.RecommendedDoctors, hospitals = response.RecommendedHospitals };

            await _sessionService.SaveConversationHistoryAsync(sessionId, userMessage, aiMessage.ToString(), location, suggestions, userId, disease: null, questionCount: 0, analysisComplete: true);

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
}
