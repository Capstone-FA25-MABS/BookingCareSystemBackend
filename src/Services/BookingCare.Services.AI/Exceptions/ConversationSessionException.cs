namespace BookingCare.Services.AI.Exceptions;

/// <summary>
/// Exception thrown when conversation session operations fail
/// </summary>
public class ConversationSessionException : Exception
{
    public Guid? SessionId { get; }
    public Guid? UserId { get; }

    public ConversationSessionException(string message) : base(message)
    {
    }

    public ConversationSessionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ConversationSessionException(string message, Guid? sessionId, Guid? userId = null)
        : base(message)
    {
        SessionId = sessionId;
        UserId = userId;
    }

    public ConversationSessionException(string message, Guid? sessionId, Guid? userId, Exception innerException)
        : base(message, innerException)
    {
        SessionId = sessionId;
        UserId = userId;
    }
}
