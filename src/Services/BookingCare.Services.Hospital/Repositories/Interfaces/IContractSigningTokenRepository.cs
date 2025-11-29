using BookingCare.Services.Hospital.Models.Entities;

namespace BookingCare.Services.Hospital.Repositories.Interfaces;

/// <summary>
/// Repository interface for contract signing token operations
/// </summary>
public interface IContractSigningTokenRepository
{
    /// <summary>
    /// Get token by ID
    /// </summary>
    Task<ContractSigningTokenEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get token by token string
    /// </summary>
    Task<ContractSigningTokenEntity?> GetByTokenAsync(string token);

    /// <summary>
    /// Get token by registration ID
    /// </summary>
    Task<ContractSigningTokenEntity?> GetByRegistrationIdAsync(Guid registrationId);

    /// <summary>
    /// Get active (unused and not expired) token by registration ID
    /// </summary>
    Task<ContractSigningTokenEntity?> GetActiveTokenByRegistrationIdAsync(Guid registrationId);

    /// <summary>
    /// Create a new token
    /// </summary>
    Task<ContractSigningTokenEntity> CreateAsync(ContractSigningTokenEntity token);

    /// <summary>
    /// Mark token as used
    /// </summary>
    Task<ContractSigningTokenEntity> MarkAsUsedAsync(Guid tokenId, string ipAddress, string userAgent);

    /// <summary>
    /// Delete expired tokens (cleanup)
    /// </summary>
    Task<int> DeleteExpiredTokensAsync();

    /// <summary>
    /// Invalidate all tokens for a registration (when regenerating)
    /// </summary>
    Task InvalidateTokensByRegistrationIdAsync(Guid registrationId);
}
