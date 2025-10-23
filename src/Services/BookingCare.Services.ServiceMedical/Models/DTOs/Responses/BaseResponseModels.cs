using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;

namespace BookingCare.Services.ServiceMedical.Models.DTOs.Responses
{
    /// <summary>
    /// Base response model for service
    /// </summary>
    public abstract class BaseServiceResponse
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
    }

    /// <summary>
    /// Base hospital information response model
    /// </summary>
    public abstract class BaseHospitalResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }

    /// <summary>
    /// Base response model for services by category
    /// </summary>
    public abstract class BaseServicesByCategoryResponse<T> where T : BaseServiceResponse
    {
        public Guid ServiceCategoryId { get; set; }
        public string ServiceCategoryName { get; set; } = string.Empty;
        public int TotalServices { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<T> Services { get; set; } = new();
    }
}
