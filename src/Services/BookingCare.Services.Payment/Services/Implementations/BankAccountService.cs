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
    /// T?o bank account m?i ho?c reactive account cũ n?u ?? t?n t?i nhung inactive
    /// </summary>
    public async Task<BankAccountResponse> CreateAsync(CreateBankAccountRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Đang tạo bank account mới cho User: {UserId}, Account: {AccountNumber}@{BankCode}",
                null, request.UserId, request.AccountNumber, request.BankCode);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.BankCode, nameof(request.BankCode));
            ValidateRequiredString(request.BankName, nameof(request.BankName));
            ValidateRequiredString(request.AccountNumber, nameof(request.AccountNumber));
            ValidateRequiredString(request.AccountName, nameof(request.AccountName));

            // Kiểm tra xem user đã có account number này chưa (bao gồm cả inactive)
            var existingAccount = await _bankAccountRepository.FindByAccountNumberAndUserAsync(
                request.AccountNumber, request.BankCode, request.UserId);

            if (existingAccount != null)
            {
                if (existingAccount.IsActive)
                {
                    // Account đã active -> throw conflict
                    throw new ConflictException($"Tài khoản ngân hàng {request.AccountNumber} tại {request.BankCode} đã tồn tại và đang hoạt động");
                }
                else
                {
                    // Account inactive -> reactive
                    LogInfo("Tìm thấy bank account inactive, đang reactive lại: {Id}", null, existingAccount.Id);

                    // Cập nhật thông tin từ request (có thể user muốn thay đổi bank name hoặc account name)
                    existingAccount.BankName = request.BankName;
                    existingAccount.AccountName = request.AccountName;
                    existingAccount.IsActive = true;

                    // Xử lý logic default cho reactive
                    var existingActiveAccountsCount = (await _bankAccountRepository.GetByUserIdAsync(request.UserId))
                        .Count(x => x.IsActive);
                    var shouldBeDefaultForReactive = request.IsDefault || existingActiveAccountsCount == 0;

                    if (shouldBeDefaultForReactive)
                    {
                        existingAccount.IsDefault = true;
                        // Bỏ mặc định các tài khoản khác nếu cần
                        if (existingActiveAccountsCount > 0)
                        {
                            await _bankAccountRepository.SetAsDefaultAsync(existingAccount.Id, request.UserId);
                        }
                    }

                    var reactivated = await _bankAccountRepository.UpdateAsync(existingAccount);

                    LogInfo("Bank account đã được reactive thành công: {Id}", null, reactivated.Id);
                    return _mapper.Map<BankAccountResponse>(reactivated);
                }
            }

            //// Kiểm tra account number đã tồn tại của user khác chưa
            //var accountExistsGlobally = await _bankAccountRepository.AccountNumberExistsAsync(
            //    request.AccountNumber, request.BankCode);

            //if (accountExistsGlobally)
            //{
            //    throw new ConflictException($"Tài khoản ngân hàng {request.AccountNumber} tại {request.BankCode} đã được sử dụng bởi user khác");
            //}

            // Tạo bank account mới
            LogInfo("Tạo bank account hoàn toàn mới cho User: {UserId}", null, request.UserId);

            var existingAccountsCount = await _bankAccountRepository.CountByUserIdAsync(request.UserId);
            var shouldBeDefaultForNew = request.IsDefault || existingAccountsCount == 0;

            var entity = _mapper.Map<BankAccountEntity>(request);
            entity.IsDefault = shouldBeDefaultForNew;
            entity.IsActive = true; // Đảm bảo account mới luôn active

            // Nếu đặt làm mặc định, cần bỏ mặc định các tài khoản khác
            if (shouldBeDefaultForNew && existingAccountsCount > 0)
            {
                await _bankAccountRepository.SetAsDefaultAsync(entity.Id, request.UserId);
            }

            var created = await _bankAccountRepository.CreateAsync(entity);

            LogInfo("Bank account được tạo thành công với ID: {Id}", null, created.Id);
            return _mapper.Map<BankAccountResponse>(created);
        }, "CreateBankAccount");
    }

    /// <summary>
    /// Cập nhật bank account
    /// </summary>
    public async Task<BankAccountResponse> UpdateAsync(UpdateBankAccountRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Đang cập nhật bank account với ID: {Id}", null, request.Id);

            ValidateRequired(request, nameof(request));

            var existing = await _bankAccountRepository.GetByIdAsync(request.Id);
            if (existing == null)
            {
                throw new NotFoundException("BankAccount", request.Id);
            }

            // Kiểm tra uniqueness nếu cập nhật account number
            if (!string.IsNullOrEmpty(request.AccountNumber) && request.AccountNumber != existing.AccountNumber)
            {
                // Sử dụng BankCode mới nếu có, nếu không dùng BankCode hiện tại
                var bankCodeToCheck = !string.IsNullOrEmpty(request.BankCode) ? request.BankCode : existing.BankCode;

                // Kiểm tra trong toàn hệ thống (không phân biệt user)
                var accountExistsGlobally = await _bankAccountRepository.AccountNumberExistsAsync(
                    request.AccountNumber, bankCodeToCheck, existing.Id);

                if (accountExistsGlobally)
                {
                    throw new ConflictException($"Tài khoản ngân hàng {request.AccountNumber} tại {bankCodeToCheck} đã được sử dụng bởi user khác");
                }
            }

            // Cập nhật các trường được cung cấp
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

            // Xử lý việc đặt làm mặc định
            if (request.IsDefault.HasValue && request.IsDefault.Value && !existing.IsDefault)
            {
                await _bankAccountRepository.SetAsDefaultAsync(existing.Id, existing.UserId);
            }
            else if (request.IsDefault.HasValue)
            {
                existing.IsDefault = request.IsDefault.Value;
            }

            var updated = await _bankAccountRepository.UpdateAsync(existing);

            LogInfo("Bank account được cập nhật thành công với ID: {Id}", null, updated.Id);
            return _mapper.Map<BankAccountResponse>(updated);
        }, "UpdateBankAccount");
    }

    /// <summary>
    /// Xóa hoặc deactivate bank account thông minh
    /// </summary>
    public async Task<BankAccountDeleteResult> SmartDeleteAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Đang thực hiện Smart Delete bank account với ID: {Id}", null, id);

            var existing = await _bankAccountRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return BankAccountDeleteResult.Failed($"Bank account với ID {id} không tìm thấy");
            }

            // Kiểm tra tài khoản mặc định
            if (existing.IsDefault)
            {
                var totalAccounts = await _bankAccountRepository.CountByUserIdAsync(existing.UserId);
                if (totalAccounts > 1)
                {
                    return BankAccountDeleteResult.Failed("Không thể xóa tài khoản mặc định khi còn tài khoản khác. Hãy đặt tài khoản khác làm mặc định trước.");
                }
            }

            // Kiểm tra xem có refund histories sử dụng bank account này không
            var hasRefundHistories = await _bankAccountRepository.HasRefundHistoriesAsync(id);

            if (hasRefundHistories)
            {
                // Có refund histories -> chỉ deactivate
                LogInfo("Bank account {Id} có refund histories, thực hiện deactivate thay vì xóa", null, id);

                existing.IsActive = false;
                if (existing.IsDefault)
                {
                    existing.IsDefault = false; // Bỏ mặc định nếu deactivate
                }

                var updated = await _bankAccountRepository.UpdateAsync(existing);
                var updatedResponse = _mapper.Map<BankAccountResponse>(updated);

                LogInfo("Bank account {Id} đã được deactivate do có refund histories", null, id);

                return BankAccountDeleteResult.Deactivated(
                    updatedResponse,
                    0, // Có thể thêm count nếu cần
                    "Bank account đã được vô hiệu hóa do có liên kết với lịch sử refund. Không thể xóa hoàn toàn."
                );
            }
            else
            {
                // Không có refund histories -> xóa hoàn toàn
                LogInfo("Bank account {Id} không có refund histories, thực hiện xóa hoàn toàn", null, id);

                var result = await _bankAccountRepository.DeleteAsync(id);

                if (result)
                {
                    LogInfo("Bank account {Id} đã được xóa hoàn toàn thành công", null, id);
                    return BankAccountDeleteResult.Deleted("Bank account đã được xóa hoàn toàn thành công");
                }
                else
                {
                    LogWarning("Không thể xóa bank account {Id}", null, id);
                    return BankAccountDeleteResult.Failed("Không thể xóa bank account");
                }
            }
        }, "SmartDeleteBankAccount");
    }

    /// <summary>
    /// Xóa bank account (method cũ - deprecated)
    /// </summary>
    [Obsolete("Sử dụng SmartDeleteAsync thay thế")]
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Đang xóa bank account với ID: {Id}", null, id);

            var existing = await _bankAccountRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return false;
            }

            // Không cho phép xóa tài khoản mặc định nếu còn tài khoản khác
            if (existing.IsDefault)
            {
                var totalAccounts = await _bankAccountRepository.CountByUserIdAsync(existing.UserId);
                if (totalAccounts > 1)
                {
                    throw new InvalidOperationException("Không thể xóa tài khoản mặc định khi còn tài khoản khác. Hãy đặt tài khoản khác làm mặc định trước.");
                }
            }

            var result = await _bankAccountRepository.DeleteAsync(id);

            LogInfo("Bank account được xóa thành công với ID: {Id}", null, id);
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