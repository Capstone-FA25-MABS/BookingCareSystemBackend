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
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
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
}
