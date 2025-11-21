using System.ComponentModel.DataAnnotations;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

public class CreateSubscriptionPlanRequest
{
    [Required(ErrorMessage = "Tên gói dịch vụ là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên gói dịch vụ không được vượt quá 100 ký tự")]
    [MinLength(2, ErrorMessage = "Tên gói dịch vụ phải có ít nhất 2 ký tự")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Giá là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Chu kỳ thanh toán là bắt buộc")]
    [MaxLength(20, ErrorMessage = "Chu kỳ thanh toán không hợp lệ")]
    [RegularExpression("^(MONTHLY|QUARTERLY|YEARLY)$", ErrorMessage = "Chu kỳ thanh toán phải là MONTHLY, QUARTERLY hoặc YEARLY")]
    public string BillingCycle { get; set; } = "MONTHLY";

    [Range(-1, int.MaxValue, ErrorMessage = "Số bác sĩ tối đa phải lớn hơn hoặc bằng -1 (-1 = không giới hạn)")]
    public int? MaxDoctors { get; set; } = 0; // null hoặc -1 = unlimited

    [Range(-1, int.MaxValue, ErrorMessage = "Số chuyên khoa tối đa phải lớn hơn hoặc bằng -1 (-1 = không giới hạn)")]
    public int? MaxSpecialties { get; set; } = 0; // null hoặc -1 = unlimited

    [Range(-1, int.MaxValue, ErrorMessage = "Số lịch hẹn tối đa phải lớn hơn hoặc bằng -1 (-1 = không giới hạn)")]
    public int? MaxAppointments { get; set; } = 0; // null hoặc -1 = unlimited

    [Range(-1, int.MaxValue, ErrorMessage = "Số dịch vụ tối đa phải lớn hơn hoặc bằng -1 (-1 = không giới hạn)")]
    public int? MaxServices { get; set; } = 0; // null hoặc -1 = unlimited

    [MaxLength(5000, ErrorMessage = "Tính năng không được vượt quá 5000 ký tự")]
    public string? Features { get; set; }
}

public class UpdateSubscriptionPlanRequest
{
    [MaxLength(100, ErrorMessage = "Tên gói dịch vụ không được vượt quá 100 ký tự")]
    [MinLength(2, ErrorMessage = "Tên gói dịch vụ phải có ít nhất 2 ký tự")]
    public string? Name { get; set; }

    [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
    public decimal? Price { get; set; }

    [MaxLength(20, ErrorMessage = "Chu kỳ thanh toán không hợp lệ")]
    [RegularExpression("^(MONTHLY|QUARTERLY|YEARLY)$", ErrorMessage = "Chu kỳ thanh toán phải là MONTHLY, QUARTERLY hoặc YEARLY")]
    public string? BillingCycle { get; set; }

    [Range(-1, int.MaxValue, ErrorMessage = "Số bác sĩ tối đa phải lớn hơn hoặc bằng -1 (-1 = không giới hạn)")]
    public int? MaxDoctors { get; set; }

    [Range(-1, int.MaxValue, ErrorMessage = "Số chuyên khoa tối đa phải lớn hơn hoặc bằng -1 (-1 = không giới hạn)")]
    public int? MaxSpecialties { get; set; }

    [Range(-1, int.MaxValue, ErrorMessage = "Số lịch hẹn tối đa phải lớn hơn hoặc bằng -1 (-1 = không giới hạn)")]
    public int? MaxAppointments { get; set; }

    [Range(-1, int.MaxValue, ErrorMessage = "Số dịch vụ tối đa phải lớn hơn hoặc bằng -1 (-1 = không giới hạn)")]
    public int? MaxServices { get; set; }

    [MaxLength(5000, ErrorMessage = "Tính năng không được vượt quá 5000 ký tự")]
    public string? Features { get; set; }

    public Status? Status { get; set; }
}

public class SubscriptionPlanFilterRequest
{
    public string? Name { get; set; }
    public Status? Status { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? BillingCycle { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
}
