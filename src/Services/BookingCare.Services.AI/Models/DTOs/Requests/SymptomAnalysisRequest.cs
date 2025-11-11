using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.AI.Models.DTOs.Requests;

/// <summary>
/// Request model for symptom analysis
/// </summary>
public class SymptomAnalysisRequest
{
    /// <summary>
    /// Session ID to track conversation history (optional for new conversations)
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// User ID if authenticated (optional)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// User's message/symptom description
    /// </summary>
    [Required(ErrorMessage = "Message is required")]
    [MaxLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// User's location context for finding nearby doctors/hospitals
    /// </summary>
    public LocationContext? Location { get; set; }

    /// <summary>
    /// Previous conversation history for context (optional)
    /// Will be loaded from DB if SessionId is provided
    /// </summary>
    public List<ConversationMessage>? ConversationHistory { get; set; }
}

/// <summary>
/// User's location information
/// </summary>
public class LocationContext
{
    public string? ProvinceId { get; set; }
    public string? DistrictId { get; set; }

    [Required]
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Single message in conversation
/// </summary>
public class ConversationMessage
{
    [Required]
    public string Role { get; set; } = string.Empty; // "user" or "ai"

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Suggestions (doctors/hospitals) associated with this AI message (optional)
    /// </summary>
    public object? Suggestions { get; set; } // Store as JSON object for flexibility
}


