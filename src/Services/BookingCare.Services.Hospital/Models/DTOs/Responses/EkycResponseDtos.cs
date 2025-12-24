using BookingCare.Services.Hospital.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Responses;

/// <summary>
/// Response DTO for OCR ID card processing
/// </summary>
public class EkycOcrResponseDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    // Front side information
    public string? IdNumber { get; set; }
    public string? FullName { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? PlaceOfOrigin { get; set; }
    public string? PlaceOfResidence { get; set; }
    public string? ExpiryDate { get; set; }

    // Back side information
    public string? IssueDate { get; set; }
    public string? IssuedBy { get; set; }

    // Confidence scores
    public double? OverallConfidence { get; set; }
    public Dictionary<string, double>? FieldConfidences { get; set; }

    // Document type detection
    public string? DocumentType { get; set; } // CMND, CCCD, PASSPORT
}

/// <summary>
/// Response DTO for face matching verification
/// </summary>
public class EkycFaceMatchResponseDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    public bool IsMatch { get; set; }
    public double Similarity { get; set; }
    public string? Message { get; set; }

    // Threshold used for matching
    public double Threshold { get; set; } = 80.0;
}

/// <summary>
/// Response DTO for liveness detection
/// </summary>
public class EkycLivenessResponseDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    public bool IsLive { get; set; }
    public double LivenessScore { get; set; }
    public string? Message { get; set; }

    // Threshold used for liveness
    public double Threshold { get; set; } = 80.0;
}

/// <summary>
/// Response DTO for complete eKYC verification
/// Privacy-friendly: Images are not stored, only verification results
/// </summary>
public class EkycVerificationResponseDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public EkycStatus Status { get; set; }
    public string? SessionId { get; set; }

    // OCR Results (returned for display only, not stored)
    public EkycOcrResponseDto? OcrResult { get; set; }

    // Face Match Results
    public EkycFaceMatchResponseDto? FaceMatchResult { get; set; }

    // Liveness Results
    public EkycLivenessResponseDto? LivenessResult { get; set; }

    // Overall verification
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

/// <summary>
/// FPT.AI OCR API Response model
/// </summary>
public class FptOcrApiResponse
{
    public int ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public List<FptOcrData>? Data { get; set; }
}

public class FptOcrData
{
    public string? Id { get; set; }
    public string? Id_prob { get; set; }
    public string? Name { get; set; }
    public string? Name_prob { get; set; }
    public string? Dob { get; set; }
    public string? Dob_prob { get; set; }
    public string? Sex { get; set; }
    public string? Sex_prob { get; set; }
    public string? Nationality { get; set; }
    public string? Nationality_prob { get; set; }
    public string? Home { get; set; }
    public string? Home_prob { get; set; }
    public string? Address { get; set; }
    public string? Address_prob { get; set; }
    public string? Doe { get; set; }
    public string? Doe_prob { get; set; }
    public string? Issue_date { get; set; }
    public string? Issue_date_prob { get; set; }
    public string? Issue_loc { get; set; }
    public string? Issue_loc_prob { get; set; }
    public string? Type { get; set; }
    public string? Type_new { get; set; }
}

/// <summary>
/// FPT.AI Face Match API Response model
/// </summary>
public class FptFaceMatchApiResponse
{
    public int Code { get; set; }
    public string? Message { get; set; }
    public FptFaceMatchData? Data { get; set; }
}

public class FptFaceMatchData
{
    public string? IsMatch { get; set; }
    public string? Similarity { get; set; }
    public FptFaceInfo? Face1 { get; set; }
    public FptFaceInfo? Face2 { get; set; }
}

public class FptFaceInfo
{
    public string? X { get; set; }
    public string? Y { get; set; }
    public string? W { get; set; }
    public string? H { get; set; }
}

/// <summary>
/// FPT.AI Liveness API Response model
/// </summary>
public class FptLivenessApiResponse
{
    public int Code { get; set; }
    public string? Message { get; set; }
    public FptLivenessData? Data { get; set; }
}

public class FptLivenessData
{
    public string? Result { get; set; }
    public string? Liveness { get; set; }
}
