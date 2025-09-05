using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Services.Interfaces;

namespace BookingCare.Services.Communication.Examples;

/// <summary>
/// Demo class showing different lazy loading scenarios
/// </summary>
public class LazyLoadingExamples
{
    private readonly IConversationService _conversationService;

    public LazyLoadingExamples(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    /// <summary>
    /// Example 1: Mobile App - Load minimal data for conversation list
    /// Tối ưu cho mobile với băng thông hạn chế
    /// </summary>
    public async Task<IEnumerable<ConversationResponse>> GetConversationsForMobile(string userId)
    {
        var options = new ConversationLoadOptions
        {
            IncludeParticipantDetails = false,  // Không load chi tiết user (tiết kiệm bandwidth)
            IncludeUnreadCount = true,          // Cần thiết cho badge notification
            IncludeRecentMessages = false,      // Không load tin nhắn gần đây (có LastMessage rồi)
            IncludeMetadata = false,            // Không cần metadata cho mobile list
            IncludeOnlineStatus = false         // Tiết kiệm real-time queries
        };

        return await _conversationService.GetByUserIdAsync(userId, 1, 10, options);
    }

    /// <summary>
    /// Example 2: Web Dashboard - Load moderate data for overview
    /// Cân bằng giữa performance và user experience
    /// </summary>
    public async Task<IEnumerable<ConversationResponse>> GetConversationsForWebDashboard(string userId)
    {
        var options = new ConversationLoadOptions
        {
            IncludeParticipantDetails = true,   // Load để hiển thị tên, avatar
            IncludeUnreadCount = true,          // Hiển thị số tin nhắn chưa đọc
            IncludeRecentMessages = true,       // Load 3 tin nhắn gần đây cho preview
            RecentMessagesCount = 3,
            IncludeMetadata = false,            // Không cần metadata ngay lập tức
            IncludeOnlineStatus = true          // Hiển thị trạng thái online
        };

        return await _conversationService.GetByUserIdAsync(userId, 1, 20, options);
    }

    /// <summary>
    /// Example 3: Admin Panel - Load full data with analytics
    /// Đầy đủ thông tin cho admin/analytics
    /// </summary>
    public async Task<IEnumerable<ConversationResponse>> GetConversationsForAdmin(string userId)
    {
        var options = new ConversationLoadOptions
        {
            IncludeParticipantDetails = true,   // Đầy đủ thông tin users
            IncludeUnreadCount = true,          // Thống kê tin nhắn
            IncludeRecentMessages = true,       // Xem nội dung gần đây
            RecentMessagesCount = 5,
            IncludeMetadata = true,             // Thống kê đầy đủ (total messages, files, etc.)
            IncludeOnlineStatus = true          // Trạng thái hoạt động
        };

        return await _conversationService.GetByUserIdAsync(userId, 1, 50, options);
    }

    /// <summary>
    /// Example 4: Progressive Loading - Load basic first, then enhance
    /// Chiến lược progressive loading cho UX tốt
    /// </summary>
    public async Task<ProgressiveLoadingResult> GetConversationsProgressively(string userId)
    {
        // Step 1: Load basic data first (fast)
        var basicOptions = new ConversationLoadOptions
        {
            IncludeUnreadCount = true,
            IncludeParticipantDetails = false,
            IncludeRecentMessages = false,
            IncludeMetadata = false
        };

        var basicConversations = await _conversationService.GetByUserIdAsync(userId, 1, 20, basicOptions);

        // Step 2: Load enhanced data for visible items
        var enhancedOptions = new ConversationLoadOptions
        {
            IncludeParticipantDetails = true,
            IncludeRecentMessages = true,
            RecentMessagesCount = 2,
            IncludeOnlineStatus = true
        };

        // Chỉ enhance top 5 conversations (visible items)
        var topConversationIds = basicConversations.Take(5).Select(c => c.Id).ToList();
        var enhancedConversations = new List<ConversationResponse>();

        foreach (var conversationId in topConversationIds)
        {
            var enhanced = await _conversationService.GetConversationDetailsAsync(conversationId, enhancedOptions);
            if (enhanced != null)
            {
                enhancedConversations.Add(enhanced);
            }
        }

        return new ProgressiveLoadingResult
        {
            BasicConversations = basicConversations,
            EnhancedConversations = enhancedConversations,
            LoadingStrategy = "Progressive: Basic first, then enhance visible items"
        };
    }

    /// <summary>
    /// Example 5: Conditional Loading - Load based on user preferences
    /// Tùy chỉnh loading theo preferences của user
    /// </summary>
    public async Task<IEnumerable<ConversationResponse>> GetConversationsWithUserPreferences(
        string userId, 
        UserPreferences preferences)
    {
        var options = new ConversationLoadOptions
        {
            IncludeParticipantDetails = preferences.ShowParticipantDetails,
            IncludeUnreadCount = preferences.ShowUnreadBadges,
            IncludeRecentMessages = preferences.ShowMessagePreviews,
            RecentMessagesCount = preferences.PreviewMessageCount,
            IncludeMetadata = preferences.ShowConversationStats,
            IncludeOnlineStatus = preferences.ShowOnlineStatus
        };

        var pageSize = preferences.IsMobile ? 10 : 20;
        return await _conversationService.GetByUserIdAsync(userId, 1, pageSize, options);
    }

    /// <summary>
    /// Example 6: Search with Lazy Loading
    /// Tìm kiếm với lazy loading để tối ưu performance
    /// </summary>
    public async Task<SearchWithLazyResult> SearchConversationsWithLazy(string userId, string searchTerm)
    {
        // First: Get basic search results (fast)
        var allConversations = await _conversationService.GetByUserIdAsync(userId, 1, 100, null);
        
        // Filter by search term (simple implementation)
        var filteredConversations = allConversations
            .Where(c => c.LastMessage?.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        // Then: Enhance search results with additional data
        var enhancedOptions = new ConversationLoadOptions
        {
            IncludeParticipantDetails = true,
            IncludeRecentMessages = true,
            RecentMessagesCount = 5, // More messages for search context
            IncludeMetadata = true,
            IncludeOnlineStatus = true
        };

        var enhancedResults = new List<ConversationResponse>();
        foreach (var conversation in filteredConversations.Take(10)) // Top 10 results
        {
            var enhanced = await _conversationService.GetConversationDetailsAsync(conversation.Id, enhancedOptions);
            if (enhanced != null)
            {
                enhancedResults.Add(enhanced);
            }
        }

        return new SearchWithLazyResult
        {
            SearchTerm = searchTerm,
            TotalMatches = filteredConversations.Count,
            EnhancedResults = enhancedResults,
            PerformanceNote = "Basic search first, then enhanced top results"
        };
    }
}

/// <summary>
/// User preferences for lazy loading customization
/// </summary>
public class UserPreferences
{
    public bool IsMobile { get; set; }
    public bool ShowParticipantDetails { get; set; } = true;
    public bool ShowUnreadBadges { get; set; } = true;
    public bool ShowMessagePreviews { get; set; } = false;
    public int PreviewMessageCount { get; set; } = 3;
    public bool ShowConversationStats { get; set; } = false;
    public bool ShowOnlineStatus { get; set; } = true;
}

/// <summary>
/// Result for progressive loading strategy
/// </summary>
public class ProgressiveLoadingResult
{
    public IEnumerable<ConversationResponse> BasicConversations { get; set; } = new List<ConversationResponse>();
    public IEnumerable<ConversationResponse> EnhancedConversations { get; set; } = new List<ConversationResponse>();
    public string LoadingStrategy { get; set; } = string.Empty;
}

/// <summary>
/// Result for search with lazy loading
/// </summary>
public class SearchWithLazyResult
{
    public string SearchTerm { get; set; } = string.Empty;
    public int TotalMatches { get; set; }
    public IEnumerable<ConversationResponse> EnhancedResults { get; set; } = new List<ConversationResponse>();
    public string PerformanceNote { get; set; } = string.Empty;
}