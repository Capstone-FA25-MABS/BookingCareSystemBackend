using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Communication.Controllers;

/// <summary>
/// Controller cho quản lý tags/labels của cuộc hội thoại
/// </summary>
[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class TagsController : BaseApiController
{
    private readonly ITagService _tagService;
    private readonly IConversationService _conversationService;

    public TagsController(ITagService tagService, IConversationService conversationService)
    {
        _tagService = tagService;
        _conversationService = conversationService;
    }

    #region Tag Management

    /// <summary>
    /// Tạo tag mới
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="dto">Thông tin tag cần tạo</param>
    [HttpPost("users/{userId}/tags")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateTag(string userId, [FromBody] CreateTagDto dto)
    {
        var result = await _tagService.CreateTagAsync(userId, dto);
        return Created(result, "Tag đã được tạo thành công!");
    }

    /// <summary>
    /// Cập nhật tag
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="tagId">ID của tag cần cập nhật</param>
    /// <param name="dto">Thông tin cập nhật</param>
    [HttpPut("users/{userId}/tags/{tagId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateTag(
        string userId,
        string tagId,
        [FromBody] UpdateTagDto dto
    )
    {
        var result = await _tagService.UpdateTagAsync(userId, tagId, dto);
        if (result == null)
        {
            return NotFound($"Tag với ID '{tagId}' không tồn tại");
        }
        return Success(result, "Tag đã được cập nhật thành công!");
    }

    /// <summary>
    /// Xóa tag
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="tagId">ID của tag cần xóa</param>
    [HttpDelete("users/{userId}/tags/{tagId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteTag(string userId, string tagId)
    {
        var result = await _tagService.DeleteTagAsync(userId, tagId);
        if (!result)
        {
            return NotFound($"Tag với ID '{tagId}' không tồn tại");
        }
        return Success(new { TagId = tagId }, "Tag đã được xóa thành công!");
    }

    /// <summary>
    /// Lấy thông tin tag theo ID
    /// </summary>
    /// <param name="tagId">ID của tag</param>
    [HttpGet("tags/{tagId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetTagById(string tagId)
    {
        var result = await _tagService.GetTagByIdAsync(tagId);
        if (result == null)
        {
            return NotFound($"Tag với ID '{tagId}' không tồn tại");
        }
        return Success(result, "Lấy thông tin tag thành công!");
    }

    /// <summary>
    /// Lấy danh sách tag của user
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="includeSystem">Có bao gồm system tags không</param>
    [HttpGet("users/{userId}/tags")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetUserTags(
        string userId,
        [FromQuery] bool includeSystem = true
    )
    {
        var result = await _tagService.GetUserTagsAsync(userId, includeSystem);
        return Success(result, "Lấy danh sách tag thành công!");
    }

    /// <summary>
    /// Lấy danh sách system tags
    /// </summary>
    [HttpGet("tags/system")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetSystemTags()
    {
        var result = await _tagService.GetSystemTagsAsync();
        return Success(result, "Lấy danh sách system tags thành công!");
    }

    #endregion

    #region Conversation-Tag Association

    /// <summary>
    /// Gán tag vào cuộc hội thoại
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="dto">Thông tin gán tag</param>
    [HttpPost("users/{userId}/conversations/tags/add")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> AddTagToConversation(
        string userId,
        [FromBody] AddTagToConversationDto dto
    )
    {
        var result = await _tagService.AddTagToConversationAsync(
            userId,
            dto.ConversationId,
            dto.TagIds
        );
        return Success(new { Success = result }, "Gán tag thành công!");
    }

    /// <summary>
    /// Xóa tag khỏi cuộc hội thoại
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="dto">Thông tin xóa tag</param>
    [HttpPost("users/{userId}/conversations/tags/remove")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RemoveTagFromConversation(
        string userId,
        [FromBody] RemoveTagFromConversationDto dto
    )
    {
        var result = await _tagService.RemoveTagFromConversationAsync(
            userId,
            dto.ConversationId,
            dto.TagId
        );
        return Success(new { Success = result }, "Xóa tag thành công!");
    }

    /// <summary>
    /// Xóa tất cả tag khỏi cuộc hội thoại
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="conversationId">ID của cuộc hội thoại</param>
    [HttpDelete("users/{userId}/conversations/{conversationId}/tags")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RemoveAllTagsFromConversation(
        string userId,
        string conversationId
    )
    {
        var result = await _tagService.RemoveAllTagsFromConversationAsync(userId, conversationId);
        return Success(new { Success = result }, "Xóa tất cả tag thành công!");
    }

    /// <summary>
    /// Cập nhật danh sách tag cho cuộc hội thoại
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="conversationId">ID của cuộc hội thoại</param>
    /// <param name="tagIds">Danh sách tag IDs mới</param>
    [HttpPut("users/{userId}/conversations/{conversationId}/tags")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateConversationTags(
        string userId,
        string conversationId,
        [FromBody] List<string> tagIds
    )
    {
        var result = await _tagService.UpdateConversationTagsAsync(userId, conversationId, tagIds);
        return Success(new { Success = result }, "Cập nhật tag thành công!");
    }

    /// <summary>
    /// Lấy danh sách tag của một cuộc hội thoại
    /// </summary>
    /// <param name="conversationId">ID của cuộc hội thoại</param>
    [HttpGet("conversations/{conversationId}/tags")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetConversationTags(string conversationId)
    {
        var result = await _tagService.GetConversationTagsAsync(conversationId);
        return Success(result, "Lấy danh sách tag thành công!");
    }

    /// <summary>
    /// Lấy danh sách tag của user cho một cuộc hội thoại cụ thể
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="conversationId">ID của cuộc hội thoại</param>
    [HttpGet("users/{userId}/conversations/{conversationId}/tags")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetUserConversationTags(string userId, string conversationId)
    {
        var result = await _tagService.GetUserConversationTagsAsync(userId, conversationId);
        return Success(result, "Lấy danh sách tag của user thành công!");
    }

    #endregion

    #region Tag Statistics & Filtering

    /// <summary>
    /// Lấy thống kê của tag
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="tagId">ID của tag</param>
    [HttpGet("users/{userId}/tags/{tagId}/statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetTagStatistics(string userId, string tagId)
    {
        var result = await _tagService.GetTagStatisticsAsync(userId, tagId);
        if (result == null)
        {
            return NotFound($"Tag với ID '{tagId}' không tồn tại");
        }
        return Success(result, "Lấy thống kê tag thành công!");
    }

    /// <summary>
    /// Lấy danh sách thống kê của tất cả tag của user
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    [HttpGet("users/{userId}/tags/statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAllTagsStatistics(string userId)
    {
        var result = await _tagService.GetAllTagsStatisticsAsync(userId);
        return Success(result, "Lấy danh sách thống kê thành công!");
    }

    /// <summary>
    /// Lọc cuộc hội thoại theo tag
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="dto">Thông tin lọc</param>
    [HttpPost("users/{userId}/conversations/filter-by-tags")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> FilterConversationsByTags(
        string userId,
        [FromBody] FilterConversationsByTagDto dto
    )
    {
        var result = await _tagService.FilterConversationsByTagsAsync(
            userId,
            dto.TagIds,
            dto.FilterMode,
            dto.PageNumber,
            dto.PageSize
        );
        return Success(result, "Lọc cuộc hội thoại thành công!");
    }

    /// <summary>
    /// Lọc cuộc hội thoại theo tag (sử dụng ConversationService)
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="dto">Thông tin lọc</param>
    [HttpPost("users/{userId}/conversations/by-tags")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetConversationsByTags(
        string userId,
        [FromBody] FilterConversationsByTagDto dto
    )
    {
        var result = await _conversationService.GetConversationsByTagsAsync(
            userId,
            dto.TagIds,
            dto.FilterMode,
            dto.PageNumber,
            dto.PageSize
        );
        return Success(result, "Lấy cuộc hội thoại theo tag thành công!");
    }

    /// <summary>
    /// Lấy cuộc hội thoại được nhóm theo tag
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    [HttpGet("users/{userId}/conversations/grouped-by-tags")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetGroupedConversationsByTags(string userId)
    {
        var result = await _conversationService.GetGroupedConversationsByTagsAsync(userId);
        return Success(result, "Lấy cuộc hội thoại nhóm theo tag thành công!");
    }

    /// <summary>
    /// Lấy số lượng cuộc hội thoại theo từng tag
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    [HttpGet("users/{userId}/tags/conversation-counts")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetConversationCountByTags(string userId)
    {
        var result = await _tagService.GetConversationCountByTagsAsync(userId);
        return Success(result, "Lấy số lượng cuộc hội thoại theo tag thành công!");
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Gán một tag cho nhiều cuộc hội thoại
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="tagId">ID của tag</param>
    /// <param name="conversationIds">Danh sách ID cuộc hội thoại</param>
    [HttpPost("users/{userId}/tags/{tagId}/bulk-add")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> BulkAddTagToConversations(
        string userId,
        string tagId,
        [FromBody] List<string> conversationIds
    )
    {
        var count = await _tagService.BulkAddTagToConversationsAsync(
            userId,
            tagId,
            conversationIds
        );
        return Success(new { AffectedCount = count }, $"Đã gán tag cho {count} cuộc hội thoại!");
    }

    /// <summary>
    /// Xóa một tag khỏi nhiều cuộc hội thoại
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="tagId">ID của tag</param>
    /// <param name="conversationIds">Danh sách ID cuộc hội thoại</param>
    [HttpPost("users/{userId}/tags/{tagId}/bulk-remove")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> BulkRemoveTagFromConversations(
        string userId,
        string tagId,
        [FromBody] List<string> conversationIds
    )
    {
        var count = await _tagService.BulkRemoveTagFromConversationsAsync(
            userId,
            tagId,
            conversationIds
        );
        return Success(new { AffectedCount = count }, $"Đã xóa tag khỏi {count} cuộc hội thoại!");
    }

    #endregion
}
