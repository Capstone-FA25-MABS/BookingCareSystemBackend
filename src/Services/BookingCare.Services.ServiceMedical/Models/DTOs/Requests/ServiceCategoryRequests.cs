using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookingCare.Services.ServiceMedical.Models.DTOs.Requests
{
    public class CreateServiceCategoryRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ParentId { get; set; }
    }

    public class UpdateServiceCategoryRequest
    {
        [Required]
        [JsonRequired]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ParentId { get; set; }
        public string Status { get; set; } = "INACTIVE";
    }

    public class ServiceCategoryQueryRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public Guid? ParentId { get; set; }
        public bool IncludeChildren { get; set; } = false;
    }

    public class GetServiceCategoryChildrenRequest
    {
        [Required]
        public Guid ParentId { get; set; }
        public bool IncludeInactive { get; set; } = false;
    }
}
