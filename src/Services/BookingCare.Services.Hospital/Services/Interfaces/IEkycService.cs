using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

/// <summary>
/// Service interface for eKYC (Electronic Know Your Customer) operations
/// Integrates with FPT.AI Identity APIs
/// </summary>
public interface IEkycService
{
    /// <summary>
    /// Process ID card images using OCR to extract information
    /// </summary>
    /// <param name="request">Front and back images of ID card</param>
    /// <returns>Extracted information from ID card</returns>
    Task<EkycOcrResponseDto> ProcessIdCardOcrAsync(EkycOcrRequestDto request);

    /// <summary>
    /// Compare face in selfie with face on ID card
    /// </summary>
    /// <param name="request">Selfie and ID card front image</param>
    /// <returns>Face matching result with similarity score</returns>
    Task<EkycFaceMatchResponseDto> VerifyFaceMatchAsync(EkycFaceMatchRequestDto request);

    /// <summary>
    /// Check if the image is from a live person (anti-spoofing)
    /// </summary>
    /// <param name="request">Selfie image for liveness check</param>
    /// <returns>Liveness detection result</returns>
    Task<EkycLivenessResponseDto> CheckLivenessAsync(EkycLivenessRequestDto request);

    /// <summary>
    /// Perform complete eKYC verification (OCR + Face Match + Liveness)
    /// </summary>
    /// <param name="request">All required images for verification</param>
    /// <returns>Complete verification result</returns>
    Task<EkycVerificationResponseDto> VerifyIdentityAsync(EkycVerifyRequestDto request);
}
