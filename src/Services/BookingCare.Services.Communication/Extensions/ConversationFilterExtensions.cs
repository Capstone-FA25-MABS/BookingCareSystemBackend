using BookingCare.Services.Communication.Models.Entities;

namespace BookingCare.Services.Communication.Extensions;

/// <summary>
/// Extension methods cho filtering conversations
/// </summary>
public static class ConversationFilterExtensions
{
    /// <summary>
    /// Lọc conversations theo tag IDs với filter mode
    /// </summary>
    /// <param name="conversations">Danh sách conversations cần filter</param>
    /// <param name="userId">User ID để kiểm tra UserTags</param>
    /// <param name="tagIds">Danh sách tag IDs cần filter</param>
    /// <param name="filterMode">Mode: "all" (phải có tất cả tags) hoặc "any" (có ít nhất 1 tag)</param>
    /// <returns>Danh sách conversations đã được filter</returns>
    public static IEnumerable<ConversationEntity> FilterByTags(
        this IEnumerable<ConversationEntity> conversations,
        string userId,
        List<string> tagIds,
        string filterMode = "any"
    )
    {
        if (filterMode.ToLower() == "all")
        {
            // Conversation phải có TẤT CẢ các tag (trong UserTags của user)
            return conversations.Where(c =>
                c.UserTags.ContainsKey(userId)
                && tagIds.All(tagId => c.UserTags[userId].Contains(tagId))
            );
        }
        else // "any"
        {
            // Conversation có ÍT NHẤT 1 tag (trong UserTags của user)
            return conversations.Where(c =>
                c.UserTags.ContainsKey(userId)
                && c.UserTags[userId].Any(tagId => tagIds.Contains(tagId))
            );
        }
    }

    /// <summary>
    /// Kiểm tra conversation có chứa tag cụ thể cho user không
    /// </summary>
    /// <param name="conversation">Conversation cần kiểm tra</param>
    /// <param name="userId">User ID</param>
    /// <param name="tagId">Tag ID cần kiểm tra</param>
    /// <returns>True nếu conversation có tag, ngược lại false</returns>
    public static bool HasTag(this ConversationEntity conversation, string userId, string tagId)
    {
        return conversation.UserTags.ContainsKey(userId)
            && conversation.UserTags[userId].Contains(tagId);
    }

    /// <summary>
    /// Kiểm tra conversation có chứa bất kỳ tag nào trong danh sách cho user không
    /// </summary>
    /// <param name="conversation">Conversation cần kiểm tra</param>
    /// <param name="userId">User ID</param>
    /// <param name="tagIds">Danh sách tag IDs</param>
    /// <returns>True nếu có ít nhất 1 tag, ngược lại false</returns>
    public static bool HasAnyTag(
        this ConversationEntity conversation,
        string userId,
        List<string> tagIds
    )
    {
        return conversation.UserTags.ContainsKey(userId)
            && conversation.UserTags[userId].Any(tagId => tagIds.Contains(tagId));
    }

    /// <summary>
    /// Kiểm tra conversation có chứa tất cả tags trong danh sách cho user không
    /// </summary>
    /// <param name="conversation">Conversation cần kiểm tra</param>
    /// <param name="userId">User ID</param>
    /// <param name="tagIds">Danh sách tag IDs</param>
    /// <returns>True nếu có tất cả tags, ngược lại false</returns>
    public static bool HasAllTags(
        this ConversationEntity conversation,
        string userId,
        List<string> tagIds
    )
    {
        return conversation.UserTags.ContainsKey(userId)
            && tagIds.All(tagId => conversation.UserTags[userId].Contains(tagId));
    }

    /// <summary>
    /// Lấy danh sách tag IDs của user trong conversation
    /// </summary>
    /// <param name="conversation">Conversation</param>
    /// <param name="userId">User ID</param>
    /// <returns>Danh sách tag IDs hoặc empty list nếu không có</returns>
    public static List<string> GetUserTags(this ConversationEntity conversation, string userId)
    {
        return conversation.UserTags.ContainsKey(userId)
            ? conversation.UserTags[userId]
            : new List<string>();
    }
}
