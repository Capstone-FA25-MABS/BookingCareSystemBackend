using System.Text.Json.Serialization;

namespace BookingCare.Services.ServiceMedical.Models.DTOs.Responses
{
    public class ServiceCategoryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ParentId { get; set; }
        public string Status { get; set; } = "INACTIVE";

        // Navigation properties (ignored in JSON serialization)
        [JsonIgnore]
        public ServiceCategoryResponse? Parent { get; set; }
        public List<ServiceCategoryResponse> Children { get; set; } = new();
    }

    public class ServiceCategoryListResponse
    {
        public List<ServiceCategoryResponse> Categories { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Response model for admin service category (flat list without navigation properties)
    /// </summary>
    public class ServiceCategoryAdminResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ParentId { get; set; }
        public string Status { get; set; } = "INACTIVE";
    }

    /// <summary>
    /// Response model for admin service category management (flat list with pagination)
    /// </summary>
    public class ServiceCategoryAdminListResponse
    {
        public List<ServiceCategoryAdminResponse> ServiceCategories { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
