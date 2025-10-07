using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Interface for PaymentMethod Service
/// </summary>
public interface IPaymentMethodService
{
    /// <summary>
    /// Get all payment methods
    /// </summary>
    Task<IEnumerable<PaymentMethodResponse>> GetAllAsync();

    /// <summary>
    /// Get only active payment methods
    /// </summary>
    Task<IEnumerable<PaymentMethodResponse>> GetActiveAsync();

    /// <summary>
    /// Get payment method by ID
    /// </summary>
    Task<PaymentMethodResponse?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get payment method by name
    /// </summary>
    Task<PaymentMethodResponse?> GetByNameAsync(string name);

    /// <summary>
    /// Update payment method status
    /// </summary>
    Task<PaymentMethodResponse> UpdateStatusAsync(UpdatePaymentMethodStatusRequest request);

    /// <summary>
    /// Toggle payment method status (ACTIVE <-> INACTIVE)
    /// </summary>
    Task<PaymentMethodResponse> ToggleStatusAsync(Guid id);
}