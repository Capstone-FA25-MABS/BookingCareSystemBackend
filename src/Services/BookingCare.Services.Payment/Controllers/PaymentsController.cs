using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller quản lý các thao tác thanh toán
/// </summary>
[Route("api/[controller]")]
public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _paymentService;
    private readonly IValidator<CreatePaymentRequest> _createValidator;
    private readonly IValidator<CreateAppointmentPaymentRequest> _createAppointmentValidator;
    private readonly IValidator<CreateSubscriptionPaymentRequest> _createSubscriptionValidator;
    private readonly IValidator<UpdatePaymentStatusRequest> _updateValidator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IValidator<CreatePaymentRequest> createValidator,
        IValidator<CreateAppointmentPaymentRequest> createAppointmentValidator,
        IValidator<CreateSubscriptionPaymentRequest> createSubscriptionValidator,
        IValidator<UpdatePaymentStatusRequest> updateValidator,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _createValidator = createValidator;
        _createAppointmentValidator = createAppointmentValidator;
        _createSubscriptionValidator = createSubscriptionValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Success(new { Status = "Healthy", Service = "Payment", Timestamp = DateTime.UtcNow }, "Payment Service đang hoạt động");
    }

    /// <summary>
    /// Lấy payment theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        try
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
            {
                return NotFound($"Payment với ID {id} không tìm thấy");
            }

            return Success(payment, "Lấy payment thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment with ID: {PaymentId}", id);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy payment" });
        }
    }

    /// <summary>
    /// Lấy payment theo appointment ID
    /// </summary>
    [HttpGet("appointment/{appointmentId}")]
    public async Task<IActionResult> GetPaymentByAppointment(Guid appointmentId)
    {
        try
        {
            var payment = await _paymentService.GetByAppointmentIdAsync(appointmentId);
            if (payment == null)
            {
                return NotFound($"Payment cho appointment {appointmentId} không tìm thấy");
            }

            return Success(payment, "Lấy payment thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment for appointment ID: {AppointmentId}", appointmentId);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy payment" });
        }
    }

    /// <summary>
    /// Lấy payment theo subscription ID
    /// </summary>
    [HttpGet("subscription/{subscriptionId}")]
    public async Task<IActionResult> GetPaymentBySubscription(Guid subscriptionId)
    {
        try
        {
            var payment = await _paymentService.GetBySubscriptionIdAsync(subscriptionId);
            if (payment == null)
            {
                return NotFound($"Payment cho subscription {subscriptionId} không tìm thấy");
            }

            return Success(payment, "Lấy payment thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment for subscription ID: {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy payment" });
        }
    }

    /// <summary>
    /// Lấy danh sách payments theo clinic ID
    /// </summary>
    [HttpGet("clinic/{clinicId}")]
    public async Task<IActionResult> GetPaymentsByClinic(Guid clinicId)
    {
        try
        {
            var payments = await _paymentService.GetByClinicIdAsync(clinicId);
            return Success(payments, "Lấy danh sách payments thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for clinic ID: {ClinicId}", clinicId);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy danh sách payments" });
        }
    }

    /// <summary>
    /// Lấy danh sách payments theo patient ID
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetPaymentsByPatient(Guid patientId)
    {
        try
        {
            var payments = await _paymentService.GetByPatientIdAsync(patientId);
            return Success(payments, "Lấy danh sách payments thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for patient ID: {PatientId}", patientId);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy danh sách payments" });
        }
    }

    /// <summary>
    /// Tạo payment cho appointment (patient đặt lịch)
    /// </summary>
    [HttpPost("appointment")]
    public async Task<IActionResult> CreateAppointmentPayment([FromBody] CreateAppointmentPaymentRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _createAppointmentValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Dữ liệu không hợp lệ", errors);
            }

            var payment = await _paymentService.CreateAppointmentPaymentAsync(request);
            return Created(payment, "Tạo payment cho appointment thành công");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating appointment payment");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating appointment payment");
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment payment");
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi tạo payment cho appointment" });
        }
    }

    /// <summary>
    /// Tạo payment cho subscription (clinic đăng ký gói)
    /// </summary>
    [HttpPost("subscription")]
    public async Task<IActionResult> CreateSubscriptionPayment([FromBody] CreateSubscriptionPaymentRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _createSubscriptionValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Dữ liệu không hợp lệ", errors);
            }

            var payment = await _paymentService.CreateSubscriptionPaymentAsync(request);
            return Created(payment, "Tạo payment cho subscription thành công");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating subscription payment");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating subscription payment");
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription payment");
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi tạo payment cho subscription" });
        }
    }

    /// <summary>
    /// Cập nhật trạng thái payment
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdatePaymentStatus(Guid id, [FromBody] UpdatePaymentStatusRequest request)
    {
        try
        {
            request.Id = id; // Ensure ID matches route parameter

            // Validate request
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Dữ liệu không hợp lệ", errors);
            }

            var payment = await _paymentService.UpdateStatusAsync(request);
            return Success(payment, "Cập nhật trạng thái payment thành công");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when updating payment status");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status for ID: {PaymentId}", id);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi cập nhật trạng thái payment" });
        }
    }

    /// <summary>
    /// Xóa payment
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayment(Guid id)
    {
        try
        {
            var result = await _paymentService.DeleteAsync(id);
            if (!result)
            {
                return NotFound($"Payment với ID {id} không tìm thấy");
            }

            return Success("Xóa payment thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment with ID: {PaymentId}", id);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi xóa payment" });
        }
    }
}