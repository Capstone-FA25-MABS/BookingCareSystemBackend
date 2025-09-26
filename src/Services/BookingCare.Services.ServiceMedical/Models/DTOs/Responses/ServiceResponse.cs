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
}
