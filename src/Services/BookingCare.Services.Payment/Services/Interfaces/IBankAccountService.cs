using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Service interface cho BankAccount
/// </summary>
public interface IBankAccountService
{
    /// <summary>
    /// L?y bank account theo ID
    /// </summary>
    Task<BankAccountResponse?> GetByIdAsync(Guid id);

    /// <summary>
    /// L?y t?t c? bank accounts c?a user
    /// </summary>
    Task<IEnumerable<BankAccountResponse>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// L?y bank accounts c?a user v?i phân trang
    /// </summary>
    Task<PagedResult<BankAccountResponse>> GetPagedByUserIdAsync(GetBankAccountsRequest request);

    /// <summary>
    /// L?y bank account m?c ??nh c?a user
    /// </summary>
    Task<BankAccountResponse?> GetDefaultByUserIdAsync(Guid userId);

    /// <summary>
    /// T?o bank account m?i
    /// </summary>
    Task<BankAccountResponse> CreateAsync(CreateBankAccountRequest request);

    /// <summary>
    /// C?p nh?t bank account
    /// </summary>
    Task<BankAccountResponse> UpdateAsync(UpdateBankAccountRequest request);

    /// <summary>
    /// Xóa ho?c deactivate bank account thông minh
    /// N?u bank account ???c s? d?ng trong RefundHistories -> ch? deactivate
    /// N?u không ???c s? d?ng -> xóa h?n
    /// </summary>
    Task<BankAccountDeleteResult> SmartDeleteAsync(Guid id);

    /// <summary>
    /// Xóa bank account (method c? - deprecated)
    /// </summary>
    [Obsolete("S? d?ng SmartDeleteAsync thay th?")]
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// ??t bank account làm m?c ??nh
    /// </summary>
    Task<BankAccountResponse> SetAsDefaultAsync(Guid bankAccountId);

    /// <summary>
    /// Kích ho?t/vô hi?u hóa bank account
    /// </summary>
    Task<BankAccountResponse> ToggleActiveStatusAsync(Guid bankAccountId);
}