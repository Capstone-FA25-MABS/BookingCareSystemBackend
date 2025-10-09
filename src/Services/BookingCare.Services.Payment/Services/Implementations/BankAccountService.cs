using AutoMapper;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Payment.Services.Implementations;

/// <summary>
/// Service implementation for BankAccount
/// </summary>
public class BankAccountService : BaseService, IBankAccountService
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IMapper _mapper;

    public BankAccountService(
        IBankAccountRepository bankAccountRepository,
        IMapper mapper,
        ILogger<BankAccountService> logger) : base(logger)
    {
        _bankAccountRepository = bankAccountRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Get bank account by ID - Simple read operation
    /// </summary>
    public async Task<BankAccountResponse?> GetByIdAsync(Guid id)
    {
        var bankAccount = await _bankAccountRepository.GetByIdAsync(id);
        return bankAccount != null ? _mapper.Map<BankAccountResponse>(bankAccount) : null;
    }

    /// <summary>
    /// Get all bank accounts of a user - Simple read operation
    /// </summary>
    public async Task<IEnumerable<BankAccountResponse>> GetByUserIdAsync(Guid userId)
    {
        var bankAccounts = await _bankAccountRepository.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<BankAccountResponse>>(bankAccounts);
    }

    /// <summary>
    /// Get bank accounts of a user with pagination - Simple read operation
    /// </summary>
    public async Task<PagedResult<BankAccountResponse>> GetPagedByUserIdAsync(GetBankAccountsRequest request)
    {
        var pagedResult = await _bankAccountRepository.GetPagedByUserIdAsync(
            request.UserId, request.Page, request.PageSize, request.ActiveOnly);

        var mappedItems = _mapper.Map<List<BankAccountResponse>>(pagedResult.Items);

        return new PagedResult<BankAccountResponse>
        {
            Items = mappedItems,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };
    }

    /// <summary>
    /// Get the default bank account of a user - Simple read operation
    /// </summary>
    public async Task<BankAccountResponse?> GetDefaultByUserIdAsync(Guid userId)
    {
        var defaultAccount = await _bankAccountRepository.GetDefaultByUserIdAsync(userId);
        return defaultAccount != null ? _mapper.Map<BankAccountResponse>(defaultAccount) : null;
    }

    /// <summary>
    /// Create a new bank account or reactivate an old inactive account if it exists
    /// </summary>
    public async Task<BankAccountResponse> CreateAsync(CreateBankAccountRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating new bank account for User: {UserId}, Account: {AccountNumber}@{BankCode}",
                null, request.UserId, request.AccountNumber, request.BankCode);

            // Validation
            ValidateCreateRequest(request);

            // Check for existing account and handle accordingly
            var existingAccount = await _bankAccountRepository.FindByAccountNumberAndUserAsync(
                request.AccountNumber, request.BankCode, request.UserId);

            if (existingAccount != null)
            {
                return await HandleExistingAccount(existingAccount, request);
            }

            // Create completely new bank account
            return await CreateNewBankAccount(request);
        }, "CreateBankAccount");
    }

    /// <summary>
    /// Update bank account
    /// </summary>
    public async Task<BankAccountResponse> UpdateAsync(UpdateBankAccountRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating bank account with ID: {Id}", null, request.Id);

            ValidateRequired(request, nameof(request));

            var existing = await GetExistingBankAccount(request.Id);

            // Validate account number uniqueness if changed
            await ValidateAccountNumberUniqueness(request, existing);

            // Update fields
            UpdateBankAccountFields(request, existing);

            // Handle default setting
            await HandleDefaultSetting(request, existing);

            var updated = await _bankAccountRepository.UpdateAsync(existing);

            LogInfo("Bank account updated successfully with ID: {Id}", null, updated.Id);
            return _mapper.Map<BankAccountResponse>(updated);
        }, "UpdateBankAccount");
    }

    /// <summary>
    /// Smart delete or deactivate bank account
    /// </summary>
    public async Task<BankAccountDeleteResult> SmartDeleteAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Performing Smart Delete for bank account with ID: {Id}", null, id);

            var existing = await GetExistingBankAccountForDelete(id);

            // Check if can delete default account
            await ValidateDefaultAccountDeletion(existing);

            // Check refund histories and handle accordingly
            var hasRefundHistories = await _bankAccountRepository.HasRefundHistoriesAsync(id);

            if (hasRefundHistories)
            {
                return await DeactivateBankAccount(existing);
            }
            else
            {
                return await FullyDeleteBankAccount(id);
            }
        }, "SmartDeleteBankAccount");
    }

    /// <summary>
    /// Set bank account as default
    /// </summary>
    public async Task<BankAccountResponse> SetAsDefaultAsync(Guid bankAccountId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Setting bank account as default with ID: {Id}", null, bankAccountId);

            var existing = await _bankAccountRepository.GetByIdAsync(bankAccountId);
            if (existing == null)
            {
                throw new NotFoundException("BankAccount", bankAccountId);
            }

            if (!existing.IsActive)
            {
                throw new InvalidOperationException("Cannot set an inactive account as default");
            }

            await _bankAccountRepository.SetAsDefaultAsync(bankAccountId, existing.UserId);

            // Get again to have the latest data
            var updated = await _bankAccountRepository.GetByIdAsync(bankAccountId);

            LogInfo("Bank account set as default successfully with ID: {Id}", null, bankAccountId);
            return _mapper.Map<BankAccountResponse>(updated!);
        }, "SetAsDefaultBankAccount");
    }

    /// <summary>
    /// Activate/deactivate bank account
    /// </summary>
    public async Task<BankAccountResponse> ToggleActiveStatusAsync(Guid bankAccountId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Changing bank account status with ID: {Id}", null, bankAccountId);

            var existing = await _bankAccountRepository.GetByIdAsync(bankAccountId);
            if (existing == null)
            {
                throw new NotFoundException("BankAccount", bankAccountId);
            }

            // Do not allow deactivating the only default account
            if (existing.IsDefault && existing.IsActive)
            {
                var totalActiveAccounts = (await _bankAccountRepository.GetByUserIdAsync(existing.UserId))
                    .Count(x => x.IsActive);

                if (totalActiveAccounts <= 1)
                {
                    throw new InvalidOperationException("Cannot deactivate the only default account");
                }

                // Remove default before deactivating
                existing.IsDefault = false;
            }

            existing.IsActive = !existing.IsActive;
            var updated = await _bankAccountRepository.UpdateAsync(existing);

            LogInfo("Bank account status changed successfully with ID: {Id}, Active: {IsActive}", null, bankAccountId, updated.IsActive);
            return _mapper.Map<BankAccountResponse>(updated);
        }, "ToggleBankAccountStatus");
    }

    #region Private Helper Methods

    /// <summary>
    /// Validate create request
    /// </summary>
    private static void ValidateCreateRequest(CreateBankAccountRequest request)
    {
        ValidateRequired(request, nameof(request));
        ValidateRequiredString(request.BankCode, nameof(request.BankCode));
        ValidateRequiredString(request.BankName, nameof(request.BankName));
        ValidateRequiredString(request.AccountNumber, nameof(request.AccountNumber));
        ValidateRequiredString(request.AccountName, nameof(request.AccountName));
    }

    /// <summary>
    /// Handle existing account scenario
    /// </summary>
    private async Task<BankAccountResponse> HandleExistingAccount(BankAccountEntity existingAccount, CreateBankAccountRequest request)
    {
        if (existingAccount.IsActive)
        {
            throw new ConflictException($"Bank account {request.AccountNumber} at {request.BankCode} already exists and is active");
        }

        return await ReactivateInactiveAccount(existingAccount, request);
    }

    /// <summary>
    /// Reactivate inactive account
    /// </summary>
    private async Task<BankAccountResponse> ReactivateInactiveAccount(BankAccountEntity existingAccount, CreateBankAccountRequest request)
    {
        LogInfo("Found inactive bank account, reactivating: {Id}", null, existingAccount.Id);

        // Update info from request
        existingAccount.BankName = request.BankName;
        existingAccount.AccountName = request.AccountName;
        existingAccount.IsActive = true;

        // Handle default logic for reactivation
        await HandleDefaultLogicForReactivation(existingAccount, request);

        var reactivated = await _bankAccountRepository.UpdateAsync(existingAccount);

        LogInfo("Bank account reactivated successfully: {Id}", null, reactivated.Id);
        return _mapper.Map<BankAccountResponse>(reactivated);
    }

    /// <summary>
    /// Handle default logic for reactivation
    /// </summary>
    private async Task HandleDefaultLogicForReactivation(BankAccountEntity existingAccount, CreateBankAccountRequest request)
    {
        var existingActiveAccountsCount = (await _bankAccountRepository.GetByUserIdAsync(request.UserId))
            .Count(x => x.IsActive);
        var shouldBeDefaultForReactive = request.IsDefault || existingActiveAccountsCount == 0;

        if (shouldBeDefaultForReactive)
        {
            existingAccount.IsDefault = true;
            // Remove default from other accounts if needed
            if (existingActiveAccountsCount > 0)
            {
                await _bankAccountRepository.SetAsDefaultAsync(existingAccount.Id, request.UserId);
            }
        }
    }

    /// <summary>
    /// Create completely new bank account
    /// </summary>
    private async Task<BankAccountResponse> CreateNewBankAccount(CreateBankAccountRequest request)
    {
        LogInfo("Creating a completely new bank account for User: {UserId}", null, request.UserId);

        var existingAccountsCount = await _bankAccountRepository.CountByUserIdAsync(request.UserId);
        var shouldBeDefaultForNew = request.IsDefault || existingAccountsCount == 0;

        var entity = _mapper.Map<BankAccountEntity>(request);
        entity.IsDefault = shouldBeDefaultForNew;
        entity.IsActive = true; // Ensure new account is always active

        // If set as default, remove default from other accounts
        if (shouldBeDefaultForNew && existingAccountsCount > 0)
        {
            await _bankAccountRepository.SetAsDefaultAsync(entity.Id, request.UserId);
        }

        var created = await _bankAccountRepository.CreateAsync(entity);

        LogInfo("Bank account created successfully with ID: {Id}", null, created.Id);
        return _mapper.Map<BankAccountResponse>(created);
    }

    /// <summary>
    /// Get existing bank account with validation
    /// </summary>
    private async Task<BankAccountEntity> GetExistingBankAccount(Guid id)
    {
        var existing = await _bankAccountRepository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new NotFoundException("BankAccount", id);
        }
        return existing;
    }

    /// <summary>
    /// Validate account number uniqueness for update
    /// </summary>
    private async Task ValidateAccountNumberUniqueness(UpdateBankAccountRequest request, BankAccountEntity existing)
    {
        if (!string.IsNullOrEmpty(request.AccountNumber) && request.AccountNumber != existing.AccountNumber)
        {
            // Use new BankCode if provided, otherwise use current BankCode
            var bankCodeToCheck = !string.IsNullOrEmpty(request.BankCode) ? request.BankCode : existing.BankCode;

            // Check globally (not just for user)
            var accountExistsGlobally = await _bankAccountRepository.AccountNumberExistsAsync(
                request.AccountNumber, bankCodeToCheck, existing.Id);

            if (accountExistsGlobally)
            {
                throw new ConflictException($"Bank account {request.AccountNumber} at {bankCodeToCheck} is already used by another user");
            }
        }
    }

    /// <summary>
    /// Update bank account fields from request
    /// </summary>
    private static void UpdateBankAccountFields(UpdateBankAccountRequest request, BankAccountEntity existing)
    {
        if (!string.IsNullOrEmpty(request.BankCode))
            existing.BankCode = request.BankCode;

        if (!string.IsNullOrEmpty(request.BankName))
            existing.BankName = request.BankName;

        if (!string.IsNullOrEmpty(request.AccountNumber))
            existing.AccountNumber = request.AccountNumber;

        if (!string.IsNullOrEmpty(request.AccountName))
            existing.AccountName = request.AccountName;

        if (request.IsActive.HasValue)
            existing.IsActive = request.IsActive.Value;
    }

    /// <summary>
    /// Handle default setting logic
    /// </summary>
    private async Task HandleDefaultSetting(UpdateBankAccountRequest request, BankAccountEntity existing)
    {
        if (request.IsDefault.HasValue && request.IsDefault.Value && !existing.IsDefault)
        {
            await _bankAccountRepository.SetAsDefaultAsync(existing.Id, existing.UserId);
        }
        else if (request.IsDefault.HasValue)
        {
            existing.IsDefault = request.IsDefault.Value;
        }
    }

    /// <summary>
    /// Get existing bank account for delete operation with validation
    /// </summary>
    private async Task<BankAccountEntity> GetExistingBankAccountForDelete(Guid id)
    {
        var existing = await _bankAccountRepository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new ArgumentException($"Bank account with ID {id} not found");
        }
        return existing;
    }

    /// <summary>
    /// Validate if default account can be deleted
    /// </summary>
    private async Task ValidateDefaultAccountDeletion(BankAccountEntity existing)
    {
        if (existing.IsDefault)
        {
            var totalAccounts = await _bankAccountRepository.CountByUserIdAsync(existing.UserId);
            if (totalAccounts > 1)
            {
                throw new InvalidOperationException("Cannot delete default account when other accounts exist. Please set another account as default first.");
            }
        }
    }

    /// <summary>
    /// Deactivate bank account (soft delete)
    /// </summary>
    private async Task<BankAccountDeleteResult> DeactivateBankAccount(BankAccountEntity existing)
    {
        LogInfo("Bank account {Id} has refund histories, deactivating instead of deleting", null, existing.Id);

        existing.IsActive = false;
        if (existing.IsDefault)
        {
            existing.IsDefault = false; // Remove default if deactivating
        }

        var updated = await _bankAccountRepository.UpdateAsync(existing);
        var updatedResponse = _mapper.Map<BankAccountResponse>(updated);

        LogInfo("Bank account {Id} has been deactivated due to refund histories", null, existing.Id);

        return BankAccountDeleteResult.Deactivated(
            updatedResponse,
            0, // Can add count if needed
            "Bank account has been deactivated due to links with refund history. Cannot be fully deleted."
        );
    }

    /// <summary>
    /// Fully delete bank account from database
    /// </summary>
    private async Task<BankAccountDeleteResult> FullyDeleteBankAccount(Guid id)
    {
        LogInfo("Bank account {Id} has no refund histories, performing full delete", null, id);

        var result = await _bankAccountRepository.DeleteAsync(id);

        if (result)
        {
            LogInfo("Bank account {Id} has been fully deleted successfully", null, id);
            return BankAccountDeleteResult.Deleted("Bank account has been fully deleted successfully");
        }
        else
        {
            LogWarning("Unable to delete bank account {Id}", null, id);
            return BankAccountDeleteResult.Failed("Unable to delete bank account");
        }
    }

    #endregion
}