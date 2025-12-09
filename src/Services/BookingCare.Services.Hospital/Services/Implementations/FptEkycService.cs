using System.Net.Http.Headers;
using System.Text.Json;
using BookingCare.Services.Hospital.Enums;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Hospital.Services.Implementations;

/// <summary>
/// Implementation of eKYC service using FPT.AI Identity APIs
/// </summary>
public class FptEkycService : BaseService, IEkycService
{
    private readonly HttpClient _httpClient;

    private readonly string _apiKey;
    private readonly string _ocrEndpoint;
    private readonly string _faceMatchEndpoint;
    private readonly string _livenessEndpoint;
    private readonly double _faceMatchThreshold;
    private readonly double _livenessThreshold;
    private readonly bool _skipLivenessCheck;

    public FptEkycService(
        HttpClient httpClient,
        ILogger<FptEkycService> logger,
        IConfiguration configuration) : base(logger)
    {
        _httpClient = httpClient;

        _apiKey = configuration["FptAI:ApiKey"]
            ?? throw new InvalidOperationException("FPT.AI API Key is not configured");
        _ocrEndpoint = configuration["FptAI:Endpoints:Ocr"]
            ?? "https://api.fpt.ai/vision/idr/vnm";
        _faceMatchEndpoint = configuration["FptAI:Endpoints:FaceMatch"]
            ?? "https://api.fpt.ai/dmp/checkface/v1";
        _livenessEndpoint = configuration["FptAI:Endpoints:Liveness"]
            ?? "https://api.fpt.ai/dmp/liveness/v3";
        _faceMatchThreshold = configuration.GetValue<double>("FptAI:Thresholds:FaceMatch", 80.0);
        _livenessThreshold = configuration.GetValue<double>("FptAI:Thresholds:Liveness", 80.0);
        _skipLivenessCheck = configuration.GetValue<bool>("FptAI:SkipLivenessCheck", false);
    }

    public async Task<EkycOcrResponseDto> ProcessIdCardOcrAsync(EkycOcrRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Starting OCR processing for ID card");

            // Process front image
            var frontResult = await CallOcrApiAsync(request.FrontImage);
            if (!frontResult.Success)
            {
                return frontResult;
            }

            // Process back image
            var backResult = await CallOcrApiAsync(request.BackImage);
            if (!backResult.Success)
            {
                return new EkycOcrResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Lỗi xử lý mặt sau CMND/CCCD: {backResult.ErrorMessage}",
                    ErrorCode = backResult.ErrorCode
                };
            }

            // Merge results from front and back
            frontResult.IssueDate = backResult.IssueDate;
            frontResult.IssuedBy = backResult.IssuedBy;

            LogInfo("OCR processing completed successfully for ID: {IdNumber}", null, frontResult.IdNumber ?? "N/A");

            return frontResult;
        }, "ProcessIdCardOcr");
    }

    public async Task<EkycFaceMatchResponseDto> VerifyFaceMatchAsync(EkycFaceMatchRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Starting face matching verification");

            using var content = new MultipartFormDataContent();

            // Add first image (selfie) - use "file[]" as per FPT.AI API docs
            var selfieStream = new MemoryStream();
            await request.SelfieImage.CopyToAsync(selfieStream);
            selfieStream.Position = 0;
            var selfieContent = new StreamContent(selfieStream);
            selfieContent.Headers.ContentType = new MediaTypeHeaderValue(
                request.SelfieImage.ContentType ?? "image/jpeg");
            content.Add(selfieContent, "file[]", "selfie.jpg");

            // Add second image (ID card front) - use "file[]" as per FPT.AI API docs
            var idCardStream = new MemoryStream();
            await request.IdCardFrontImage.CopyToAsync(idCardStream);
            idCardStream.Position = 0;
            var idCardContent = new StreamContent(idCardStream);
            idCardContent.Headers.ContentType = new MediaTypeHeaderValue(
                request.IdCardFrontImage.ContentType ?? "image/jpeg");
            content.Add(idCardContent, "file[]", "idcard.jpg");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, _faceMatchEndpoint);
            requestMessage.Headers.Add("api-key", _apiKey);
            requestMessage.Content = content;

            var response = await _httpClient.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            LogDebug("Face match API response: {Response}", null, responseContent);

            // Try to parse response
            using var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            // Check for error - handle both string and number code
            var errorResult = CheckApiResponseCode(root, "FACE_MATCH", "Không thể xác thực khuôn mặt");
            if (errorResult != null)
            {
                return new EkycFaceMatchResponseDto
                {
                    Success = false,
                    ErrorMessage = errorResult.Value.message,
                    ErrorCode = errorResult.Value.errorCode
                };
            }

            // Parse similarity from data
            var (similarity, isMatch) = ParseFaceMatchData(root);

            // Also check if similarity meets threshold
            if (similarity >= _faceMatchThreshold)
                isMatch = true;

            LogInfo("Face matching completed. IsMatch: {IsMatch}, Similarity: {Similarity}%", null, isMatch, similarity);

            return new EkycFaceMatchResponseDto
            {
                Success = true,
                IsMatch = isMatch,
                Similarity = similarity,
                Threshold = _faceMatchThreshold,
                Message = isMatch
                    ? "Khuôn mặt khớp với ảnh trên CMND/CCCD"
                    : "Khuôn mặt không khớp với ảnh trên CMND/CCCD"
            };
        }, "VerifyFaceMatch");
    }

    public async Task<EkycLivenessResponseDto> CheckLivenessAsync(EkycLivenessRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Starting liveness detection");

            // FPT.AI Liveness API requires: video (video.mp4) + cmnd (face.jpg)
            if (request.Video == null || request.IdCardImage == null)
            {
                LogWarning("Liveness check requires video and ID card image");
                return new EkycLivenessResponseDto
                {
                    Success = false,
                    ErrorMessage = "Liveness check requires video and ID card image",
                    ErrorCode = "LIVENESS_MISSING_FILES"
                };
            }

            using var content = new MultipartFormDataContent();

            // Add video file
            var videoStream = new MemoryStream();
            await request.Video.CopyToAsync(videoStream);
            videoStream.Position = 0;
            var videoContent = new StreamContent(videoStream);
            videoContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(videoContent, "video", "video.mp4");

            // Add ID card image (cmnd)
            var cmndStream = new MemoryStream();
            await request.IdCardImage.CopyToAsync(cmndStream);
            cmndStream.Position = 0;
            var cmndContent = new StreamContent(cmndStream);
            cmndContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(cmndContent, "cmnd", "face.jpg");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, _livenessEndpoint);
            requestMessage.Headers.Add("api-key", _apiKey);
            requestMessage.Content = content;

            var response = await _httpClient.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            LogInfo("Liveness API response: {Response}", null, responseContent);

            // Try to parse response
            using var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            // FPT.AI Liveness v3 response format:
            // { "code": "303", "message": "...", 
            //   "liveness": { "code": "200", "is_live": "true", "spoof_prob": "0.38" },
            //   "face_match": { "code": "303", "isMatch": "false", "similarity": "59.99" } }

            // Parse liveness object
            bool isLive = false;
            double livenessScore = 0;

            if (root.TryGetProperty("liveness", out var livenessObj))
            {
                var livenessData = ParseLivenessData(livenessObj);
                if (livenessData.errorMessage != null)
                {
                    return new EkycLivenessResponseDto
                    {
                        Success = false,
                        ErrorMessage = livenessData.errorMessage,
                        ErrorCode = livenessData.errorCode
                    };
                }

                isLive = livenessData.isLive;
                livenessScore = livenessData.livenessScore;

                LogInfo("Liveness parsed - IsLive: {IsLive}, SpoofProb: {SpoofProb}, Score: {Score}%",
                    null, isLive, livenessData.spoofProb, livenessScore);
            }

            // Parse face_match object (included in Liveness v3 response)
            var (isFaceMatch, faceMatchSimilarity) = ParseFaceMatchFromLiveness(root);

            LogInfo("Face match from liveness - IsMatch: {IsMatch}, Similarity: {Similarity}%",
                null, isFaceMatch, faceMatchSimilarity);

            // If FPT.AI says is_live=true, trust it
            if (!isLive && livenessScore >= _livenessThreshold)
                isLive = true;

            // Determine overall success and message
            var (livenessSuccess, message) = DetermineLivenessResult(isLive, isFaceMatch, faceMatchSimilarity);

            LogInfo("Liveness detection completed. IsLive: {IsLive}, IsFaceMatch: {IsFaceMatch}, Score: {Score}%",
                null, isLive, isFaceMatch, livenessScore);

            return new EkycLivenessResponseDto
            {
                Success = true, // API call succeeded
                IsLive = livenessSuccess, // Combined liveness + face match result
                LivenessScore = livenessScore,
                Threshold = _livenessThreshold,
                Message = message
            };
        }, "CheckLiveness");
    }

    public async Task<EkycVerificationResponseDto> VerifyIdentityAsync(EkycVerifyRequestDto request)
    {
        var sessionId = Guid.NewGuid().ToString();

        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Starting complete eKYC verification. SessionId: {SessionId}", null, sessionId);

            var result = new EkycVerificationResponseDto
            {
                SessionId = sessionId,
                Status = EkycStatus.IN_PROGRESS
            };

            // Step 1: OCR Processing
            var ocrError = await ProcessOcrStepAsync(request, result);
            if (ocrError != null) return ocrError;

            // Step 2: Face Matching
            var faceMatchError = await ProcessFaceMatchStepAsync(request, result);
            if (faceMatchError != null) return faceMatchError;

            // Step 3: Liveness Detection
            var livenessError = await ProcessLivenessStepAsync(request, result);
            if (livenessError != null) return livenessError;

            // All verifications passed
            LogInfo("Step 4: Verification complete (images not stored for privacy)");
            return FinalizeVerificationSuccess(result, sessionId);
        }, "VerifyIdentity");
    }

    private async Task<EkycVerificationResponseDto?> ProcessOcrStepAsync(
        EkycVerifyRequestDto request, EkycVerificationResponseDto result)
    {
        LogInfo("Step 1: Processing OCR...");
        var ocrResult = await ProcessIdCardOcrAsync(new EkycOcrRequestDto
        {
            FrontImage = request.IdCardFrontImage,
            BackImage = request.IdCardBackImage
        });
        result.OcrResult = ocrResult;

        if (!ocrResult.Success)
            return SetVerificationFailed(result, $"OCR thất bại: {ocrResult.ErrorMessage}");

        return null;
    }

    private async Task<EkycVerificationResponseDto?> ProcessFaceMatchStepAsync(
        EkycVerifyRequestDto request, EkycVerificationResponseDto result)
    {
        LogInfo("Step 2: Verifying face match...");
        var faceMatchResult = await VerifyFaceMatchAsync(new EkycFaceMatchRequestDto
        {
            SelfieImage = request.SelfieImage,
            IdCardFrontImage = request.IdCardFrontImage
        });
        result.FaceMatchResult = faceMatchResult;

        if (!faceMatchResult.Success)
            return SetVerificationFailed(result, $"Face matching thất bại: {faceMatchResult.ErrorMessage}");

        if (!faceMatchResult.IsMatch)
            return SetVerificationFailed(result, "Khuôn mặt không khớp với ảnh trên CMND/CCCD");

        return null;
    }

    private async Task<EkycVerificationResponseDto?> ProcessLivenessStepAsync(
        EkycVerifyRequestDto request, EkycVerificationResponseDto result)
    {
        var hasLivenessVideo = request.LivenessVideo != null;

        if (_skipLivenessCheck && !hasLivenessVideo)
        {
            LogInfo("Step 3: Skipping liveness check (no video provided and configured to skip)");
            result.LivenessResult = CreateSkippedLivenessResult();
            return null;
        }

        if (!hasLivenessVideo)
            return SetVerificationFailed(result, "Vui lòng quay video để xác thực người thật");

        LogInfo("Step 3: Checking liveness with video...");
        var livenessResult = await CheckLivenessAsync(new EkycLivenessRequestDto
        {
            Video = request.LivenessVideo,
            IdCardImage = request.IdCardFrontImage
        });
        result.LivenessResult = livenessResult;

        if (!livenessResult.Success)
            return SetVerificationFailed(result, $"Liveness check thất bại: {livenessResult.ErrorMessage}");

        if (!livenessResult.IsLive)
            return SetVerificationFailed(result, "Không phát hiện người thật");

        return null;
    }

    private static EkycVerificationResponseDto SetVerificationFailed(
        EkycVerificationResponseDto result, string errorMessage)
    {
        result.Success = false;
        result.Status = EkycStatus.FAILED;
        result.ErrorMessage = errorMessage;
        return result;
    }

    private EkycLivenessResponseDto CreateSkippedLivenessResult() => new()
    {
        Success = true,
        IsLive = true,
        LivenessScore = 100,
        Threshold = _livenessThreshold,
        Message = "Liveness check skipped (face match passed)"
    };

    private EkycVerificationResponseDto FinalizeVerificationSuccess(
        EkycVerificationResponseDto result, string sessionId)
    {
        result.Success = true;
        result.Status = EkycStatus.VERIFIED;
        result.IsVerified = true;
        result.VerifiedAt = DateTime.UtcNow;
        LogInfo("eKYC verification completed successfully. SessionId: {SessionId}", null, sessionId);
        return result;
    }

    private async Task<EkycOcrResponseDto> CallOcrApiAsync(IFormFile image)
    {
        using var content = new MultipartFormDataContent();

        var imageContent = new StreamContent(image.OpenReadStream());
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(
            image.ContentType ?? "image/jpeg");
        content.Add(imageContent, "image", image.FileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, _ocrEndpoint);
        request.Headers.Add("api-key", _apiKey);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        LogDebug("OCR API response: {Response}", null, responseContent);

        var apiResponse = JsonSerializer.Deserialize<FptOcrApiResponse>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (apiResponse == null || apiResponse.ErrorCode != 0)
        {
            return new EkycOcrResponseDto
            {
                Success = false,
                ErrorMessage = apiResponse?.ErrorMessage ?? "Không thể đọc thông tin từ CMND/CCCD",
                ErrorCode = $"OCR_{apiResponse?.ErrorCode}"
            };
        }

        var data = apiResponse.Data?.FirstOrDefault();
        if (data == null)
        {
            return new EkycOcrResponseDto
            {
                Success = false,
                ErrorMessage = "Không tìm thấy thông tin trên CMND/CCCD",
                ErrorCode = "OCR_NO_DATA"
            };
        }

        var fieldConfidences = new Dictionary<string, double>();
        if (double.TryParse(data.Id_prob, out var idProb)) fieldConfidences["IdNumber"] = idProb;
        if (double.TryParse(data.Name_prob, out var nameProb)) fieldConfidences["FullName"] = nameProb;
        if (double.TryParse(data.Dob_prob, out var dobProb)) fieldConfidences["DateOfBirth"] = dobProb;

        return new EkycOcrResponseDto
        {
            Success = true,
            IdNumber = data.Id,
            FullName = data.Name,
            DateOfBirth = data.Dob,
            Gender = data.Sex,
            Nationality = data.Nationality,
            PlaceOfOrigin = data.Home,
            PlaceOfResidence = data.Address,
            ExpiryDate = data.Doe,
            IssueDate = data.Issue_date,
            IssuedBy = data.Issue_loc,
            DocumentType = data.Type_new ?? data.Type,
            FieldConfidences = fieldConfidences,
            OverallConfidence = fieldConfidences.Values.Any()
                ? fieldConfidences.Values.Average()
                : null
        };
    }

    #region Helper Methods for JSON Parsing

    /// <summary>
    /// Parse integer from JSON element (handles both string and number types)
    /// </summary>
    private static int ParseIntFromJson(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.GetInt32();
        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var result))
            return result;
        return 0;
    }

    /// <summary>
    /// Parse double from JSON element (handles both string and number types)
    /// </summary>
    private static double ParseDoubleFromJson(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.GetDouble();
        if (element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), out var result))
            return result;
        return 0;
    }

    /// <summary>
    /// Parse boolean from JSON element (handles string "true"/"false" and actual boolean)
    /// </summary>
    private static bool ParseBoolFromJson(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.True)
            return true;
        if (element.ValueKind == JsonValueKind.False)
            return false;
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString()?.ToLower() == "true";
        return false;
    }

    /// <summary>
    /// Check API response code and return error info if not successful
    /// </summary>
    private static (string message, string errorCode)? CheckApiResponseCode(
        JsonElement root, string errorPrefix, string defaultMessage)
    {
        if (!root.TryGetProperty("code", out var codeElement))
            return null;

        var code = ParseIntFromJson(codeElement);
        if (code == 200)
            return null;

        var message = root.TryGetProperty("message", out var msgElement)
            ? msgElement.GetString() ?? defaultMessage
            : defaultMessage;

        return (message, $"{errorPrefix}_{code}");
    }

    /// <summary>
    /// Parse face match data from API response
    /// </summary>
    private static (double similarity, bool isMatch) ParseFaceMatchData(JsonElement root)
    {
        double similarity = 0;
        bool isMatch = false;

        if (!root.TryGetProperty("data", out var dataElement))
            return (similarity, isMatch);

        if (dataElement.TryGetProperty("similarity", out var simElement))
            similarity = ParseDoubleFromJson(simElement);

        if (dataElement.TryGetProperty("isMatch", out var matchElement))
            isMatch = ParseBoolFromJson(matchElement);

        return (similarity, isMatch);
    }

    /// <summary>
    /// Parse liveness data from API response
    /// </summary>
    private static (bool isLive, double livenessScore, double spoofProb, string? errorMessage, string? errorCode) ParseLivenessData(JsonElement livenessObj)
    {
        // Check liveness-specific code
        if (livenessObj.TryGetProperty("code", out var lCodeElement))
        {
            var livenessCode = ParseIntFromJson(lCodeElement);
            if (livenessCode != 200)
            {
                var lMessage = livenessObj.TryGetProperty("message", out var lMsgElement)
                    ? lMsgElement.GetString() ?? "Liveness check failed"
                    : "Liveness check failed";
                return (false, 0, 0, lMessage, $"LIVENESS_{livenessCode}");
            }
        }

        // Get is_live field
        bool isLive = false;
        if (livenessObj.TryGetProperty("is_live", out var isLiveElement))
            isLive = ParseBoolFromJson(isLiveElement);

        // Get spoof_prob (lower is better - means less likely to be spoofed)
        double spoofProb = 0;
        if (livenessObj.TryGetProperty("spoof_prob", out var spoofElement))
            spoofProb = ParseDoubleFromJson(spoofElement);

        // Calculate liveness score: 100 - (spoof_prob * 100)
        var livenessScore = (1 - spoofProb) * 100;

        return (isLive, livenessScore, spoofProb, null, null);
    }

    /// <summary>
    /// Parse face match data from liveness response
    /// </summary>
    private (bool isFaceMatch, double similarity) ParseFaceMatchFromLiveness(JsonElement root)
    {
        double similarity = 0;
        bool isFaceMatch = false;

        if (!root.TryGetProperty("face_match", out var faceMatchObj))
            return (isFaceMatch, similarity);

        if (faceMatchObj.TryGetProperty("similarity", out var simElement))
            similarity = ParseDoubleFromJson(simElement);

        if (faceMatchObj.TryGetProperty("isMatch", out var matchElement))
            isFaceMatch = ParseBoolFromJson(matchElement);

        // Also check threshold
        if (similarity >= _faceMatchThreshold)
            isFaceMatch = true;

        return (isFaceMatch, similarity);
    }

    /// <summary>
    /// Determine liveness result message based on liveness and face match status
    /// </summary>
    private (bool success, string message) DetermineLivenessResult(bool isLive, bool isFaceMatch, double faceMatchSimilarity)
    {
        if (!isLive)
            return (false, "Không phát hiện người thật. Vui lòng thử lại.");

        if (!isFaceMatch)
            return (false, $"Khuôn mặt trong video không khớp với CMND/CCCD (độ khớp: {faceMatchSimilarity:F1}%, yêu cầu: {_faceMatchThreshold}%)");

        return (true, "Xác thực người thật thành công");
    }

    #endregion

}
