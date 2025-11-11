using System;

namespace BookingCare.Services.AI.Exceptions;

/// <summary>
/// Exception thrown when symptom analysis operations fail
/// </summary>
public class SymptomAnalysisException : Exception
{
    public Guid? SessionId { get; }
    public Guid? UserId { get; }

    public SymptomAnalysisException(string message)
        : base(message)
    {
    }

    public SymptomAnalysisException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SymptomAnalysisException(string message, Guid? sessionId, Guid? userId)
        : base(message)
    {
        SessionId = sessionId;
        UserId = userId;
    }

    public SymptomAnalysisException(string message, Guid? sessionId, Guid? userId, Exception innerException)
        : base(message, innerException)
    {
        SessionId = sessionId;
        UserId = userId;
    }
}
