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

        // Navigation properties
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
}
