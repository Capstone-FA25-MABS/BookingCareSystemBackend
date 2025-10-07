using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Repository interface for BankAccount
/// </summary>
public interface IBankAccountRepository
{
    /// <summary>
    /// Get bank account by ID
    /// </summary>
    Task<BankAccountEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all bank accounts of a user
    /// </summary>
    Task<IEnumerable<BankAccountEntity>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Get bank accounts of a user with pagination
    /// </summary>
    Task<PagedResult<BankAccountEntity>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, bool? activeOnly = null);

    /// <summary>
    /// Get the default bank account of a user
    /// </summary>
    Task<BankAccountEntity?> GetDefaultByUserIdAsync(Guid userId);

    /// <summary>
    /// Check if an account number already exists
    /// </summary>
    Task<bool> AccountNumberExistsAsync(string accountNumber, string bankCode, Guid? excludeId = null);

    /// <summary>
    /// Check if an account number exists for a specific user
    /// </summary>
    Task<bool> AccountNumberExistsForUserAsync(string accountNumber, string bankCode, Guid userId, Guid? excludeId = null);

    /// <summary>
    /// Find bank account by accountNumber, bankCode and userId (includes inactive)
    /// </summary>
    Task<BankAccountEntity?> FindByAccountNumberAndUserAsync(string accountNumber, string bankCode, Guid userId);

    /// <summary>
    /// Check if bank account is used in RefundHistories
    /// </summary>
    Task<bool> HasRefundHistoriesAsync(Guid bankAccountId);

    /// <summary>
    /// Create a new bank account
    /// </summary>
    Task<BankAccountEntity> CreateAsync(BankAccountEntity entity);

    /// <summary>
    /// Update bank account
    /// </summary>
    Task<BankAccountEntity> UpdateAsync(BankAccountEntity entity);

    /// <summary>
    /// Delete bank account
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Set account as default and unset other accounts of the user
    /// </summary>
    Task SetAsDefaultAsync(Guid bankAccountId, Guid userId);

    /// <summary>
    /// Count bank accounts of a user
    /// </summary>
    Task<int> CountByUserIdAsync(Guid userId);
}