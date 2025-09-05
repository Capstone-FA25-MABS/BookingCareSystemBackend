namespace BookingCare.Services.Communication.Models.DTOs;

/// <summary>
/// Options cho vi?c load d? li?u conversation (lazy loading)
/// </summary>
public class ConversationLoadOptions
{
    /// <summary>
    /// Load chi ti?t thông tin participants t? User Service
    /// </summary>
    public bool IncludeParticipantDetails { get; set; } = false;

    /// <summary>
    /// Load s? tin nh?n ch?a ??c
    /// </summary>
    public bool IncludeUnreadCount { get; set; } = true;

    /// <summary>
    /// Load m?t s? tin nh?n g?n ?ây
    /// </summary>
    public bool IncludeRecentMessages { get; set; } = false;

    /// <summary>
    /// S? l??ng tin nh?n g?n ?ây c?n load
    /// </summary>
    public int RecentMessagesCount { get; set; } = 5;

    /// <summary>
    /// Load metadata b? sung (t?ng s? tin nh?n, files, etc.)
    /// </summary>
    public bool IncludeMetadata { get; set; } = false;

    /// <summary>
    /// Load thông tin online status c?a participants
    /// </summary>
    public bool IncludeOnlineStatus { get; set; } = false;
}