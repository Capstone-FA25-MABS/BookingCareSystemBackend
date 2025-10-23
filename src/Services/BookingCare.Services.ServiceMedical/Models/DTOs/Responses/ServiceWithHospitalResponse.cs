using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;

namespace BookingCare.Services.ServiceMedical.Models.DTOs.Responses
{
    /// <summary>
    /// Response model for service with hospital information
    /// </summary>
    public class ServiceWithHospitalResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public Guid HospitalId { get; set; }
        public Guid? ServiceCategoryId { get; set; }
        public int DurationTime { get; set; }
        public string Status { get; set; } = string.Empty;

        // Service Category Information
        public ServiceCategoryResponse? ServiceCategory { get; set; }

        // Hospital Information
        public HospitalInfoResponse? Hospital { get; set; }
    }

    /// <summary>
    /// Hospital information response model
    /// </summary>
    public class HospitalInfoResponse
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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<HospitalSpecialtyInfoResponse>? Specialties { get; set; }
        public List<HospitalImageInfoResponse>? Images { get; set; }
        public HospitalSubscriptionInfoResponse? CurrentSubscription { get; set; }
    }

    /// <summary>
    /// Hospital specialty information for ServiceMedical
    /// </summary>
    public class HospitalSpecialtyInfoResponse
    {
        public Guid SpecialtyId { get; set; }
        public string? SpecialtyName { get; set; }
    }

    /// <summary>
    /// Hospital image information for ServiceMedical
    /// </summary>
    public class HospitalImageInfoResponse
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; }
    }

    /// <summary>
    /// Hospital subscription information for ServiceMedical
    /// </summary>
    public class HospitalSubscriptionInfoResponse
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// Response model for services by category with hospital information
    /// </summary>
    public class ServicesByCategoryWithHospitalResponse
    {
        public Guid ServiceCategoryId { get; set; }
        public string ServiceCategoryName { get; set; } = string.Empty;
        public int TotalServices { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<ServiceWithHospitalResponse> Services { get; set; } = new();
    }

    /// <summary>
    /// Response for batch hospital retrieval from Hospital Service
    /// </summary>
    public class HospitalsByIdsResponse
    {
        public List<HospitalInfoResponse> Hospitals { get; set; } = new();
        public int TotalCount { get; set; }
        public int RequestedCount { get; set; }
        public List<Guid> NotFoundIds { get; set; } = new();
    }
}
