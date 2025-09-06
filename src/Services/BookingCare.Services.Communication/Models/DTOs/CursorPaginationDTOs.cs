namespace BookingCare.Services.Communication.Models.DTOs;

/// <summary>
/// Response cho cursor-based pagination (infinite scroll friendly)
/// </summary>
public class CursorPaginatedResponse<T>
{
    /// <summary>
    /// Danh sách items
    /// </summary>
    public List<T> Data { get; set; } = new();

    /// <summary>
    /// Cursor để load trang trước đó (older items)
    /// </summary>
    public string? PreviousCursor { get; set; }

    /// <summary>
    /// Cursor để load trang kế tiếp (newer items)
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Có trang trước không
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Có trang kế tiếp không
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Số lượng items trong trang hiện tại
    /// </summary>
    public int Count => Data.Count;

    /// <summary>
    /// Limit đã request
    /// </summary>
    public int Limit { get; set; }
}

/// <summary>
/// Request cho cursor-based pagination
/// </summary>
public class CursorPaginationRequest
{
    /// <summary>
    /// ID của conversation
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Cursor để load items trước đó (older)
    /// </summary>
    public string? Before { get; set; }

    /// <summary>
    /// Cursor để load items sau đó (newer)
    /// </summary>
    public string? After { get; set; }

    /// <summary>
    /// Số lượng items cần load
    /// </summary>
    public int Limit { get; set; } = 50;
}