using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;

namespace BookingCare.Services.ServiceMedical.Models.DTOs.Responses
{
    /// <summary>
    /// Optimized response model for service with minimal hospital information
    /// </summary>
    public class ServiceOptimizedResponse : BaseServiceResponse
    {
        // Only parent category name instead of full ServiceCategory object
        public string? ParentCategoryName { get; set; }

        // Minimal hospital information
        public HospitalBasicInfo? Hospital { get; set; }
    }

    /// <summary>
    /// Basic hospital information for optimized response
    /// </summary>
    public class HospitalBasicInfo : BaseHospitalResponse
    {
    }

    /// <summary>
    /// Optimized response model for services by category with hospital information
    /// </summary>
    public class ServicesByCategoryOptimizedResponse : BaseServicesByCategoryResponse<ServiceOptimizedResponse>
    {
        public string? ServiceCategoryDescription { get; set; } // Add service category description
        public string? ParentCategoryName { get; set; } // Add parent category name
    }
}
