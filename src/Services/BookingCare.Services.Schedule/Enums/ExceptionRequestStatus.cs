namespace BookingCare.Services.Schedule.Enums;

/// <summary>
/// Represents the status of an exception request (for approval workflow)
/// </summary>
public enum ExceptionRequestStatus
{
    /// <summary>
    /// The exception request is pending approval from staff
    /// </summary>
    PENDING = 1,

    /// <summary>
    /// The exception request has been approved by staff
    /// </summary>
    APPROVED = 2,

    /// <summary>
    /// The exception request has been rejected by staff
    /// </summary>
    REJECTED = 3,

    /// <summary>
    /// The exception request has been cancelled by the requester
    /// </summary>
    CANCELLED = 4
}
