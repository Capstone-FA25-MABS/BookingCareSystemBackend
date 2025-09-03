namespace BookingCare.Services.Communication.Enums;

/// <summary>
/// Represents the type of a message.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Text message
    /// </summary>
    Text,
    
    /// <summary>
    /// Image message with attachment
    /// </summary>
    Image,
    
    /// <summary>
    /// File message with attachment
    /// </summary>
    File,
    
    /// <summary>
    /// Video message with attachment
    /// </summary>
    Video,
    
    /// <summary>
    /// Audio message with attachment
    /// </summary>
    Audio,
    
    /// <summary>
    /// System message (auto-generated)
    /// </summary>
    System,

    /// <summary>
    /// Voice recording message
    /// </summary>
    VoiceNote,

    /// <summary>
    /// Location sharing message
    /// </summary>
    Location,

    /// <summary>
    /// Contact sharing message
    /// </summary>
    Contact,

    /// <summary>
    /// Sticker/emoji message
    /// </summary>
    Sticker,

    /// <summary>
    /// GIF animation message
    /// </summary>
    Gif,

    /// <summary>
    /// Poll/survey message
    /// </summary>
    Poll,

    /// <summary>
    /// Event/calendar message
    /// </summary>
    Event,

    /// <summary>
    /// Link preview message
    /// </summary>
    LinkPreview,

    /// <summary>
    /// Rich text message with formatting
    /// </summary>
    RichText,

    /// <summary>
    /// Quoted/reply message
    /// </summary>
    Reply,

    /// <summary>
    /// Forwarded message
    /// </summary>
    Forward
}