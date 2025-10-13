namespace BookingCare.Services.Payment.Constants;

/// <summary>
/// Contains all error messages used throughout the Payment Service
/// </summary>
public static class ErrorMessages
{
    #region Refund History Error Messages

    /// <summary>
    /// Error message for invalid refund history ID
    /// </summary>
    public const string InvalidRefundHistoryId = "Invalid refund history ID";

    /// <summary>
    /// Error message for invalid request data
    /// </summary>
    public const string InvalidRequestData = "Invalid request data";

    /// <summary>
    /// Error message for retrieving refund histories list
    /// </summary>
    public const string RefundHistoriesListError = "An error occurred while retrieving refund histories list";

    /// <summary>
    /// Error message for retrieving refund history
    /// </summary>
    public const string RefundHistoryRetrievalError = "An error occurred while retrieving refund history";

    /// <summary>
    /// Error message for creating refund history
    /// </summary>
    public const string RefundHistoryCreationError = "An error occurred while creating refund history";

    /// <summary>
    /// Error message for updating refund history status
    /// </summary>
    public const string RefundHistoryStatusUpdateError = "An error occurred while updating refund history status";

    /// <summary>
    /// Error message for deleting refund history
    /// </summary>
    public const string RefundHistoryDeletionError = "An error occurred while deleting refund history";

    /// <summary>
    /// Error message for processing refund histories
    /// </summary>
    public const string RefundHistoryProcessingError = "An error occurred while processing refund histories";

    /// <summary>
    /// Error message for retrieving refund statistics
    /// </summary>
    public const string RefundStatisticsRetrievalError = "An error occurred while retrieving refund statistics";

    /// <summary>
    /// Error message for checking payment refund eligibility
    /// </summary>
    public const string PaymentRefundCheckError = "An error occurred while checking the payment";

    /// <summary>
    /// Error message for retrieving processable refund histories
    /// </summary>
    public const string ProcessableRefundHistoriesRetrievalError = "An error occurred while retrieving processable refund histories";

    /// <summary>
    /// Error message for marking refund as transferred
    /// </summary>
    public const string RefundMarkTransferredError = "An error occurred while marking refund as transferred";

    /// <summary>
    /// Error message for reporting bank account issue
    /// </summary>
    public const string BankIssueReportError = "An error occurred while reporting bank account issue";

    #endregion

    #region General Validation Messages

    /// <summary>
    /// Error message for invalid payment ID
    /// </summary>
    public const string InvalidPaymentId = "Invalid payment ID";

    /// <summary>
    /// Error message for invalid user ID
    /// </summary>
    public const string InvalidUserId = "Invalid user ID";

    /// <summary>
    /// Error message for invalid hospital ID
    /// </summary>
    public const string InvalidHospitalId = "Invalid hospital ID";

    #endregion
}

