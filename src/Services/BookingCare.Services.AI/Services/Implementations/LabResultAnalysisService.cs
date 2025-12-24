using System.Text;
using System.Text.Encodings.Web;
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
using SkiaSharp;

namespace BookingCare.Services.AI.Services.Implementations;

public class LabResultAnalysisService : ILabResultAnalysisService
{
    private readonly ILogger<LabResultAnalysisService> _logger;
    private readonly GroqApiHelper _groqApiHelper;
    private readonly ServiceGroqConfiguration _serviceConfig;
    private readonly IConversationSessionService _sessionService;
    private readonly RecommendationHelper _recommendationHelper;
    private readonly FileUploadHelper _fileUploadHelper;
    private readonly ILabResultCacheService _cacheService;
    private readonly ILabResultKeywordExtractor _keywordExtractor;
    private readonly string _tesseractDataPath;
    private readonly string _tesseractLanguage;

    // Dùng encoder relaxed để giữ nguyên ký tự UTF-8 (tránh \uXXXX khi lưu DB)
    private static readonly JsonSerializerOptions Utf8JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public LabResultAnalysisService(
        ILogger<LabResultAnalysisService> logger,
        GroqApiHelper groqApiHelper,
        IOptions<GroqServicesConfiguration> groqServicesConfig,
        IConversationSessionService sessionService,
        RecommendationHelper recommendationHelper,
        FileUploadHelper fileUploadHelper,
        ILabResultCacheService cacheService,
        ILabResultKeywordExtractor keywordExtractor,
        IConfiguration configuration)
    {
        _logger = logger;
        _groqApiHelper = groqApiHelper;
        _serviceConfig = groqServicesConfig.Value.LabResultAnalysis;
        _sessionService = sessionService;
        _recommendationHelper = recommendationHelper;
        _fileUploadHelper = fileUploadHelper;
        _cacheService = cacheService;
        _keywordExtractor = keywordExtractor;
        
        // Get DataPath from config, with fallback to TESSDATA_PREFIX env or default
        var configuredPath = configuration["Tesseract:DataPath"];
        var envTessdataPrefix = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
        
        // Use TESSDATA_PREFIX if configured path is relative or doesn't exist
        if (!string.IsNullOrEmpty(configuredPath) && Path.IsPathRooted(configuredPath) && Directory.Exists(configuredPath))
        {
            _tesseractDataPath = configuredPath;
        }
        else if (!string.IsNullOrEmpty(envTessdataPrefix) && Directory.Exists(envTessdataPrefix))
        {
            _tesseractDataPath = envTessdataPrefix;
            _logger.LogWarning("Using TESSDATA_PREFIX environment variable as DataPath config is invalid: {ConfigPath}", configuredPath);
        }
        else
        {
            _tesseractDataPath = "/usr/share/tesseract-ocr/tessdata";
            _logger.LogWarning("Using default tessdata path as both config and env are invalid");
        }
        
        _tesseractLanguage = configuration["Tesseract:Language"] ?? "vie+eng";
        
        // Log Tesseract configuration with more details
        _logger.LogInformation("=== Tesseract Configuration ===");
        _logger.LogInformation("DataPath: {DataPath}", _tesseractDataPath);
        _logger.LogInformation("Language: {Language}", _tesseractLanguage);
        _logger.LogInformation("TESSDATA_PREFIX env: {TessDataPrefix}", Environment.GetEnvironmentVariable("TESSDATA_PREFIX"));
        _logger.LogInformation("Current Directory: {CurrentDir}", Directory.GetCurrentDirectory());
        
        // Check if tessdata directory exists
        if (Directory.Exists(_tesseractDataPath))
        {
            _logger.LogInformation("✅ Tessdata directory EXISTS");
            try
            {
                var trainedDataFiles = Directory.GetFiles(_tesseractDataPath, "*.traineddata");
                _logger.LogInformation("Found {Count} traineddata files:", trainedDataFiles.Length);
                foreach (var file in trainedDataFiles)
                {
                    var fileInfo = new FileInfo(file);
                    _logger.LogInformation("  - {FileName} ({Size} bytes)", Path.GetFileName(file), fileInfo.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing traineddata files");
            }
        }
        else
        {
            _logger.LogError("❌ Tessdata directory does NOT exist: {Path}", _tesseractDataPath);
            
            // Try to find tessdata in common locations
            var commonPaths = new[]
            {
                "/usr/share/tesseract-ocr/tessdata",
                "/usr/share/tesseract-ocr/4.00/tessdata",
                "/usr/share/tesseract-ocr/5/tessdata",
                "/app/tessdata",
                "/usr/local/share/tessdata"
            };
            
            _logger.LogInformation("Checking common tessdata locations:");
            foreach (var path in commonPaths)
            {
                var exists = Directory.Exists(path);
                _logger.LogInformation("  {Status} {Path}", exists ? "✅" : "❌", path);
                if (exists)
                {
                    try
                    {
                        var files = Directory.GetFiles(path, "*.traineddata");
                        _logger.LogInformation("    Found {Count} files: {Files}", 
                            files.Length, 
                            string.Join(", ", files.Select(f => Path.GetFileName(f))));
                    }
                    catch { }
                }
            }
        }
        _logger.LogInformation("==============================");
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

            // Upload file to S3 (sequential, tránh conflict stream với OCR)
            var imageUrl = await _fileUploadHelper.UploadToS3Async(file, userId, "lab-results");

            // Thực hiện OCR sau khi upload xong, giống pattern của DermatologyAnalysisService
            var extractedText = await ExtractTextFromImageAsync(file);

            _logger.LogInformation("Uploaded image to {ImageUrl}", imageUrl);
            _logger.LogInformation("Extracted {Length} characters from image", extractedText.Length);

            // Try cache lookup first
            var cachedAnalysis = await TryGetCachedAnalysisAsync(extractedText);
            GeminiLabAnalysis aiAnalysis;

            if (cachedAnalysis != null)
            {
                _logger.LogInformation("✅ Cache hit! Using cached analysis");
                aiAnalysis = cachedAnalysis;
            }
            else
            {
                _logger.LogInformation("❌ Cache miss, calling Groq API");
                aiAnalysis = await AnalyzeWithGroqAsync(extractedText);

                // Save to cache after successful analysis
                await SaveAnalysisToCacheAsync(extractedText, aiAnalysis);
            }

            _logger.LogInformation("Analysis completed");

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

    private void VerifyTesseractSetup()
    {
        _logger.LogInformation("🔍 Verifying Tesseract setup...");
        
        // Check directory
        if (!Directory.Exists(_tesseractDataPath))
        {
            throw new InvalidOperationException($"Tesseract data directory not found: {_tesseractDataPath}");
        }
        
        // Check for language files
        var languages = _tesseractLanguage.Split('+');
        foreach (var lang in languages)
        {
            var langFile = Path.Combine(_tesseractDataPath, $"{lang}.traineddata");
            if (!File.Exists(langFile))
            {
                _logger.LogError("❌ Missing language file: {LangFile}", langFile);
                throw new InvalidOperationException($"Tesseract language file not found: {langFile}. Available files: {string.Join(", ", Directory.GetFiles(_tesseractDataPath, "*.traineddata").Select(f => Path.GetFileName(f)))}");
            }
            else
            {
                _logger.LogInformation("✅ Language file exists: {LangFile}", langFile);
            }
        }
        
        _logger.LogInformation("✅ Tesseract setup verified successfully");
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
            
            // Verify Tesseract is properly configured before processing
            VerifyTesseractSetup();

            // Save PDF to temp
            using (var stream = new FileStream(tempPdfPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return await ExtractTextFromPdfFileAsync(tempPdfPath);
        }
        catch (InvalidOperationException)
        {
            // Re-throw configuration errors as-is
            throw;
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

        try
        {
            // Load PDF using Docnet (cross-platform)
            using var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(1920, 1920));

            // Process each page (limit to first 10 pages)
            var pageCount = Math.Min(docReader.GetPageCount(), 10);
            _logger.LogInformation("Processing {PageCount} pages from PDF", pageCount);

            var successfulPages = 0;
            var failedPages = 0;

            for (int i = 0; i < pageCount; i++)
            {
                try
                {
                    using var pageReader = docReader.GetPageReader(i);
                    var rawBytes = pageReader.GetImage();
                    var width = pageReader.GetPageWidth();
                    var height = pageReader.GetPageHeight();

                    // Save page as PNG
                    var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

                    try
                    {
                        // Convert raw bytes to PNG using SkiaSharp (cross-platform)
                        var imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

                        using (var bitmap = new SKBitmap(imageInfo))
                        {
                            // Copy raw bytes to SKBitmap
                            var pixelPtr = bitmap.GetPixels();
                            System.Runtime.InteropServices.Marshal.Copy(rawBytes, 0, pixelPtr, rawBytes.Length);

                            // Save as PNG
                            using (var image = SKImage.FromBitmap(bitmap))
                            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                            using (var stream = File.OpenWrite(tempImagePath))
                            {
                                data.SaveTo(stream);
                            }
                        }

                        // Debug logging for Tesseract configuration
                        _logger.LogInformation("🔍 Tesseract Debug Info:");
                        _logger.LogInformation("  - DataPath: {DataPath}", _tesseractDataPath);
                        _logger.LogInformation("  - Language: {Language}", _tesseractLanguage);
                        _logger.LogInformation("  - TESSDATA_PREFIX env: {TessDataPrefix}", Environment.GetEnvironmentVariable("TESSDATA_PREFIX"));
                        _logger.LogInformation("  - Directory exists: {Exists}", Directory.Exists(_tesseractDataPath));
                        
                        if (Directory.Exists(_tesseractDataPath))
                        {
                            var files = Directory.GetFiles(_tesseractDataPath, "*.traineddata");
                            _logger.LogInformation("  - Found {Count} traineddata files: {Files}", 
                                files.Length, 
                                string.Join(", ", files.Select(f => Path.GetFileName(f))));
                        }
                        else
                        {
                            _logger.LogError("  - ❌ Tessdata directory does NOT exist!");
                        }

                        // OCR the image with better error handling
                        using var engine = new TesseractEngine(_tesseractDataPath, _tesseractLanguage, EngineMode.Default);

                        // Set page segmentation mode for better OCR results
                        engine.DefaultPageSegMode = PageSegMode.Auto;

                        using var img = Pix.LoadFromFile(tempImagePath);
                        using var ocrPage = engine.Process(img);

                        var pageText = ocrPage.GetText();

                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            extractedTexts.Add(pageText.Trim());
                            successfulPages++;
                            _logger.LogInformation("✅ Page {PageNumber}: Extracted {Length} characters", i + 1, pageText.Length);
                        }
                        else
                        {
                            failedPages++;
                            _logger.LogWarning("⚠️ Page {PageNumber}: No text extracted (might be blank or image-only)", i + 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedPages++;
                        _logger.LogError(ex, "❌ Error processing page {PageNumber}: {Message}", i + 1, ex.Message);
                        
                        // Log additional debug info for TesseractException
                        if (ex is Tesseract.TesseractException)
                        {
                            _logger.LogError("Tesseract Error Details:");
                            _logger.LogError("  - DataPath used: {DataPath}", _tesseractDataPath);
                            _logger.LogError("  - Language used: {Language}", _tesseractLanguage);
                            _logger.LogError("  - TESSDATA_PREFIX: {TessDataPrefix}", Environment.GetEnvironmentVariable("TESSDATA_PREFIX"));
                            _logger.LogError("  - Working Directory: {WorkingDir}", Directory.GetCurrentDirectory());
                        }
                        
                        // Continue with next page instead of failing completely
                    }
                    finally
                    {
                        if (File.Exists(tempImagePath))
                        {
                            try
                            {
                                File.Delete(tempImagePath);
                            }
                            catch
                            {
                                // Ignore cleanup errors
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedPages++;
                    _logger.LogError(ex, "❌ Error reading page {PageNumber} from PDF", i + 1);
                }
            }

            var combinedText = string.Join("\n\n", extractedTexts);

            _logger.LogInformation("PDF OCR Summary: {SuccessfulPages} successful, {FailedPages} failed out of {TotalPages} pages",
                successfulPages, failedPages, pageCount);

            if (string.IsNullOrWhiteSpace(combinedText))
            {
                var errorMessage = failedPages == pageCount
                    ? "Không thể trích xuất văn bản từ bất kỳ trang nào của PDF. File có thể là ảnh scan chất lượng thấp hoặc không chứa văn bản."
                    : "Không thể trích xuất đủ văn bản từ PDF. Vui lòng đảm bảo PDF chứa văn bản rõ ràng hoặc ảnh scan chất lượng cao.";

                _logger.LogError("PDF extraction failed: {Message}. Successful: {Success}, Failed: {Failed}",
                    errorMessage, successfulPages, failedPages);

                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation("✅ PDF OCR extraction completed successfully. Total {Length} characters from {PageCount} pages",
                combinedText.Length, extractedTexts.Count);

            return combinedText;
        }
        catch (InvalidOperationException)
        {
            // Re-throw our custom error messages
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Critical error extracting text from PDF file: {Message}", ex.Message);
            throw new InvalidOperationException($"Lỗi khi xử lý file PDF: {ex.Message}. Vui lòng thử lại với file PDF khác hoặc chuyển đổi sang định dạng ảnh (JPG/PNG).", ex);
        }
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
            
            // Verify Tesseract is properly configured before processing
            VerifyTesseractSetup();

            // Save file to temporary location
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return ExtractTextFromImagePath(tempFilePath);
        }
        catch (InvalidOperationException)
        {
            // Re-throw configuration errors as-is
            throw;
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
        try
        {
            // Debug logging for Tesseract configuration
            _logger.LogInformation("🔍 Tesseract Debug Info (Image OCR):");
            _logger.LogInformation("  - DataPath: {DataPath}", _tesseractDataPath);
            _logger.LogInformation("  - Language: {Language}", _tesseractLanguage);
            _logger.LogInformation("  - TESSDATA_PREFIX env: {TessDataPrefix}", Environment.GetEnvironmentVariable("TESSDATA_PREFIX"));
            _logger.LogInformation("  - Directory exists: {Exists}", Directory.Exists(_tesseractDataPath));
            
            if (Directory.Exists(_tesseractDataPath))
            {
                var files = Directory.GetFiles(_tesseractDataPath, "*.traineddata");
                _logger.LogInformation("  - Found {Count} traineddata files: {Files}", 
                    files.Length, 
                    string.Join(", ", files.Select(f => Path.GetFileName(f))));
            }
            else
            {
                _logger.LogError("  - ❌ Tessdata directory does NOT exist!");
            }

            // Perform OCR using Tesseract
            using var engine = new TesseractEngine(_tesseractDataPath, _tesseractLanguage, EngineMode.Default);

            // Set page segmentation mode for better OCR results
            engine.DefaultPageSegMode = PageSegMode.Auto;

            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);

            var extractedText = page.GetText();

            _logger.LogInformation("Image OCR extraction completed. Extracted {Length} characters", extractedText?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("OCR extracted empty text from image");
                throw new InvalidOperationException("Không thể trích xuất văn bản từ ảnh. Vui lòng đảm bảo:\n" +
                    "- Ảnh chứa văn bản rõ ràng và dễ đọc\n" +
                    "- Ảnh có độ phân giải đủ cao (tối thiểu 300 DPI)\n" +
                    "- Văn bản không bị mờ hoặc bị che khuất\n" +
                    "- Thử chụp lại ảnh với ánh sáng tốt hơn");
            }

            return extractedText;
        }
        catch (InvalidOperationException)
        {
            // Re-throw our custom error messages
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OCR processing: {Message}", ex.Message);
            throw new InvalidOperationException($"Lỗi khi xử lý ảnh: {ex.Message}. Vui lòng thử lại với ảnh chất lượng cao hơn.", ex);
        }
    }

    private async Task<GeminiLabAnalysis> AnalyzeWithGroqAsync(string extractedText)
    {
        var prompt = BuildAnalysisPrompt(extractedText);
        var groqResponse = await CallGroqApiAsync(prompt);
        return await ParseGeminiResponseAsync(groqResponse);
    }

    private string BuildAnalysisPrompt(string extractedText)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("Bạn là bác sĩ AI chuyên phân tích kết quả xét nghiệm. Phân tích kết quả sau:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**KẾT QUẢ XÉT NGHIỆM:**");
        promptBuilder.AppendLine(extractedText);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**YÊU CẦU QUAN TRỌNG:**");
        promptBuilder.AppendLine("⚠️ TẤT CẢ NỘI DUNG PHẢI TRẢ VỀ BẰNG TIẾNG VIỆT (tên chỉ số, giải thích, lời khuyên, chẩn đoán, chuyên khoa, disclaimer)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("1. Phân loại các chỉ số thành bình thường và bất thường");
        promptBuilder.AppendLine("2. Với mỗi chỉ số bất thường:");
        promptBuilder.AppendLine("   - Giải thích ngắn gọn tại sao bất thường (BẰNG TIẾNG VIỆT)");
        promptBuilder.AppendLine("   - Đưa ra lời khuyên cụ thể (BẰNG TIẾNG VIỆT)");
        promptBuilder.AppendLine("   - Chẩn đoán bệnh có thể (BẰNG TIẾNG VIỆT)");
        promptBuilder.AppendLine("   - **BẮT BUỘC**: Đề xuất 1 chuyên khoa phù hợp với confidence (0.0-1.0), urgency (NORMAL/URGENT), và reasons (BẰNG TIẾNG VIỆT)");
        promptBuilder.AppendLine("3. Đề xuất 1-3 chuyên khoa tổng quát (TÊN CHUYÊN KHOA BẰNG TIẾNG VIỆT)");
        promptBuilder.AppendLine("4. Tạo disclaimer ngắn gọn (BẰNG TIẾNG VIỆT)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**TRẢ VỀ JSON (ngắn gọn, chỉ các chỉ số quan trọng, TẤT CẢ BẰNG TIẾNG VIỆT):**");
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
        promptBuilder.AppendLine("  \"disclaimer\": \"Kết quả này chỉ mang tính chất tham khảo. Bạn nên tham khảo ý kiến bác sĩ chuyên khoa để chẩn đoán và điều trị chính xác.\"");
        promptBuilder.AppendLine("}");

        return promptBuilder.ToString();
    }

    private async Task<string> CallGroqApiAsync(string prompt)
    {
        return await _groqApiHelper.CallGroqApiWithDefaultsAsync(
            prompt,
            _serviceConfig);
    }

    private async Task<GeminiLabAnalysis> ParseGeminiResponseAsync(string groqResponse)
    {
        try
        {
            var root = ExtractRootJsonElement(groqResponse);
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

    private static JsonElement ExtractRootJsonElement(string responseText)
    {
        var jsonStart = responseText.IndexOf('{');
        var jsonEnd = responseText.LastIndexOf('}');

        if (jsonStart == -1 || jsonEnd == -1)
        {
            throw new InvalidOperationException("No JSON found in AI response");
        }

        var jsonText = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
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
            var aiMessage = BuildAnalysisMessage(response);

            var suggestions = new
            {
                doctors = response.RecommendedDoctors,
                hospitals = response.RecommendedHospitals,
                imageUrl
            };

            await _sessionService.SaveConversationHistoryAsync(
                sessionId,
                userMessage,
                aiMessage,
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

    private static string BuildAnalysisMessage(LabResultAnalysisResponse response)
    {
        var aiMessage = new StringBuilder();
        aiMessage.AppendLine("Kết quả phân tích xét nghiệm:");
        aiMessage.AppendLine();

        AppendNormalIndicators(aiMessage, response.NormalIndicators);
        AppendAbnormalIndicators(aiMessage, response.AbnormalIndicators);
        AppendDisclaimer(aiMessage, response.Disclaimer);

        return aiMessage.ToString();
    }

    private static void AppendNormalIndicators(StringBuilder aiMessage, List<LabIndicator> normalIndicators)
    {
        if (normalIndicators.Count == 0)
        {
            return;
        }

        aiMessage.AppendLine("Các chỉ số bình thường:");
        foreach (var indicator in normalIndicators)
        {
            aiMessage.AppendLine($"- {indicator.Name}: {indicator.Value} {indicator.Unit} (Tham chiếu: {indicator.ReferenceRange})");
        }
        aiMessage.AppendLine();
    }

    private static void AppendAbnormalIndicators(StringBuilder aiMessage, List<AbnormalLabIndicator> abnormalIndicators)
    {
        if (abnormalIndicators.Count == 0)
        {
            return;
        }

        aiMessage.AppendLine("Các chỉ số bất thường:");
        foreach (var indicator in abnormalIndicators)
        {
            AppendAbnormalIndicatorDetails(aiMessage, indicator);
        }
    }

    private static void AppendAbnormalIndicatorDetails(StringBuilder aiMessage, AbnormalLabIndicator indicator)
    {
        aiMessage.AppendLine($"- {indicator.Name}: {indicator.Value} {indicator.Unit} (Tham chiếu: {indicator.ReferenceRange})");
        aiMessage.AppendLine($"- Giải thích: {indicator.Explanation}");
        aiMessage.AppendLine($"- Lời khuyên: {indicator.Advice}");

        if (!string.IsNullOrEmpty(indicator.PossibleDiagnosis))
        {
            aiMessage.AppendLine($"- Chẩn đoán có thể: {indicator.PossibleDiagnosis}");
            AppendRecommendedSpecialties(aiMessage, indicator.RecommendedSpecialties);
        }

        aiMessage.AppendLine();
    }

    private static void AppendRecommendedSpecialties(StringBuilder aiMessage, List<SpecialtyMatch>? recommendedSpecialties)
    {
        if (recommendedSpecialties == null || recommendedSpecialties.Count == 0)
        {
            return;
        }

        var specialtyNames = string.Join(", ", recommendedSpecialties.Select(s => s.SpecialtyName));
        aiMessage.AppendLine($"- Chuyên khoa phù hợp: {specialtyNames}");
    }

    private static void AppendDisclaimer(StringBuilder aiMessage, string? disclaimer)
    {
        if (string.IsNullOrEmpty(disclaimer))
        {
            return;
        }

        aiMessage.AppendLine();
        aiMessage.AppendLine(disclaimer);
    }

    #region Cache Methods

    /// <summary>
    /// Try to get cached analysis using 3-tier lookup (exact text → exact keywords → fuzzy)
    /// </summary>
    private async Task<GeminiLabAnalysis?> TryGetCachedAnalysisAsync(string extractedText)
    {
        try
        {
            // Tier 0: Exact normalized text match (fastest, most accurate)
            var normalizedText = _keywordExtractor.NormalizeText(extractedText);
            if (!string.IsNullOrWhiteSpace(normalizedText))
            {
                var exactTextMatch = await _cacheService.FindExactTextMatchAsync(normalizedText);

                if (exactTextMatch != null)
                {
                    await _cacheService.IncrementUsageAsync(exactTextMatch.Id);
                    _logger.LogInformation(
                        "✅ Tier 0 Cache Hit: Exact text match");
                    return CreateAnalysisFromCache(exactTextMatch);
                }
            }

            // Extract keywords
            var keywords = _keywordExtractor.ExtractKeywords(extractedText);

            if (string.IsNullOrEmpty(keywords))
            {
                _logger.LogDebug("No keywords extracted, skipping cache lookup");
                return null;
            }

            _logger.LogDebug(
                "Cache lookup: Text length={Length}, Keywords='{Keywords}'",
                extractedText.Length,
                keywords);

            // Tier 1: Exact keywords match
            var exactMatch = await _cacheService.FindExactMatchAsync(keywords);

            if (exactMatch != null)
            {
                await _cacheService.IncrementUsageAsync(exactMatch.Id);
                return CreateAnalysisFromCache(exactMatch);
            }

            // Tier 2: Fuzzy keywords match
            var fuzzyMatch = await _cacheService.FindFuzzyMatchAsync(
                keywords,
                threshold: 0.75); // 75% similarity

            if (fuzzyMatch != null)
            {
                await _cacheService.IncrementUsageAsync(fuzzyMatch.Id);
                return CreateAnalysisFromCache(fuzzyMatch);
            }

            // Cache miss
            _logger.LogInformation(
                "❌ Cache miss for Keywords='{Keywords}' → Will call Groq",
                keywords);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache lookup, falling back to Groq");
            return null;
        }
    }

    /// <summary>
    /// Create analysis from cached entity
    /// </summary>
    private GeminiLabAnalysis CreateAnalysisFromCache(
        Models.Entities.LabResultAbnormalIndicatorCacheEntity cached)
    {
        try
        {
            var analysis = new GeminiLabAnalysis
            {
                NormalIndicators = new List<LabIndicator>(),
                AbnormalIndicators = new List<AbnormalLabIndicator>(),
                Specialties = new List<string>()
            };

            // Deserialize normal indicators
            if (!string.IsNullOrEmpty(cached.NormalIndicatorsJson))
            {
                analysis.NormalIndicators = JsonSerializer.Deserialize<List<LabIndicator>>(
                    cached.NormalIndicatorsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<LabIndicator>();
            }

            // Deserialize abnormal indicators
            if (!string.IsNullOrEmpty(cached.AbnormalIndicatorsJson))
            {
                analysis.AbnormalIndicators = JsonSerializer.Deserialize<List<AbnormalLabIndicator>>(
                    cached.AbnormalIndicatorsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<AbnormalLabIndicator>();
            }

            // Deserialize specialties
            if (!string.IsNullOrEmpty(cached.SpecialtiesJson))
            {
                analysis.Specialties = JsonSerializer.Deserialize<List<string>>(
                    cached.SpecialtiesJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<string>();
            }

            analysis.Disclaimer = cached.Disclaimer;

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing cached analysis, falling back to Groq");
            return null!;
        }
    }

    /// <summary>
    /// Save analysis to cache (only if not already exists)
    /// </summary>
    private async Task SaveAnalysisToCacheAsync(
        string extractedText,
        GeminiLabAnalysis analysis)
    {
        try
        {
            // Extract components
            var keywords = _keywordExtractor.ExtractKeywords(extractedText);
            var normalizedText = _keywordExtractor.NormalizeText(extractedText);

            // Check if already exists in cache
            var existingCache = !string.IsNullOrWhiteSpace(normalizedText)
                ? await _cacheService.FindExactTextMatchAsync(normalizedText)
                : null;

            if (existingCache == null)
            {
                existingCache = await _cacheService.FindExactMatchAsync(keywords);
            }

            if (existingCache != null)
            {
                _logger.LogDebug(
                    "⏭️ Skipping cache save: Analysis already exists for Keywords='{Keywords}'",
                    keywords);
                return;
            }

            // Serialize indicators
            var abnormalIndicatorsJson = JsonSerializer.Serialize(
                analysis.AbnormalIndicators,
                Utf8JsonOptions);

            var normalIndicatorsJson = JsonSerializer.Serialize(
                analysis.NormalIndicators,
                Utf8JsonOptions);

            var specialtiesJson = JsonSerializer.Serialize(
                analysis.Specialties,
                Utf8JsonOptions);

            // Save to cache
            await _cacheService.SaveAnalysisAsync(
                normalizedKeywords: keywords,
                normalizedText: normalizedText,
                abnormalIndicatorsJson: abnormalIndicatorsJson,
                normalIndicatorsJson: normalIndicatorsJson,
                specialtiesJson: specialtiesJson,
                disclaimer: analysis.Disclaimer,
                createdBy: "GROQ");

            _logger.LogInformation(
                "💾 Saved to cache: Keywords='{Keywords}', {AbnormalCount} abnormal indicators",
                keywords,
                analysis.AbnormalIndicators.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save analysis to cache (non-critical)");
        }
    }

    #endregion
}

internal class GeminiLabAnalysis
{
    public List<LabIndicator> NormalIndicators { get; set; } = new();
    public List<AbnormalLabIndicator> AbnormalIndicators { get; set; } = new();
    public List<string> Specialties { get; set; } = new();
    public string? Disclaimer { get; set; }
}

