using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookingCare.Services.ServiceMedical.Models.DTOs.Requests
{
    /// <summary>
    /// Base class containing common service properties to avoid code duplication
    /// </summary>
    public abstract class BaseServiceRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        [JsonRequired]
        public Guid HospitalId { get; set; }

        public Guid? ServiceCategoryId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Duration time must be greater than 0")]
        public int DurationTime { get; set; }
    }

    public class CreateServiceRequest : BaseServiceRequest
    {
        // Inherits all properties from BaseServiceRequest
    }

    public class UpdateServiceRequest : BaseServiceRequest
    {
        [Required]
        [JsonRequired]
        public Guid Id { get; set; }

        public string Status { get; set; } = "INACTIVE";
    }

    public class ServiceQueryRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public Guid? HospitalId { get; set; }
        public Guid? ServiceCategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Sort field: "Name" or "Price"
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Sort direction: "asc" (ascending) or "desc" (descending). Default: "asc"
        /// </summary>
        public string? SortDirection { get; set; } = "asc";
    }

    public class GetServicesByCategoryRequest
    {
        [Required]
        public Guid ServiceCategoryId { get; set; }
        public bool IncludeInactive { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetHospitalsByServiceCategoryRequest
    {
        [Required]
        public Guid ServiceCategoryId { get; set; }
        public bool IncludeInactive { get; set; } = false;
    }

}
