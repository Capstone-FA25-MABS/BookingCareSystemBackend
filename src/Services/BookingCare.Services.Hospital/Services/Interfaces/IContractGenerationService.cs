using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

/// <summary>
/// Service interface for generating partnership contracts
/// </summary>
public interface IContractGenerationService
{
    /// <summary>
    /// Generate a partnership contract PDF for a hospital registration
    /// </summary>
    /// <param name="registrationId">Hospital registration ID</param>
    /// <param name="adminId">Admin ID who is generating the contract</param>
    /// <returns>Generated contract file URL and metadata</returns>
    Task<GenerateContractResponseDto> GenerateContractAsync(Guid registrationId, string adminId);

    /// <summary>
    /// Generate contract PDF from contract data
    /// </summary>
    /// <param name="contractData">Contract data</param>
    /// <param name="hospitalSignatureBase64">Optional hospital signature in base64 format</param>
    /// <param name="hospitalSignedAt">Optional hospital signed date</param>
    /// <param name="adminSignatureBase64Override">Optional admin signature override to avoid re-downloading</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> GenerateContractPdfAsync(ContractDataDto contractData, string hospitalSignatureBase64 = "", DateTime? hospitalSignedAt = null, string adminSignatureBase64Override = "");

    /// <summary>
    /// Add hospital signature to existing contract by regenerating with both signatures
    /// </summary>
    /// <param name="registrationId">Hospital registration ID</param>
    /// <param name="adminId">Admin ID who generated the original contract</param>
    /// <param name="hospitalSignatureUrl">Hospital signature image URL</param>
    /// <param name="signedAt">Timestamp when signed</param>
    /// <returns>Updated contract file URL with both signatures</returns>
    Task<string> AddHospitalSignatureToContractAsync(
        Guid registrationId,
        string hospitalSignatureUrl,
        DateTime signedAt);
}
