namespace BookingCare.Services.Communication.Models.DTOs;

/// <summary>
/// Request ?? clear cache cho multiple accounts
/// </summary>
public class ClearMultipleAccountsCacheRequest
{
    /// <summary>
    /// Danh sách account IDs c?n clear cache
    /// </summary>
    public IEnumerable<string> AccountIds { get; set; } = new List<string>();
}

/// <summary>
/// Request ?? debug participant enrichment process
/// </summary>
public class DebugParticipantEnrichmentRequest
{
    /// <summary>
    /// Danh sách account IDs ?? test
    /// </summary>
    public IEnumerable<string> AccountIds { get; set; } = new List<string>();
}