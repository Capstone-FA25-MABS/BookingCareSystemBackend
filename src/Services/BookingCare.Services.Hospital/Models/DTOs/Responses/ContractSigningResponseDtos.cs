namespace BookingCare.Services.Hospital.Models.DTOs.Responses;

/// <summary>
/// Response DTO after generating contract
/// </summary>
public class GenerateContractResponseDto
{
    public string ContractFileUrl { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public Guid AdminSignatureId { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Response DTO after generating contract for a registration
/// </summary>
public class GenerateContractForRegistrationResponseDto
{
    public Guid RegistrationId { get; set; }
    public string ContractFileUrl { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public string SigningLink { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime LinkExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO after validating token
/// </summary>
public class ValidateTokenResponseDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public ContractSigningInfoDto? ContractInfo { get; set; }
}

/// <summary>
/// Contract information for signing page
/// </summary>
public class ContractSigningInfoDto
{
    public Guid RegistrationId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string RepresentativeName { get; set; } = string.Empty;
    public string RepresentativeEmail { get; set; } = string.Empty;
    public string ContractDraftUrl { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}



/// <summary>
/// Response DTO after signing contract
/// </summary>
public class SignContractResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SignedContractUrl { get; set; }
    public DateTime? SignedAt { get; set; }
}
