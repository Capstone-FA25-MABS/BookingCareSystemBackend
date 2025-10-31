namespace BookingCare.Services.Communication.Enums;

/// <summary>
/// Simplified Message Type for API requests - only what clients need to specify
/// </summary>
public enum SimpleMessageType
{
    /// <summary>
    /// Text message only
    /// </summary>
    Text = 0,

    /// <summary>
    /// Any file attachment (will be auto-categorized by server)
    /// </summary>
    File = 1
}

/// <summary>
/// Detailed Message Type used internally for storage and processing
/// Auto-detected from file content type and extension
/// </summary>
public enum DetailedMessageType
{
    /// <summary>
    /// Text message
    /// </summary>
    Text = 0,

    /// <summary>
    /// Image file (detected: image/*)
    /// </summary>
    Image = 1,

    /// <summary>
    /// Video file (detected: video/*)
    /// </summary>
    Video = 2,

    /// <summary>
    /// Audio file (detected: audio/*)
    /// </summary>
    Audio = 3,

    /// <summary>
    /// Voice recording (detected: audio/* with specific patterns)
    /// </summary>
    VoiceNote = 4,

    /// <summary>
    /// Document file (detected: application/*, text/*)
    /// </summary>
    Document = 5,

    /// <summary>
    /// GIF animation (detected: image/gif)
    /// </summary>
    Gif = 6,

    /// <summary>
    /// Archive file (detected: zip, rar, etc.)
    /// </summary>
    Archive = 7,

    /// <summary>
    /// Other/Unknown file type
    /// </summary>
    Other = 99
}