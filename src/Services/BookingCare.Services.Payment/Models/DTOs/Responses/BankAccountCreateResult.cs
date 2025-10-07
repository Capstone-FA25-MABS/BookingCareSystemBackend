namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// K?t qu? c?a thao tác Smart Create bank account
/// </summary>
public class BankAccountCreateResult
{
    /// <summary>
    /// Thao tác có thành công hay không
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Lo?i thao tác ?ã th?c hi?n
    /// </summary>
    public BankAccountCreateAction Action { get; set; }

    /// <summary>
    /// Thông báo mô t? k?t qu?
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Bank account ???c t?o ho?c reactive
    /// </summary>
    public BankAccountResponse? BankAccount { get; set; }

    /// <summary>
    /// T?o k?t qu? cho thao tác t?o m?i thành công
    /// </summary>
    public static BankAccountCreateResult Created(BankAccountResponse bankAccount, string? customMessage = null)
    {
        var message = customMessage ?? "Bank account ???c t?o m?i thành công";

        return new BankAccountCreateResult
        {
            Success = true,
            Action = BankAccountCreateAction.Created,
            Message = message,
            BankAccount = bankAccount
        };
    }

    /// <summary>
    /// T?o k?t qu? cho thao tác reactive thành công
    /// </summary>
    public static BankAccountCreateResult Reactivated(BankAccountResponse bankAccount, string? customMessage = null)
    {
        var message = customMessage ??
            "Bank account ?ã t?n t?i nh?ng b? vô hi?u hóa tr??c ?ó. Tài kho?n ?ã ???c kích ho?t l?i thành công.";

        return new BankAccountCreateResult
        {
            Success = true,
            Action = BankAccountCreateAction.Reactivated,
            Message = message,
            BankAccount = bankAccount
        };
    }

    /// <summary>
    /// T?o k?t qu? cho thao tác th?t b?i
    /// </summary>
    public static BankAccountCreateResult Failed(string message)
    {
        return new BankAccountCreateResult
        {
            Success = false,
            Action = BankAccountCreateAction.None,
            Message = message
        };
    }
}

/// <summary>
/// Lo?i thao tác ?ã th?c hi?n trong Smart Create
/// </summary>
public enum BankAccountCreateAction
{
    /// <summary>
    /// Không th?c hi?n thao tác nào
    /// </summary>
    None,

    /// <summary>
    /// T?o hoàn toàn m?i bank account
    /// </summary>
    Created,

    /// <summary>
    /// Kích ho?t l?i bank account ?ã t?n t?i nh?ng b? inactive
    /// </summary>
    Reactivated
}