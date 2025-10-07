using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Service interface for BankAccount
/// </summary>
public interface IBankAccountService
{
    /// <summary>
    /// Get bank account by ID
    /// </summary>
    Task<BankAccountResponse?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all bank accounts of a user
    /// </summary>
    Task<IEnumerable<BankAccountResponse>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Get bank accounts of a user with pagination
    /// </summary>
    Task<PagedResult<BankAccountResponse>> GetPagedByUserIdAsync(GetBankAccountsRequest request);

    /// <summary>
    /// Get the default bank account of a user
    /// </summary>
    Task<BankAccountResponse?> GetDefaultByUserIdAsync(Guid userId);

    /// <summary>
    /// Create a new bank account
    /// </summary>
    Task<BankAccountResponse> CreateAsync(CreateBankAccountRequest request);

    /// <summary>
    /// Update bank account
    /// </summary>
    Task<BankAccountResponse> UpdateAsync(UpdateBankAccountRequest request);

    /// <summary>
    /// Smart delete or deactivate bank account
    /// If the bank account is used in RefundHistories -> only deactivate
    /// If not used -> permanently delete
    /// </summary>
    Task<BankAccountDeleteResult> SmartDeleteAsync(Guid id);


    /// <summary>
    /// Set bank account as default
    /// </summary>
    Task<BankAccountResponse> SetAsDefaultAsync(Guid bankAccountId);

    /// <summary>
    /// Activate/deactivate bank account
    /// </summary>
    Task<BankAccountResponse> ToggleActiveStatusAsync(Guid bankAccountId);
}