using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

/// <summary>
/// Service interface for contract signing operations
/// </summary>
public interface IContractSigningService
{
    /// <summary>
    /// Generate a secure token for contract signing
    /// </summary>
    /// <param name="registrationId">Hospital registration ID</param>
    /// <param name="expiryDays">Token expiry in days (default 7)</param>
    /// <returns>Generated token string</returns>
    Task<string> GenerateSigningTokenAsync(Guid registrationId, int expiryDays = 7);

    /// <summary>
    /// Validate a contract signing token
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>Validation result with contract info</returns>
    Task<ValidateTokenResponseDto> ValidateTokenAsync(string token);

    /// <summary>
    /// Send OTP to representative email for contract signing verification
    /// </summary>
    /// <param name="token">Contract signing token</param>
    /// <returns>Success status</returns>
    Task<bool> SendSigningOtpAsync(string token);

    /// <summary>
    /// Sign the contract with hospital signature
    /// </summary>
    /// <param name="request">Signing request with signature and OTP</param>
    /// <returns>Signing result</returns>
    Task<SignContractResponseDto> SignContractAsync(SignContractRequestDto request);

    /// <summary>
    /// Get contract signing link for a registration
    /// </summary>
    /// <param name="registrationId">Hospital registration ID</param>
    /// <returns>Full signing URL</returns>
    Task<string> GetSigningLinkAsync(Guid registrationId);
}
