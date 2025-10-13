using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Controllers;

namespace BookingCare.Services.Payment.Helpers;

/// <summary>
/// Helper class for common payment validation logic
/// </summary>
public static class PaymentValidationHelper
{
    /// <summary>
    /// Validates payment existence and amount match for payment gateway operations
    /// </summary>
    /// <param name="paymentService">Payment service instance</param>
    /// <param name="paymentId">Payment ID to validate</param>
    /// <param name="expectedAmount">Expected amount to match</param>
    /// <param name="validateStatus">Whether to validate payment status is PENDING</param>
    /// <returns>Tuple with validation result and payment data</returns>
    public static async Task<(IActionResult? ValidationError, PaymentResponse? Payment)> ValidatePaymentForGatewayAsync(
        IPaymentService paymentService,
        Guid paymentId,
        decimal expectedAmount,
        bool validateStatus = true)
    {
        // Validate payment exists
        var payment = await paymentService.GetByIdAsync(paymentId);
        if (payment == null)
        {
            return (CreateNotFoundResult(paymentId), null);
        }

        // Validate amount matches (using tolerance for decimal comparison)
        if (Math.Abs(payment.Amount - expectedAmount) > 0.01m)
        {
            return (CreateAmountMismatchResult(payment.Amount, expectedAmount), null);
        }

        // Validate payment status if requested
        if (validateStatus && payment.Status != PaymentStatus.PENDING)
        {
            return (CreateInvalidStatusResult(payment.Status), null);
        }

        return (null, payment);
    }

    /// <summary>
    /// Creates a standardized not found result for payment
    /// </summary>
    private static IActionResult CreateNotFoundResult(Guid paymentId)
    {
        return new ObjectResult(new
        {
            success = false,
            message = $"Payment with ID {paymentId} was not found",
            timestamp = DateTime.UtcNow
        })
        {
            StatusCode = 404
        };
    }

    /// <summary>
    /// Creates a standardized amount mismatch result
    /// </summary>
    private static IActionResult CreateAmountMismatchResult(decimal actualAmount, decimal expectedAmount)
    {
        return new ObjectResult(new
        {
            success = false,
            message = "Amount does not match the payment in the system",
            details = new
            {
                actualAmount,
                expectedAmount,
                difference = Math.Abs(actualAmount - expectedAmount)
            },
            timestamp = DateTime.UtcNow
        })
        {
            StatusCode = 400
        };
    }

    /// <summary>
    /// Creates a standardized invalid status result
    /// </summary>
    private static IActionResult CreateInvalidStatusResult(PaymentStatus currentStatus)
    {
        return new ObjectResult(new
        {
            success = false,
            message = $"Payment has been processed with status: {currentStatus}",
            details = new
            {
                currentStatus = currentStatus.ToString(),
                expectedStatus = PaymentStatus.PENDING.ToString()
            },
            timestamp = DateTime.UtcNow
        })
        {
            StatusCode = 400
        };
    }
}