namespace BookingCare.Services.Communication.Enums;

/// <summary>
/// Loại tag/label cho cuộc hội thoại
/// </summary>
public enum ConversationTagType
{
    /// <summary>
    /// Tag mặc định/tùy chỉnh do người dùng tạo
    /// </summary>
    Custom = 0,

    /// <summary>
    /// Tag được hệ thống tự động gán (ví dụ: urgent, important)
    /// </summary>
    System = 1,

    /// <summary>
    /// Tag cho công việc
    /// </summary>
    Work = 2,

    /// <summary>
    /// Tag cho cá nhân
    /// </summary>
    Personal = 3,

    /// <summary>
    /// Tag cho khách hàng VIP
    /// </summary>
    VIP = 4,

    /// <summary>
    /// Tag cho bạn bè
    /// </summary>
    Friends = 5,

    /// <summary>
    /// Tag cho gia đình
    /// </summary>
    Family = 6,

    /// <summary>
    /// Tag cho nhóm
    /// </summary>
    Group = 7,
}
