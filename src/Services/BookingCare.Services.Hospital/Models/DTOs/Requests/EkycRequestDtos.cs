using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

/// <summary>
/// Request DTO for OCR ID card processing
/// </summary>
public class EkycOcrRequestDto
{
    [Required(ErrorMessage = "Ảnh mặt trước CMND/CCCD là bắt buộc")]
    public IFormFile FrontImage { get; set; } = null!;

    [Required(ErrorMessage = "Ảnh mặt sau CMND/CCCD là bắt buộc")]
    public IFormFile BackImage { get; set; } = null!;
}

/// <summary>
/// Request DTO for face matching verification
/// </summary>
public class EkycFaceMatchRequestDto
{
    [Required(ErrorMessage = "Ảnh selfie là bắt buộc")]
    public IFormFile SelfieImage { get; set; } = null!;

    [Required(ErrorMessage = "Ảnh mặt trước CMND/CCCD là bắt buộc")]
    public IFormFile IdCardFrontImage { get; set; } = null!;
}

/// <summary>
/// Request DTO for liveness detection (requires video + ID card image per FPT.AI API)
/// </summary>
public class EkycLivenessRequestDto
{
    /// <summary>
    /// Selfie image (used as fallback or for cmnd field)
    /// </summary>
    public IFormFile? Image { get; set; }

    /// <summary>
    /// Video file for liveness detection (required by FPT.AI)
    /// </summary>
    public IFormFile? Video { get; set; }

    /// <summary>
    /// ID card front image (cmnd field in FPT.AI API)
    /// </summary>
    public IFormFile? IdCardImage { get; set; }
}

/// <summary>
/// Request DTO for complete eKYC verification (OCR + Face Match + Liveness)
/// </summary>
public class EkycVerifyRequestDto
{
    [Required(ErrorMessage = "Ảnh mặt trước CMND/CCCD là bắt buộc")]
    public IFormFile IdCardFrontImage { get; set; } = null!;

    [Required(ErrorMessage = "Ảnh mặt sau CMND/CCCD là bắt buộc")]
    public IFormFile IdCardBackImage { get; set; } = null!;

    [Required(ErrorMessage = "Ảnh selfie là bắt buộc")]
    public IFormFile SelfieImage { get; set; } = null!;

    /// <summary>
    /// Video for liveness detection (optional - if not provided, liveness check may be skipped)
    /// </summary>
    public IFormFile? LivenessVideo { get; set; }
}
