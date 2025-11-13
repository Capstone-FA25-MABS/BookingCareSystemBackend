using System.ComponentModel.DataAnnotations;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Models.DTOs;

#region Tag DTOs

/// <summary>
/// DTO cho việc tạo tag mới
/// </summary>
public class CreateTagDto
{
    /// <summary>
    /// Tên của tag
    /// </summary>
    [Required(ErrorMessage = "Tên tag không được để trống")]
    [StringLength(50, ErrorMessage = "Tên tag không được vượt quá 50 ký tự")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết về tag
    /// </summary>
    [StringLength(200, ErrorMessage = "Mô tả không được vượt quá 200 ký tự")]
    public string? Description { get; set; }

    /// <summary>
    /// Màu sắc của tag (hex color code)
    /// </summary>
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Mã màu không hợp lệ")]
    public string Color { get; set; } = "#3B82F6"; // Blue-500 default

    /// <summary>
    /// Icon của tag (emoji hoặc icon name)
    /// </summary>
    [StringLength(20, ErrorMessage = "Icon không được vượt quá 20 ký tự")]
    public string? Icon { get; set; }

    /// <summary>
    /// Loại tag
    /// </summary>
    public ConversationTagType Type { get; set; } = ConversationTagType.Custom;

    /// <summary>
    /// Thứ tự sắp xếp của tag
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự phải là số không âm")]
    public int Order { get; set; } = 0;

    /// <summary>
    /// Tag có được ghim không
    /// </summary>
    public bool IsPinned { get; set; } = false;
}

/// <summary>
/// DTO cho việc cập nhật tag
/// </summary>
public class UpdateTagDto
{
    /// <summary>
    /// Tên của tag
    /// </summary>
    [StringLength(50, ErrorMessage = "Tên tag không được vượt quá 50 ký tự")]
    public string? Name { get; set; }

    /// <summary>
    /// Mô tả chi tiết về tag
    /// </summary>
    [StringLength(200, ErrorMessage = "Mô tả không được vượt quá 200 ký tự")]
    public string? Description { get; set; }

    /// <summary>
    /// Màu sắc của tag (hex color code)
    /// </summary>
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Mã màu không hợp lệ")]
    public string? Color { get; set; }

    /// <summary>
    /// Icon của tag
    /// </summary>
    [StringLength(20, ErrorMessage = "Icon không được vượt quá 20 ký tự")]
    public string? Icon { get; set; }

    /// <summary>
    /// Loại tag
    /// </summary>
    public ConversationTagType? Type { get; set; }

    /// <summary>
    /// Thứ tự sắp xếp của tag
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự phải là số không âm")]
    public int? Order { get; set; }

    /// <summary>
    /// Tag có được ghim không
    /// </summary>
    public bool? IsPinned { get; set; }

    /// <summary>
    /// Trạng thái hoạt động của tag
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO cho thông tin tag trả về
/// </summary>
public class TagDto
{
    /// <summary>
    /// ID của tag
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID người dùng tạo tag
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Tên của tag
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết về tag
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Màu sắc của tag
    /// </summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Icon của tag
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Loại tag
    /// </summary>
    public ConversationTagType Type { get; set; }

    /// <summary>
    /// Thứ tự sắp xếp
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Số lượng cuộc hội thoại sử dụng tag này
    /// </summary>
    public int ConversationCount { get; set; }

    /// <summary>
    /// Tag có được ghim không
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời gian cập nhật
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

#endregion

#region Conversation-Tag Association DTOs

/// <summary>
/// DTO cho việc gán tag vào cuộc hội thoại
/// </summary>
public class AddTagToConversationDto
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    [Required(ErrorMessage = "ID cuộc hội thoại không được để trống")]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách ID các tag cần gán
    /// </summary>
    [Required(ErrorMessage = "Danh sách tag không được để trống")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất một tag")]
    public List<string> TagIds { get; set; } = new();
}

/// <summary>
/// DTO cho việc xóa tag khỏi cuộc hội thoại
/// </summary>
public class RemoveTagFromConversationDto
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    [Required(ErrorMessage = "ID cuộc hội thoại không được để trống")]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID của tag cần xóa
    /// </summary>
    [Required(ErrorMessage = "ID tag không được để trống")]
    public string TagId { get; set; } = string.Empty;
}

/// <summary>
/// DTO cho việc lọc cuộc hội thoại theo tag
/// </summary>
public class FilterConversationsByTagDto
{
    /// <summary>
    /// Danh sách ID các tag để lọc
    /// </summary>
    [Required(ErrorMessage = "Danh sách tag không được để trống")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất một tag")]
    public List<string> TagIds { get; set; } = new();

    /// <summary>
    /// Chế độ lọc: "any" (có ít nhất 1 tag) hoặc "all" (có tất cả các tag)
    /// </summary>
    [RegularExpression(@"^(any|all)$", ErrorMessage = "Chế độ lọc phải là 'any' hoặc 'all'")]
    public string FilterMode { get; set; } = "any";

    /// <summary>
    /// Số trang (bắt đầu từ 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn 0")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Số lượng kết quả mỗi trang
    /// </summary>
    [Range(1, 100, ErrorMessage = "Kích thước trang phải từ 1 đến 100")]
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// DTO cho thống kê tag
/// </summary>
public class TagStatisticsDto
{
    /// <summary>
    /// Thông tin tag
    /// </summary>
    public TagDto Tag { get; set; } = new();

    /// <summary>
    /// Số lượng cuộc hội thoại có tag này
    /// </summary>
    public int TotalConversations { get; set; }

    /// <summary>
    /// Số lượng tin nhắn chưa đọc trong các cuộc hội thoại có tag này
    /// </summary>
    public int UnreadMessagesCount { get; set; }

    /// <summary>
    /// Thời gian tin nhắn mới nhất trong các cuộc hội thoại có tag này
    /// </summary>
    public DateTime? LastMessageAt { get; set; }
}

#endregion
