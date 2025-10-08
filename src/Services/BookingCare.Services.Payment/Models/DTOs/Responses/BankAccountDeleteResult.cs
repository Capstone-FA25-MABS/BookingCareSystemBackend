namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Result of the Smart Delete bank account operation
/// </summary>
public class BankAccountDeleteResult
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The action performed
    /// </summary>
    public BankAccountDeleteAction Action { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Bank account after the operation (if deactivated)
    /// </summary>
    public BankAccountResponse? UpdatedBankAccount { get; set; }

    /// <summary>
    /// Count of related refund histories
    /// </summary>
    public int RefundHistoriesCount { get; set; }

    /// <summary>
    /// Create result for a successful delete operation
    /// </summary>
    public static BankAccountDeleteResult Deleted(string message = "Bank account has been deleted successfully")
    {
        return new BankAccountDeleteResult
        {
            Success = true,
            Action = BankAccountDeleteAction.Deleted,
            Message = message
        };
    }

    /// <summary>
    /// Create result for a successful deactivate operation
    /// </summary>
    public static BankAccountDeleteResult Deactivated(BankAccountResponse updatedAccount, int refundHistoriesCount, string? customMessage = null)
    {
        var message = customMessage ??
            $"Bank account was deactivated because there are {refundHistoriesCount} related refund histories. The account cannot be fully deleted.";

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
    /// Create result for a failed operation
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
/// Actions performed during Smart Delete
/// </summary>
public enum BankAccountDeleteAction
{
    /// <summary>
    /// No action performed
    /// </summary>
    None,

    /// <summary>
    /// Bank account was fully deleted
    /// </summary>
    Deleted,

    /// <summary>
    /// Bank account was only deactivated (due to related refund histories)
    /// </summary>
    Deactivated
}