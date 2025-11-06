namespace BookingCare.Services.Hospital.Models.DTOs.Responses;

public class HospitalProfileResponse
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? BackgroundUrl { get; set; }
    public string? AvatarUrl { get; set; }
    public List<HospitalImageSimpleResponse> Images { get; set; } = new();
    public List<HospitalSpecialtyWithImageResponse> Specialties { get; set; } = new();
    public List<HospitalServiceTypeResponse> ServiceTypes { get; set; } = new();
    public List<HospitalServiceMedicalResponse> ServiceMedicals { get; set; } = new();
}

public class HospitalImageSimpleResponse
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class HospitalSpecialtyWithImageResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int DoctorCount { get; set; } = 0;
}



