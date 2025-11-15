using BookingCare.Services.Hospital.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Responses;

/// <summary>
/// Response DTO for hospital registration
/// </summary>
public class HospitalRegistrationResponseDto
{
    public Guid Id { get; set; }

    // Representative Information
    public string RepresentativeName { get; set; } = string.Empty;
    public string RepresentativeEmail { get; set; } = string.Empty;
    public string RepresentativePhone { get; set; } = string.Empty;

    // Hospital Information
    public string HospitalName { get; set; } = string.Empty;
    public string HospitalEmail { get; set; } = string.Empty;
    public string HospitalPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string LicenseFile { get; set; } = string.Empty;
    public string BusinessCertificateFile { get; set; } = string.Empty;
    public string IdentityCardFile { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public RegistrationStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string? ContractFile { get; set; }
    public Guid? HospitalId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Response DTO for paginated hospital registrations list
/// </summary>
public class HospitalRegistrationListResponseDto
{
    public List<HospitalRegistrationResponseDto> Registrations { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

