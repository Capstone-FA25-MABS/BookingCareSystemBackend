namespace BookingCare.Shared.Common.Interfaces;

/// <summary>
/// Interface for services that support avatar operations
/// </summary>
public interface IAvatarService
{
    /// <summary>
    /// Check if entity exists by account ID
    /// </summary>
    Task<bool> EntityExistsByAccountIdAsync(Guid accountId);

    /// <summary>
    /// Get entity avatar URL by account ID
    /// </summary>
    Task<string?> GetAvatarUrlByAccountIdAsync(Guid accountId);

    /// <summary>
    /// Update entity avatar URL by account ID
    /// </summary>
    Task<bool> UpdateAvatarUrlByAccountIdAsync(Guid accountId, string avatarUrl);
}

