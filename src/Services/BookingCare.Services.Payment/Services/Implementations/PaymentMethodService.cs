using AutoMapper;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Enums;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Payment.Services.Implementations;

/// <summary>
/// Implementation of PaymentMethod Service
/// </summary>
public class PaymentMethodService : BaseService, IPaymentMethodService
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IMapper _mapper;

    public PaymentMethodService(
        IPaymentMethodRepository paymentMethodRepository,
        IMapper mapper,
        ILogger<PaymentMethodService> logger) : base(logger)
    {
        _paymentMethodRepository = paymentMethodRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Get all payment methods - Simple read operation
    /// </summary>
    public async Task<IEnumerable<PaymentMethodResponse>> GetAllAsync()
    {
        var paymentMethods = await _paymentMethodRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<PaymentMethodResponse>>(paymentMethods);
    }

    /// <summary>
    /// Get only active payment methods - Simple read operation
    /// </summary>
    public async Task<IEnumerable<PaymentMethodResponse>> GetActiveAsync()
    {
        var paymentMethods = await _paymentMethodRepository.GetActiveAsync();
        return _mapper.Map<IEnumerable<PaymentMethodResponse>>(paymentMethods);
    }

    /// <summary>
    /// Get payment method by ID - Simple read operation
    /// </summary>
    public async Task<PaymentMethodResponse?> GetByIdAsync(Guid id)
    {
        var paymentMethod = await _paymentMethodRepository.GetByIdAsync(id);
        return paymentMethod != null ? _mapper.Map<PaymentMethodResponse>(paymentMethod) : null;
    }

    /// <summary>
    /// Get payment method by name - Simple read operation
    /// </summary>
    public async Task<PaymentMethodResponse?> GetByNameAsync(string name)
    {
        var paymentMethod = await _paymentMethodRepository.GetByNameAsync(name);
        return paymentMethod != null ? _mapper.Map<PaymentMethodResponse>(paymentMethod) : null;
    }

    /// <summary>
    /// Update payment method status - Business operation MUST use ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentMethodResponse> UpdateStatusAsync(UpdatePaymentMethodStatusRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Starting to update payment method status: {PaymentMethodId} -> {Status}", null, request.Id, request.Status);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.Id, nameof(request.Id));

            var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.Id);
            if (paymentMethod == null)
            {
                LogError(new ArgumentException($"Payment method with ID {request.Id} does not exist"),
                    "Payment method does not exist: {PaymentMethodId}", null, request.Id);
                throw new ArgumentException($"Payment method with ID {request.Id} does not exist");
            }

            // Business logic
            paymentMethod.Status = request.Status;
            var updatedPaymentMethod = await _paymentMethodRepository.UpdateAsync(paymentMethod);

            LogInfo("Payment method status updated successfully: {PaymentMethodId}", null, request.Id);
            return _mapper.Map<PaymentMethodResponse>(updatedPaymentMethod);
        }, "UpdatePaymentMethodStatus");
    }

    /// <summary>
    /// Toggle payment method status - Business operation MUST use ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentMethodResponse> ToggleStatusAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Starting to toggle payment method status: {PaymentMethodId}", null, id);

            ValidateGuid(id, nameof(id));

            var paymentMethod = await _paymentMethodRepository.GetByIdAsync(id);
            if (paymentMethod == null)
            {
                LogError(new ArgumentException($"Payment method with ID {id} does not exist"),
                    "Payment method does not exist: {PaymentMethodId}", null, id);
                throw new ArgumentException($"Payment method with ID {id} does not exist");
            }

            // Toggle logic
            var oldStatus = paymentMethod.Status;
            paymentMethod.Status = paymentMethod.Status == PaymentMethodStatus.ACTIVE
                ? PaymentMethodStatus.INACTIVE
                : PaymentMethodStatus.ACTIVE;

            var updatedPaymentMethod = await _paymentMethodRepository.UpdateAsync(paymentMethod);

            LogInfo("Payment method status toggled successfully: {PaymentMethodId} - {OldStatus} -> {NewStatus}",
                null, id, oldStatus, paymentMethod.Status);
            return _mapper.Map<PaymentMethodResponse>(updatedPaymentMethod);
        }, "TogglePaymentMethodStatus");
    }
}