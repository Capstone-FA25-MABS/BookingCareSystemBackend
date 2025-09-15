using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Interface cho PaymentMethod Service
/// </summary>
public interface IPaymentMethodService
{
    /// <summary>
    /// L?y t?t c? payment methods
    /// </summary>
    Task<IEnumerable<PaymentMethodResponse>> GetAllAsync();

    /// <summary>
    /// L?y ch? payment methods ?ang active
    /// </summary>
    Task<IEnumerable<PaymentMethodResponse>> GetActiveAsync();

    /// <summary>
    /// L?y payment method theo ID
    /// </summary>
    Task<PaymentMethodResponse?> GetByIdAsync(Guid id);

    /// <summary>
    /// L?y payment method theo t?n
    /// </summary>
    Task<PaymentMethodResponse?> GetByNameAsync(string name);

    /// <summary>
    /// C?p nh?t tr?ng thái payment method
    /// </summary>
    Task<PaymentMethodResponse> UpdateStatusAsync(UpdatePaymentMethodStatusRequest request);

    /// <summary>
    /// Toggle tr?ng thái payment method (ACTIVE <-> INACTIVE)
    /// </summary>
    Task<PaymentMethodResponse> ToggleStatusAsync(Guid id);
}