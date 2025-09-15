namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO cho phân trang danh sách payments
/// </summary>
public class GetPaymentsPagedRequest
{
    /// <summary>
    /// Số trang (bắt đầu từ 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Số lượng items mỗi trang (tối đa 100)
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Từ khóa tìm kiếm (tùy chọn)
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Sắp xếp theo trường nào (CreatedAt, Amount, Status)
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Thứ tự sắp xếp (asc hoặc desc)
    /// </summary>
    public string SortOrder { get; set; } = "desc";
}