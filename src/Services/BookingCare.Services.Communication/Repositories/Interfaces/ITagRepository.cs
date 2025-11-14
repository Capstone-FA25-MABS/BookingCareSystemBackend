using BookingCare.Services.Communication.Models.Entities;

namespace BookingCare.Services.Communication.Repositories.Interfaces;

/// <summary>
/// Interface cho Tag repository
/// </summary>
public interface ITagRepository
{
    /// <summary>
    /// Lấy tag theo ID
    /// </summary>
    Task<TagEntity?> GetByIdAsync(string id);

    /// <summary>
    /// Lấy danh sách tag của user
    /// </summary>
    Task<IEnumerable<TagEntity>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Lấy tất cả system tags
    /// </summary>
    Task<IEnumerable<TagEntity>> GetSystemTagsAsync();

    /// <summary>
    /// Lấy tag theo tên (của một user cụ thể)
    /// </summary>
    Task<TagEntity?> GetByNameAsync(string userId, string name);

    /// <summary>
    /// Lấy tag theo tên bao gồm cả tags đã xóa (isActive = false)
    /// </summary>
    Task<TagEntity?> GetByNameIncludingInactiveAsync(string userId, string name);

    /// <summary>
    /// Lấy nhiều tag theo danh sách IDs
    /// </summary>
    Task<IEnumerable<TagEntity>> GetByIdsAsync(List<string> ids);

    /// <summary>
    /// Tạo tag mới
    /// </summary>
    Task<TagEntity> CreateAsync(TagEntity tag);

    /// <summary>
    /// Cập nhật tag
    /// </summary>
    Task<TagEntity?> UpdateAsync(string id, TagEntity tag);

    /// <summary>
    /// Xóa tag
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Kích hoạt lại tag đã xóa (set isActive = true)
    /// </summary>
    Task<TagEntity?> ReactivateAsync(string id);

    /// <summary>
    /// Tăng số lượng cuộc hội thoại sử dụng tag
    /// </summary>
    Task<bool> IncrementConversationCountAsync(string tagId, int increment = 1);

    /// <summary>
    /// Giảm số lượng cuộc hội thoại sử dụng tag
    /// </summary>
    Task<bool> DecrementConversationCountAsync(string tagId, int decrement = 1);

    /// <summary>
    /// Kiểm tra tag có tồn tại không
    /// </summary>
    Task<bool> ExistsAsync(string id);

    /// <summary>
    /// Kiểm tra tag có thuộc về user không
    /// </summary>
    Task<bool> IsTagOwnedByUserAsync(string tagId, string userId);

    /// <summary>
    /// Lấy danh sách tag được sắp xếp theo thứ tự và pinned
    /// </summary>
    Task<IEnumerable<TagEntity>> GetSortedTagsAsync(string userId, bool includeSystem = true);
}
