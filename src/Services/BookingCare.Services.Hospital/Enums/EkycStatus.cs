namespace BookingCare.Services.Hospital.Enums;

/// <summary>
/// Represents the status of eKYC verification
/// </summary>
public enum EkycStatus
{
    /// <summary>
    /// eKYC not started
    /// </summary>
    NOT_STARTED,

    /// <summary>
    /// eKYC in progress
    /// </summary>
    IN_PROGRESS,

    /// <summary>
    /// eKYC verification successful
    /// </summary>
    VERIFIED,

    /// <summary>
    /// eKYC verification failed
    /// </summary>
    FAILED
}
