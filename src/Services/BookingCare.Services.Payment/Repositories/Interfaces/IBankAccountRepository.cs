using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Repository interface cho BankAccount
/// </summary>
public interface IBankAccountRepository
{
    /// <summary>
    /// L?y bank account theo ID
    /// </summary>
    Task<BankAccountEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// L?y t?t c? bank accounts c?a user
    /// </summary>
    Task<IEnumerable<BankAccountEntity>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// L?y bank accounts c?a user v?i phân trang
    /// </summary>
    Task<PagedResult<BankAccountEntity>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, bool? activeOnly = null);

    /// <summary>
    /// L?y bank account m?c ??nh c?a user
    /// </summary>
    Task<BankAccountEntity?> GetDefaultByUserIdAsync(Guid userId);

    /// <summary>
    /// Ki?m tra s? tài kho?n ?ã t?n t?i ch?a
    /// </summary>
    Task<bool> AccountNumberExistsAsync(string accountNumber, string bankCode, Guid? excludeId = null);

    /// <summary>
    /// T?o bank account m?i
    /// </summary>
    Task<BankAccountEntity> CreateAsync(BankAccountEntity entity);

    /// <summary>
    /// C?p nh?t bank account
    /// </summary>
    Task<BankAccountEntity> UpdateAsync(BankAccountEntity entity);

    /// <summary>
    /// Xóa bank account
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// ??t tài kho?n làm m?c ??nh và b? m?c ??nh các tài kho?n khác c?a user
    /// </summary>
    Task SetAsDefaultAsync(Guid bankAccountId, Guid userId);

    /// <summary>
    /// ??m s? l??ng bank accounts c?a user
    /// </summary>
    Task<int> CountByUserIdAsync(Guid userId);
}