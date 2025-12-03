using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.AI.Models.DTOs.Requests;

/// <summary>
/// Request model để lấy gợi ý bác sĩ/bệnh viện sau khi đã có kết luận và chuyên khoa
/// </summary>
public class SymptomSuggestionRequest
{
    /// <summary>
    /// Danh sách tên chuyên khoa (đúng với danh sách mà conclusion trả về)
    /// </summary>
    [Required]
    public List<string> SpecialtyNames { get; set; } = new();

    /// <summary>
    /// Thông tin vị trí người dùng (tùy chọn)
    /// </summary>
    public LocationContext? Location { get; set; }
}



