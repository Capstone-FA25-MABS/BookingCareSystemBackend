namespace BookingCare.Services.Payment.Enums;

/// <summary>
/// Tr?ng thái c?a yêu c?u refund
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// ?ang ch? - User ch?a có bank account ho?c không có bank account nào active
    /// </summary>
    WAITING,

    /// <summary>
    /// ?ang x? lý - ?ang ??i staff refund ti?n cho user
    /// </summary>
    PENDING,

    /// <summary>
    /// Hoàn thành - Refund ti?n thành công
    /// </summary>
    COMPLETED
}