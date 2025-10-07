namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// K?t qu? c?a thao tác Smart Delete bank account
/// </summary>
public class BankAccountDeleteResult
{
    /// <summary>
    /// Thao tác có thành công hay không
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Lo?i thao tác ?ã th?c hi?n
    /// </summary>
    public BankAccountDeleteAction Action { get; set; }

    /// <summary>
    /// Thông báo mô t? k?t qu?
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Bank account sau khi th?c hi?n thao tác (n?u deactivate)
    /// </summary>
    public BankAccountResponse? UpdatedBankAccount { get; set; }

    /// <summary>
    /// S? l??ng refund histories liên quan
    /// </summary>
    public int RefundHistoriesCount { get; set; }

    /// <summary>
    /// T?o k?t qu? cho thao tác xóa thành công
    /// </summary>
    public static BankAccountDeleteResult Deleted(string message = "Bank account ?ã ???c xóa thành công")
    {
        return new BankAccountDeleteResult
        {
            Success = true,
            Action = BankAccountDeleteAction.Deleted,
            Message = message
        };
    }

    /// <summary>
    /// T?o k?t qu? cho thao tác deactivate thành công
    /// </summary>
    public static BankAccountDeleteResult Deactivated(BankAccountResponse updatedAccount, int refundHistoriesCount, string? customMessage = null)
    {
        var message = customMessage ??
            $"Bank account ?ã ???c vô hi?u hóa do có {refundHistoriesCount} refund history liên quan. Bank account không th? xóa hoàn toàn.";

        return new BankAccountDeleteResult
        {
            Success = true,
            Action = BankAccountDeleteAction.Deactivated,
            Message = message,
            UpdatedBankAccount = updatedAccount,
            RefundHistoriesCount = refundHistoriesCount
        };
    }

    /// <summary>
    /// T?o k?t qu? cho thao tác th?t b?i
    /// </summary>
    public static BankAccountDeleteResult Failed(string message)
    {
        return new BankAccountDeleteResult
        {
            Success = false,
            Action = BankAccountDeleteAction.None,
            Message = message
        };
    }
}

/// <summary>
/// Lo?i thao tác ?ã th?c hi?n trong Smart Delete
/// </summary>
public enum BankAccountDeleteAction
{
    /// <summary>
    /// Không th?c hi?n thao tác nào
    /// </summary>
    None,

    /// <summary>
    /// Xóa hoàn toàn bank account
    /// </summary>
    Deleted,

    /// <summary>
    /// Ch? vô hi?u hóa bank account (do có refund histories)
    /// </summary>
    Deactivated
}