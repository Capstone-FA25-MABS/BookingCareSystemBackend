using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface cho Tag service - quản lý tags/labels cho cuộc hội thoại
/// </summary>
public interface ITagService
{
    #region Tag Management

    /// <summary>
    /// Tạo tag mới
    /// </summary>
    Task<TagDto> CreateTagAsync(string userId, CreateTagDto dto);

    /// <summary>
    /// Cập nhật tag
    /// </summary>
    Task<TagDto?> UpdateTagAsync(string userId, string tagId, UpdateTagDto dto);

    /// <summary>
    /// Xóa tag
    /// </summary>
    Task<bool> DeleteTagAsync(string userId, string tagId);

    /// <summary>
    /// Lấy tag theo ID
    /// </summary>
    Task<TagDto?> GetTagByIdAsync(string tagId);

    /// <summary>
    /// Lấy danh sách tag của user
    /// </summary>
    Task<IEnumerable<TagDto>> GetUserTagsAsync(string userId, bool includeSystem = true);

    /// <summary>
    /// Lấy danh sách system tags
    /// </summary>
    Task<IEnumerable<TagDto>> GetSystemTagsAsync();

    #endregion

    #region Conversation-Tag Association

    /// <summary>
    /// Gán tag vào cuộc hội thoại
    /// </summary>
    Task<bool> AddTagToConversationAsync(string userId, string conversationId, List<string> tagIds);

    /// <summary>
    /// Xóa tag khỏi cuộc hội thoại
    /// </summary>
    Task<bool> RemoveTagFromConversationAsync(string userId, string conversationId, string tagId);

    /// <summary>
    /// Xóa tất cả tag khỏi cuộc hội thoại
    /// </summary>
    Task<bool> RemoveAllTagsFromConversationAsync(string userId, string conversationId);

    /// <summary>
    /// Cập nhật danh sách tag cho cuộc hội thoại (replace toàn bộ)
    /// </summary>
    Task<bool> UpdateConversationTagsAsync(
        string userId,
        string conversationId,
        List<string> tagIds
    );

    /// <summary>
    /// Lấy danh sách tag của một cuộc hội thoại
    /// </summary>
    Task<IEnumerable<TagDto>> GetConversationTagsAsync(string conversationId);

    /// <summary>
    /// Lấy danh sách tag của user cho một cuộc hội thoại cụ thể
    /// </summary>
    Task<IEnumerable<TagDto>> GetUserConversationTagsAsync(string userId, string conversationId);

    #endregion

    #region Tag Statistics & Filtering

    /// <summary>
    /// Lấy thống kê của tag (số lượng cuộc hội thoại, tin nhắn chưa đọc, etc.)
    /// </summary>
    Task<TagStatisticsDto?> GetTagStatisticsAsync(string userId, string tagId);

    /// <summary>
    /// Lấy danh sách thống kê của tất cả tag của user
    /// </summary>
    Task<IEnumerable<TagStatisticsDto>> GetAllTagsStatisticsAsync(string userId);

    /// <summary>
    /// Lọc cuộc hội thoại theo tag
    /// </summary>
    Task<PaginatedResponse<ConversationResponse>> FilterConversationsByTagsAsync(
        string userId,
        List<string> tagIds,
        string filterMode = "any",
        int pageNumber = 1,
        int pageSize = 20
    );

    /// <summary>
    /// Lấy số lượng cuộc hội thoại theo từng tag
    /// </summary>
    Task<Dictionary<string, int>> GetConversationCountByTagsAsync(string userId);

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Gán một tag cho nhiều cuộc hội thoại
    /// </summary>
    Task<int> BulkAddTagToConversationsAsync(
        string userId,
        string tagId,
        List<string> conversationIds
    );

    /// <summary>
    /// Xóa một tag khỏi nhiều cuộc hội thoại
    /// </summary>
    Task<int> BulkRemoveTagFromConversationsAsync(
        string userId,
        string tagId,
        List<string> conversationIds
    );

    #endregion

    #region Validation & Helper Methods

    /// <summary>
    /// Kiểm tra tag có tồn tại và thuộc về user không
    /// </summary>
    Task<bool> ValidateTagOwnershipAsync(string userId, string tagId);

    /// <summary>
    /// Kiểm tra danh sách tag có hợp lệ không
    /// </summary>
    Task<bool> ValidateTagsAsync(string userId, List<string> tagIds);

    #endregion
}
