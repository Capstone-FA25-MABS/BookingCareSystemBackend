using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Hospital.Controllers;

/// <summary>
/// Controller for eKYC (Electronic Know Your Customer) operations
/// Provides endpoints for identity verification using FPT.AI
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class EkycController : BaseApiController
{
    private readonly IEkycService _ekycService;
    private readonly ILogger<EkycController> _logger;

    public EkycController(IEkycService ekycService, ILogger<EkycController> logger)
    {
        _ekycService = ekycService;
        _logger = logger;
    }

    /// <summary>
    /// Process ID card images using OCR to extract information
    /// </summary>
    /// <param name="request">Front and back images of CMND/CCCD</param>
    /// <returns>Extracted information from ID card</returns>
    /// <remarks>
    /// Supported document types: CMND (9 digits), CCCD (12 digits)
    /// Supported image formats: JPG, JPEG, PNG
    /// Maximum file size: 10MB per image
    /// </remarks>
    [HttpPost("ocr")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EkycOcrResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20MB total
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    public async Task<IActionResult> ProcessIdCardOcr([FromForm] EkycOcrRequestDto request)
    {
        _logger.LogInformation("Processing OCR request for ID card");

        // Validate file types
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
        if (!allowedTypes.Contains(request.FrontImage.ContentType?.ToLower()))
        {
            return BadRequest("Ảnh mặt trước phải có định dạng JPG hoặc PNG");
        }
        if (!allowedTypes.Contains(request.BackImage.ContentType?.ToLower()))
        {
            return BadRequest("Ảnh mặt sau phải có định dạng JPG hoặc PNG");
        }

        var result = await _ekycService.ProcessIdCardOcrAsync(request);

        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage ?? "Không thể đọc thông tin CMND/CCCD");
        }

        return Success(result, "Đọc thông tin CMND/CCCD thành công");
    }

    /// <summary>
    /// Compare face in selfie with face on ID card
    /// </summary>
    /// <param name="request">Selfie image and ID card front image</param>
    /// <returns>Face matching result with similarity score</returns>
    /// <remarks>
    /// The similarity threshold is 80% by default.
    /// Ensure good lighting and clear face visibility for best results.
    /// </remarks>
    [HttpPost("face-match")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EkycFaceMatchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20MB total
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    public async Task<IActionResult> VerifyFaceMatch([FromForm] EkycFaceMatchRequestDto request)
    {
        _logger.LogInformation("Processing face match verification request");

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
        if (!allowedTypes.Contains(request.SelfieImage.ContentType?.ToLower()))
        {
            return BadRequest("Ảnh selfie phải có định dạng JPG hoặc PNG");
        }
        if (!allowedTypes.Contains(request.IdCardFrontImage.ContentType?.ToLower()))
        {
            return BadRequest("Ảnh CMND/CCCD phải có định dạng JPG hoặc PNG");
        }

        var result = await _ekycService.VerifyFaceMatchAsync(request);

        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage ?? "Không thể xác thực khuôn mặt");
        }

        return Success(result, result.IsMatch
            ? "Xác thực khuôn mặt thành công"
            : "Khuôn mặt không khớp");
    }

    /// <summary>
    /// Check if the image is from a live person (anti-spoofing)
    /// </summary>
    /// <param name="request">Selfie image for liveness check</param>
    /// <returns>Liveness detection result</returns>
    /// <remarks>
    /// This endpoint detects if the image is from a real person or a photo/video.
    /// The liveness threshold is 80% by default.
    /// </remarks>
    [HttpPost("liveness")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EkycLivenessResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> CheckLiveness([FromForm] EkycLivenessRequestDto request)
    {
        _logger.LogInformation("Processing liveness detection request");

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
        if (!allowedTypes.Contains(request.Image!.ContentType?.ToLower()))
        {
            return BadRequest("Ảnh phải có định dạng JPG hoặc PNG");
        }

        var result = await _ekycService.CheckLivenessAsync(request);

        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage ?? "Không thể kiểm tra liveness");
        }

        return Success(result, result.IsLive
            ? "Xác thực người thật thành công"
            : "Không phát hiện người thật");
    }

    /// <summary>
    /// Perform complete eKYC verification (OCR + Face Match + Liveness)
    /// </summary>
    /// <param name="request">All required images for complete verification</param>
    /// <returns>Complete verification result including all steps</returns>
    /// <remarks>
    /// This endpoint performs a complete identity verification:
    /// 1. OCR: Extract information from ID card
    /// 2. Face Match: Compare selfie with ID card photo
    /// 3. Liveness: Verify the selfie is from a real person
    /// 
    /// All three steps must pass for successful verification.
    /// </remarks>
    [HttpPost("verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EkycVerificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB total (includes video up to 50MB)
    [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
    public async Task<IActionResult> VerifyIdentity([FromForm] EkycVerifyRequestDto request)
    {
        _logger.LogInformation("Processing complete eKYC verification request");

        var allowedImageTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
        var allowedVideoTypes = new[] { "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo" };

        if (!allowedImageTypes.Contains(request.IdCardFrontImage.ContentType?.ToLower()))
        {
            return BadRequest("Ảnh mặt trước CMND/CCCD phải có định dạng JPG hoặc PNG");
        }
        if (!allowedImageTypes.Contains(request.IdCardBackImage.ContentType?.ToLower()))
        {
            return BadRequest("Ảnh mặt sau CMND/CCCD phải có định dạng JPG hoặc PNG");
        }
        if (!allowedImageTypes.Contains(request.SelfieImage.ContentType?.ToLower()))
        {
            return BadRequest("Ảnh selfie phải có định dạng JPG hoặc PNG");
        }
        if (request.LivenessVideo != null &&
            !allowedVideoTypes.Contains(request.LivenessVideo.ContentType?.ToLower()))
        {
            return BadRequest("Video phải có định dạng MP4, WebM, MOV hoặc AVI");
        }

        var result = await _ekycService.VerifyIdentityAsync(request);

        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage ?? "Xác thực danh tính thất bại");
        }

        return Success(result, "Xác thực danh tính thành công");
    }
}
