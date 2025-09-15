using AutoMapper;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Services.Implementations;

/// <summary>
/// Implementation của Payment Service
/// </summary>
public class PaymentService : BaseService, IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IMapper _mapper;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IMapper mapper,
        ILogger<PaymentService> logger) : base(logger)
    {
        _paymentRepository = paymentRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Lấy payment theo ID - Thao tác đọc đơn giản
    /// </summary>
    public async Task<PaymentResponse?> GetByIdAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);
        return payment != null ? _mapper.Map<PaymentResponse>(payment) : null;
    }

    /// <summary>
    /// Lấy payment theo appointment ID - Thao tác đọc đơn giản
    /// </summary>
    public async Task<PaymentResponse?> GetByAppointmentIdAsync(Guid appointmentId)
    {
        var payment = await _paymentRepository.GetByAppointmentIdAsync(appointmentId);
        return payment != null ? _mapper.Map<PaymentResponse>(payment) : null;
    }

    /// <summary>
    /// Lấy payment theo subscription ID - Thao tác đọc đơn giản
    /// </summary>
    public async Task<PaymentResponse?> GetBySubscriptionIdAsync(Guid subscriptionId)
    {
        var payment = await _paymentRepository.GetBySubscriptionIdAsync(subscriptionId);
        return payment != null ? _mapper.Map<PaymentResponse>(payment) : null;
    }

    /// <summary>
    /// Lấy danh sách payments theo clinic ID - Thao tác đọc đơn giản
    /// </summary>
    public async Task<IEnumerable<PaymentResponse>> GetByClinicIdAsync(Guid clinicId)
    {
        var payments = await _paymentRepository.GetByClinicIdAsync(clinicId);
        return _mapper.Map<IEnumerable<PaymentResponse>>(payments);
    }

    /// <summary>
    /// Lấy danh sách payments theo patient ID - Thao tác đọc đơn giản
    /// </summary>
    public async Task<IEnumerable<PaymentResponse>> GetByPatientIdAsync(Guid patientId)
    {
        var payments = await _paymentRepository.GetByPatientIdAsync(patientId);
        return _mapper.Map<IEnumerable<PaymentResponse>>(payments);
    }

    /// <summary>
    /// Tạo payment mới - Thao tác business BẮT BUỘC sử dụng ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentResponse> CreateAsync(CreatePaymentRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Bắt đầu tạo payment - Type: {TransactionType}, AppointmentId: {AppointmentId}, SubscriptionId: {SubscriptionId}",
                null, request.TransactionType, request.AppointmentId, request.SubscriptionId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.PaymentMethodId, nameof(request.PaymentMethodId));

            // Validate AppointmentId nếu có giá trị
            if (request.AppointmentId.HasValue)
            {
                ValidateGuid(request.AppointmentId.Value, nameof(request.AppointmentId));
            }

            // Validate SubscriptionId nếu có giá trị
            if (request.SubscriptionId.HasValue)
            {
                ValidateGuid(request.SubscriptionId.Value, nameof(request.SubscriptionId));
            }

            // Validate payment method exists
            var paymentMethodExists = await _paymentMethodRepository.ExistsAsync(request.PaymentMethodId);
            if (!paymentMethodExists)
            {
                LogError(new ArgumentException($"Payment method with ID {request.PaymentMethodId} does not exist"),
                    "Payment method không tồn tại: {PaymentMethodId}", null, request.PaymentMethodId);
                throw new ArgumentException($"Payment method with ID {request.PaymentMethodId} does not exist");
            }

            // Check if payment already exists for this appointment (nếu AppointmentId có giá trị)
            if (request.AppointmentId.HasValue)
            {
                var existingPayment = await _paymentRepository.GetByAppointmentIdAsync(request.AppointmentId.Value);
                if (existingPayment != null)
                {
                    LogError(new InvalidOperationException($"Payment already exists for appointment {request.AppointmentId}"),
                        "Payment đã tồn tại cho appointment: {AppointmentId}", null, request.AppointmentId);
                    throw new InvalidOperationException($"Payment already exists for appointment {request.AppointmentId}");
                }
            }


            // Business logic
            var paymentEntity = _mapper.Map<PaymentEntity>(request);
            var createdPayment = await _paymentRepository.CreateAsync(paymentEntity);

            LogInfo("Payment được tạo thành công với ID: {PaymentId}", null, createdPayment.Id);
            return _mapper.Map<PaymentResponse>(createdPayment);
        }, "CreatePayment");
    }

    /// <summary>
    /// Tạo payment cho appointment (patient đặt lịch) - Thao tác business BẮT BUỘC sử dụng ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentResponse> CreateAppointmentPaymentAsync(CreateAppointmentPaymentRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Bắt đầu tạo payment cho appointment: {AppointmentId}", null, request.AppointmentId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.AppointmentId, nameof(request.AppointmentId));
            ValidateGuid(request.PatientId, nameof(request.PatientId));
            ValidateGuid(request.PaymentMethodId, nameof(request.PaymentMethodId));

            // Validate payment method exists
            var paymentMethodExists = await _paymentMethodRepository.ExistsAsync(request.PaymentMethodId);
            if (!paymentMethodExists)
            {
                LogError(new ArgumentException($"Payment method with ID {request.PaymentMethodId} does not exist"),
                    "Payment method không tồn tại: {PaymentMethodId}", null, request.PaymentMethodId);
                throw new ArgumentException($"Payment method with ID {request.PaymentMethodId} does not exist");
            }

            // Check if payment already exists for this appointment
            var existingPayment = await _paymentRepository.GetByAppointmentIdAsync(request.AppointmentId);
            if (existingPayment != null)
            {
                LogError(new InvalidOperationException($"Payment already exists for appointment {request.AppointmentId}"),
                    "Payment đã tồn tại cho appointment: {AppointmentId}", null, request.AppointmentId);
                throw new InvalidOperationException($"Payment already exists for appointment {request.AppointmentId}");
            }

            // Business logic - Map to PaymentEntity
            var paymentEntity = new PaymentEntity
            {
                AppointmentId = request.AppointmentId,
                PatientId = request.PatientId,
                ClinicId = null,
                SubscriptionId = null,
                Amount = request.Amount,
                TransactionType = TransactionType.APPOINTMENT,
                PaymentMethodId = request.PaymentMethodId,
                Status = PaymentStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            };

            var createdPayment = await _paymentRepository.CreateAsync(paymentEntity);

            LogInfo("Payment cho appointment được tạo thành công với ID: {PaymentId}", null, createdPayment.Id);
            return _mapper.Map<PaymentResponse>(createdPayment);
        }, "CreateAppointmentPayment");
    }

    /// <summary>
    /// Tạo payment cho subscription (clinic đăng ký gói) - Thao tác business BẮT BUỘC sử dụng ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentResponse> CreateSubscriptionPaymentAsync(CreateSubscriptionPaymentRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Bắt đầu tạo payment cho subscription: {SubscriptionId}", null, request.SubscriptionId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.SubscriptionId, nameof(request.SubscriptionId));
            ValidateGuid(request.ClinicId, nameof(request.ClinicId));
            ValidateGuid(request.PaymentMethodId, nameof(request.PaymentMethodId));

            // Validate payment method exists
            var paymentMethodExists = await _paymentMethodRepository.ExistsAsync(request.PaymentMethodId);
            if (!paymentMethodExists)
            {
                LogError(new ArgumentException($"Payment method with ID {request.PaymentMethodId} does not exist"),
                    "Payment method không tồn tại: {PaymentMethodId}", null, request.PaymentMethodId);
                throw new ArgumentException($"Payment method with ID {request.PaymentMethodId} does not exist");
            }



            // Business logic - Map to PaymentEntity
            var paymentEntity = new PaymentEntity
            {
                AppointmentId = null,
                PatientId = null,
                ClinicId = request.ClinicId,
                SubscriptionId = request.SubscriptionId,
                Amount = request.Amount,
                TransactionType = TransactionType.SUBSCRIPTION,
                PaymentMethodId = request.PaymentMethodId,
                Status = PaymentStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            };

            var createdPayment = await _paymentRepository.CreateAsync(paymentEntity);

            LogInfo("Payment cho subscription được tạo thành công với ID: {PaymentId}", null, createdPayment.Id);
            return _mapper.Map<PaymentResponse>(createdPayment);
        }, "CreateSubscriptionPayment");
    }

    /// <summary>
    /// Cập nhật trạng thái payment - Thao tác business BẮT BUỘC sử dụng ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentResponse> UpdateStatusAsync(UpdatePaymentStatusRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Bắt đầu cập nhật trạng thái payment: {PaymentId} -> {Status}", null, request.Id, request.Status);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateGuid(request.Id, nameof(request.Id));

            var payment = await _paymentRepository.GetByIdAsync(request.Id);
            if (payment == null)
            {
                LogError(new ArgumentException($"Payment with ID {request.Id} does not exist"),
                    "Payment không tồn tại: {PaymentId}", null, request.Id);
                throw new ArgumentException($"Payment with ID {request.Id} does not exist");
            }

            // Business logic
            payment.Status = request.Status;
            var updatedPayment = await _paymentRepository.UpdateAsync(payment);

            LogInfo("Trạng thái payment được cập nhật thành công: {PaymentId}", null, request.Id);
            return _mapper.Map<PaymentResponse>(updatedPayment);
        }, "UpdatePaymentStatus");
    }

    /// <summary>
    /// Xóa payment - Thao tác business BẮT BUỘC sử dụng ExecuteWithErrorHandling
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Bắt đầu xóa payment: {PaymentId}", null, id);

            ValidateGuid(id, nameof(id));

            var exists = await _paymentRepository.ExistsAsync(id);
            if (!exists)
            {
                LogWarning("Payment không tồn tại để xóa: {PaymentId}", null, id);
                return false;
            }

            // Business logic
            var result = await _paymentRepository.DeleteAsync(id);

            if (result)
            {
                LogInfo("Payment được xóa thành công: {PaymentId}", null, id);
            }
            else
            {
                LogError(new InvalidOperationException("Failed to delete payment"),
                    "Không thể xóa payment: {PaymentId}", null, id);
            }

            return result;
        }, "DeletePayment");
    }
}