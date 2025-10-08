namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Result of the Smart Create bank account operation
/// </summary>
public class BankAccountCreateResult
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The action performed
    /// </summary>
    public BankAccountCreateAction Action { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The bank account that was created or reactivated
    /// </summary>
    public BankAccountResponse? BankAccount { get; set; }

    /// <summary>
    /// Create result for a newly created bank account
    /// </summary>
    public static BankAccountCreateResult Created(BankAccountResponse bankAccount, string? customMessage = null)
    {
        var message = customMessage ?? "Bank account has been created successfully";

        return new BankAccountCreateResult
        {
            Success = true,
            Action = BankAccountCreateAction.Created,
            Message = message,
            BankAccount = bankAccount
        };
    }

    /// <summary>
    /// Create result for a successfully reactivated bank account
    /// </summary>
    public static BankAccountCreateResult Reactivated(BankAccountResponse bankAccount, string? customMessage = null)
    {
        var message = customMessage ??
            "Bank account already existed but was inactive. The account has been reactivated successfully.";

        return new BankAccountCreateResult
        {
            Success = true,
            Action = BankAccountCreateAction.Reactivated,
            Message = message,
            BankAccount = bankAccount
        };
    }

    /// <summary>
    /// Create result for a failed operation
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
/// Actions performed during Smart Create
/// </summary>
public enum BankAccountCreateAction
{
    /// <summary>
    /// No action performed
    /// </summary>
    None,

    /// <summary>
    /// A brand new bank account was created
    /// </summary>
    Created,

    /// <summary>
    /// An existing inactive bank account was reactivated
    /// </summary>
    Reactivated
}