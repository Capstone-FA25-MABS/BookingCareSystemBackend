namespace BookingCare.Services.ServiceMedical.Models.DTOs.Responses
{
    public class ServiceResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public Guid HospitalId { get; set; }
        public Guid? ServiceCategoryId { get; set; }
        public int DurationTime { get; set; }
        public string Status { get; set; } = "INACTIVE";

        // Navigation properties
        public ServiceCategoryResponse? ServiceCategory { get; set; }
    }

    public class ServiceListResponse
    {
        public List<ServiceResponse> Services { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }


    // Response cho danh sách Hospital ID theo Service Category
    public class HospitalsByServiceCategoryResponse
    {
        public Guid ServiceCategoryId { get; set; }
        public string ServiceCategoryName { get; set; } = string.Empty;
        public List<Guid> HospitalIds { get; set; } = new();
        public int TotalHospitals { get; set; }
    }

    /// <summary>
    /// Response model for service with hospital name and category name
    /// </summary>
    public class ServiceDetailResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public string? HospitalName { get; set; }
        public string? ServiceCategoryName { get; set; }
        public string Status { get; set; } = "INACTIVE";
    }

    /// <summary>
    /// List response for service details
    /// </summary>
    public class ServiceDetailListResponse
    {
        public List<ServiceDetailResponse> Services { get; set; } = new();
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Simple response model with only Id and Name (for dropdowns)
    /// </summary>
    public class SimpleItemResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for filter options (hospitals and service categories)
    /// </summary>
    public class FilterOptionsResponse
    {
        public List<SimpleItemResponse> Hospitals { get; set; } = new();
        public List<SimpleItemResponse> ServiceCategories { get; set; } = new();
    }

    /// <summary>
    /// Lightweight DTO for batch fetching service basic info (gRPC optimized)
    /// </summary>
    public class ServiceBasicInfoDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }
}
