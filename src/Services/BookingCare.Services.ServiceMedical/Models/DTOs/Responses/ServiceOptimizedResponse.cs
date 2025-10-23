using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;

namespace BookingCare.Services.ServiceMedical.Models.DTOs.Responses
{
    /// <summary>
    /// Optimized response model for service with minimal hospital information
    /// </summary>
    public class ServiceOptimizedResponse
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

        // Only parent category name instead of full ServiceCategory object
        public string? ParentCategoryName { get; set; }

        // Minimal hospital information
        public HospitalBasicInfo? Hospital { get; set; }
    }

    /// <summary>
    /// Basic hospital information for optimized response
    /// </summary>
    public class HospitalBasicInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }

    /// <summary>
    /// Optimized response model for services by category with hospital information
    /// </summary>
    public class ServicesByCategoryOptimizedResponse
    {
        public Guid ServiceCategoryId { get; set; }
        public string ServiceCategoryName { get; set; } = string.Empty;
        public string? ServiceCategoryDescription { get; set; } // Add service category description
        public string? ParentCategoryName { get; set; } // Add parent category name
        public int TotalServices { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<ServiceOptimizedResponse> Services { get; set; } = new();
    }
}
