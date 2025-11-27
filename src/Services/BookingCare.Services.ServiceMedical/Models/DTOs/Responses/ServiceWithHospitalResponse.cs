using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;

namespace BookingCare.Services.ServiceMedical.Models.DTOs.Responses
{
    /// <summary>
    /// Response model for service with hospital information
    /// </summary>
    public class ServiceWithHospitalResponse : BaseServiceResponse
    {
        // Service Category Information
        public ServiceCategoryResponse? ServiceCategory { get; set; }

        // Hospital Information
        public HospitalInfoResponse? Hospital { get; set; }

        // Review Statistics
        public ServiceReviewStatisticsResponse? ReviewStatistics { get; set; }
    }

    /// <summary>
    /// Service review statistics response model
    /// </summary>
    public class ServiceReviewStatisticsResponse
    {
        public double AverageRating { get; set; }
        public long TotalReviews { get; set; }
    }

    /// <summary>
    /// Hospital information response model
    /// </summary>
    public class HospitalInfoResponse : BaseHospitalResponse
    {
        public Guid AccountId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? BackgroundUrl { get; set; }
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
    public class ServicesByCategoryWithHospitalResponse : BaseServicesByCategoryResponse<ServiceWithHospitalResponse>
    {
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
