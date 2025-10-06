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
/// Service implementation cho BankAccount
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
    /// L?y bank account theo ID - Thao tác ??c ??n gi?n
    /// </summary>
    public async Task<BankAccountResponse?> GetByIdAsync(Guid id)
    {
        var bankAccount = await _bankAccountRepository.GetByIdAsync(id);
        return bankAccount != null ? _mapper.Map<BankAccountResponse>(bankAccount) : null;
    }

    /// <summary>
    /// L?y t?t c? bank accounts c?a user - Thao tác ??c ??n gi?n
    /// </summary>
    public async Task<IEnumerable<BankAccountResponse>> GetByUserIdAsync(Guid userId)
    {
        var bankAccounts = await _bankAccountRepository.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<BankAccountResponse>>(bankAccounts);
    }

    /// <summary>
    /// L?y bank accounts c?a user v?i phân trang - Thao tác ??c ??n gi?n
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
    /// L?y bank account m?c ??nh c?a user - Thao tác ??c ??n gi?n
    /// </summary>
    public async Task<BankAccountResponse?> GetDefaultByUserIdAsync(Guid userId)
    {
        var defaultAccount = await _bankAccountRepository.GetDefaultByUserIdAsync(userId);
        return defaultAccount != null ? _mapper.Map<BankAccountResponse>(defaultAccount) : null;
    }

    /// <summary>
    /// T?o bank account m?i
    /// </summary>
    public async Task<BankAccountResponse> CreateAsync(CreateBankAccountRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("?ang t?o bank account m?i cho User: {UserId}", null, request.UserId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.BankCode, nameof(request.BankCode));
            ValidateRequiredString(request.BankName, nameof(request.BankName));
            ValidateRequiredString(request.AccountNumber, nameof(request.AccountNumber));
            ValidateRequiredString(request.AccountName, nameof(request.AccountName));

            // Ki?m tra s? tài kho?n ?ã t?n t?i ch?a
            var accountExists = await _bankAccountRepository.AccountNumberExistsAsync(request.AccountNumber, request.BankCode);
            if (accountExists)
            {
                throw new ConflictException($"Tài kho?n ngân hàng {request.AccountNumber} t?i {request.BankCode} ?ã t?n t?i");
            }

            // N?u ?ây là tài kho?n ??u tiên c?a user ho?c ???c ??t làm m?c ??nh
            var existingAccountsCount = await _bankAccountRepository.CountByUserIdAsync(request.UserId);
            var shouldBeDefault = request.IsDefault || existingAccountsCount == 0;

            // T?o entity
            var entity = _mapper.Map<BankAccountEntity>(request);
            entity.IsDefault = shouldBeDefault;

            // N?u ??t làm m?c ??nh, c?n b? m?c ??nh các tài kho?n khác
            if (shouldBeDefault && existingAccountsCount > 0)
            {
                await _bankAccountRepository.SetAsDefaultAsync(entity.Id, request.UserId);
            }

            var created = await _bankAccountRepository.CreateAsync(entity);

            LogInfo("Bank account ???c t?o thành công v?i ID: {Id}", null, created.Id);
            return _mapper.Map<BankAccountResponse>(created);
        }, "CreateBankAccount");
    }

    /// <summary>
    /// C?p nh?t bank account
    /// </summary>
    public async Task<BankAccountResponse> UpdateAsync(UpdateBankAccountRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("?ang c?p nh?t bank account v?i ID: {Id}", null, request.Id);

            ValidateRequired(request, nameof(request));

            var existing = await _bankAccountRepository.GetByIdAsync(request.Id);
            if (existing == null)
            {
                throw new NotFoundException("BankAccount", request.Id);
            }

            // Ki?m tra uniqueness n?u c?p nh?t account number
            if (!string.IsNullOrEmpty(request.AccountNumber) && request.AccountNumber != existing.AccountNumber)
            {
                // S? d?ng BankCode m?i n?u có, n?u không dùng BankCode hi?n t?i
                var bankCodeToCheck = !string.IsNullOrEmpty(request.BankCode) ? request.BankCode : existing.BankCode;

                var accountExists = await _bankAccountRepository.AccountNumberExistsAsync(
                    request.AccountNumber, bankCodeToCheck, existing.Id);

                if (accountExists)
                {
                    throw new ConflictException($"Tài kho?n ngân hàng {request.AccountNumber} t?i {bankCodeToCheck} ?ã t?n t?i");
                }
            }

            // C?p nh?t các tr??ng ???c cung c?p
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

            // X? lý vi?c ??t làm m?c ??nh
            if (request.IsDefault.HasValue && request.IsDefault.Value && !existing.IsDefault)
            {
                await _bankAccountRepository.SetAsDefaultAsync(existing.Id, existing.UserId);
            }
            else if (request.IsDefault.HasValue)
            {
                existing.IsDefault = request.IsDefault.Value;
            }

            var updated = await _bankAccountRepository.UpdateAsync(existing);

            LogInfo("Bank account ???c c?p nh?t thành công v?i ID: {Id}", null, updated.Id);
            return _mapper.Map<BankAccountResponse>(updated);
        }, "UpdateBankAccount");
    }

    /// <summary>
    /// Xóa bank account
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("?ang xóa bank account v?i ID: {Id}", null, id);

            var existing = await _bankAccountRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return false;
            }

            // Không cho phép xóa tài kho?n m?c ??nh n?u còn tài kho?n khác
            if (existing.IsDefault)
            {
                var totalAccounts = await _bankAccountRepository.CountByUserIdAsync(existing.UserId);
                if (totalAccounts > 1)
                {
                    throw new InvalidOperationException("Không th? xóa tài kho?n m?c ??nh khi còn tài kho?n khác. Hãy ??t tài kho?n khác làm m?c ??nh tr??c.");
                }
            }

            var result = await _bankAccountRepository.DeleteAsync(id);

            LogInfo("Bank account ???c xóa thành công v?i ID: {Id}", null, id);
            return result;
        }, "DeleteBankAccount");
    }

    /// <summary>
    /// ??t bank account làm m?c ??nh
    /// </summary>
    public async Task<BankAccountResponse> SetAsDefaultAsync(Guid bankAccountId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("?ang ??t bank account làm m?c ??nh v?i ID: {Id}", null, bankAccountId);

            var existing = await _bankAccountRepository.GetByIdAsync(bankAccountId);
            if (existing == null)
            {
                throw new NotFoundException("BankAccount", bankAccountId);
            }

            if (!existing.IsActive)
            {
                throw new InvalidOperationException("Không th? ??t tài kho?n không ho?t ??ng làm m?c ??nh");
            }

            await _bankAccountRepository.SetAsDefaultAsync(bankAccountId, existing.UserId);

            // L?y l?i ?? có d? li?u m?i nh?t
            var updated = await _bankAccountRepository.GetByIdAsync(bankAccountId);

            LogInfo("Bank account ???c ??t làm m?c ??nh thành công v?i ID: {Id}", null, bankAccountId);
            return _mapper.Map<BankAccountResponse>(updated!);
        }, "SetAsDefaultBankAccount");
    }

    /// <summary>
    /// Kích ho?t/vô hi?u hóa bank account
    /// </summary>
    public async Task<BankAccountResponse> ToggleActiveStatusAsync(Guid bankAccountId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("?ang thay ??i tr?ng thái bank account v?i ID: {Id}", null, bankAccountId);

            var existing = await _bankAccountRepository.GetByIdAsync(bankAccountId);
            if (existing == null)
            {
                throw new NotFoundException("BankAccount", bankAccountId);
            }

            // Không cho phép vô hi?u hóa tài kho?n m?c ??nh
            if (existing.IsDefault && existing.IsActive)
            {
                var totalActiveAccounts = (await _bankAccountRepository.GetByUserIdAsync(existing.UserId))
                    .Count(x => x.IsActive);

                if (totalActiveAccounts <= 1)
                {
                    throw new InvalidOperationException("Không th? vô hi?u hóa tài kho?n m?c ??nh duy nh?t");
                }

                // B? m?c ??nh tr??c khi vô hi?u hóa
                existing.IsDefault = false;
            }

            existing.IsActive = !existing.IsActive;
            var updated = await _bankAccountRepository.UpdateAsync(existing);

            LogInfo("Tr?ng thái bank account ???c thay ??i thành công v?i ID: {Id}, Active: {IsActive}", null, bankAccountId, updated.IsActive);
            return _mapper.Map<BankAccountResponse>(updated);
        }, "ToggleBankAccountStatus");
    }
}