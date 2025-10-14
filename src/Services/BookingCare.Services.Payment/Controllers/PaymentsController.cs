using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for payment operations
/// </summary>

[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class PaymentsController : BaseApiController
{
    private const string InvalidRequestDataMessage = "Invalid request data";

    private readonly IPaymentService _paymentService;
    private readonly IPayOSService _payOSService;
    private readonly IVNPayService _vnPayService;
    private readonly IValidator<CreateAppointmentPaymentRequest> _createAppointmentValidator;
    private readonly IValidator<CreateSubscriptionPaymentRequest> _createSubscriptionValidator;
    private readonly IValidator<UpdatePaymentStatusRequest> _updateValidator;
    private readonly IValidator<GetPaymentsPagedRequest> _pagedValidator;
    private readonly IValidator<GetPaymentStatisticsRequest> _statisticsValidator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IPayOSService payOSService,
        IVNPayService vnPayService,
        IValidator<CreateAppointmentPaymentRequest> createAppointmentValidator,
        IValidator<CreateSubscriptionPaymentRequest> createSubscriptionValidator,
        IValidator<UpdatePaymentStatusRequest> updateValidator,
        IValidator<GetPaymentsPagedRequest> pagedValidator,
        IValidator<GetPaymentStatisticsRequest> statisticsValidator,
        ILogger<PaymentsController> logger
    )
    {
        _paymentService = paymentService;
        _payOSService = payOSService;
        _vnPayService = vnPayService;
        _createAppointmentValidator = createAppointmentValidator;
        _createSubscriptionValidator = createSubscriptionValidator;
        _updateValidator = updateValidator;
        _pagedValidator = pagedValidator;
        _statisticsValidator = statisticsValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get payment by ID
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        try
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
            {
                return NotFound($"Payment with ID {id} was not found");
            }

            return Success(payment, "Get payment successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment with ID: {PaymentId}", id);
            return StatusCode(500, new { Message = "An error occurred while retrieving the payment" });
        }
    }

    /// <summary>
    /// Get payment by appointment ID
    /// </summary>
    [HttpGet("appointment/{appointmentId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPaymentByAppointment(Guid appointmentId)
    {
        try
        {
            var payment = await _paymentService.GetByAppointmentIdAsync(appointmentId);
            if (payment == null)
            {
                return NotFound($"Payment for appointment {appointmentId} was not found");
            }

            return Success(payment, "Get payment successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting payment for appointment ID: {AppointmentId}",
                appointmentId
            );
            return StatusCode(500, new { Message = "An error occurred while retrieving the payment" });
        }
    }

    /// <summary>
    /// Get payment by subscription ID
    /// </summary>
    [HttpGet("subscription/{subscriptionId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPaymentBySubscription(Guid subscriptionId)
    {
        try
        {
            var payment = await _paymentService.GetBySubscriptionIdAsync(subscriptionId);
            if (payment == null)
            {
                return NotFound($"Payment for subscription {subscriptionId} was not found");
            }

            return Success(payment, "Get payment successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting payment for subscription ID: {SubscriptionId}",
                subscriptionId
            );
            return StatusCode(500, new { Message = "An error occurred while retrieving the payment" });
        }
    }

    /// <summary>
    /// Get paged payments by hospital ID
    /// </summary>
    [HttpGet("hospital/{hospitalId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPagedPaymentsByHospital(
        Guid hospitalId,
        [FromQuery] GetPaymentsPagedRequest request
    )
    {
        try
        {
            // Validate request
            var validationResult = await _pagedValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(InvalidRequestDataMessage, errors);
            }

            var pagedResult = await _paymentService.GetPagedByHospitalIdAsync(hospitalId, request);
            return Paginated(
                pagedResult,
                "Get paged payments by hospital successful"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting paged payments for hospital ID: {HospitalId}",
                hospitalId
            );
            return StatusCode(500, new { Message = "An error occurred while retrieving the list of payments" });
        }
    }

    /// <summary>
    /// Get paged payments by patient ID
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPagedPaymentsByPatient(
        Guid patientId,
        [FromQuery] GetPaymentsPagedRequest request
    )
    {
        try
        {
            // Validate request
            var validationResult = await _pagedValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(InvalidRequestDataMessage, errors);
            }

            var pagedResult = await _paymentService.GetPagedByPatientIdAsync(patientId, request);
            return Paginated(
                pagedResult,
                "Get paged payments by patient successful"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting paged payments for patient ID: {PatientId}",
                patientId
            );
            return StatusCode(500, new { Message = "An error occurred while retrieving the list of payments" });
        }
    }

    /// <summary>
    /// Create appointment payment (patient books appointment) and generate payment URL
    /// </summary>
    [HttpPost("appointment")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateAppointmentPayment(
        [FromBody] CreateAppointmentPaymentRequest request
    )
    {
        try
        {
            // Validate request
            var validationResult = await _createAppointmentValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(InvalidRequestDataMessage, errors);
            }

            // Create payment first
            var payment = await _paymentService.CreateAppointmentPaymentAsync(request);

            // Get payment method to determine which gateway to use
            var paymentWithMethod = await _paymentService.GetByIdAsync(payment.Id);
            if (paymentWithMethod == null)
            {
                _logger.LogError("Payment was created but could not be retrieved: {PaymentId}", payment.Id);
                return StatusCode(500, new { Message = "Payment created but could not retrieve payment method information" });
            }

            // Generate payment URL based on payment method
            var paymentMethodName = paymentWithMethod.PaymentMethodName.ToUpper();
            CreateAppointmentPaymentResponse response;

            switch (paymentMethodName)
            {
                case "PAYOS":
                    response = await CreatePayOSPaymentUrl(payment, request);
                    break;
                case "VNPAY":
                    response = await CreateVNPayPaymentUrl(payment, request);
                    break;
                default:
                    _logger.LogWarning("Unsupported payment method: {PaymentMethod}", paymentMethodName);
                    return BadRequest($"Payment method '{paymentMethodName}' is not supported for online payment");
            }

            return Created(response, "Create appointment payment with payment URL successful");
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
            return StatusCode(
                500,
                new { Message = "An error occurred while creating appointment payment" }
            );
        }
    }

    /// <summary>
    /// Create PayOS payment URL for appointment payment
    /// </summary>
    private async Task<CreateAppointmentPaymentResponse> CreatePayOSPaymentUrl(
        PaymentResponse payment,
        CreateAppointmentPaymentRequest request)
    {
        var payOSRequest = new Models.DTOs.PayOS.PayOSPaymentRequest
        {
            PaymentId = payment.Id,
            Amount = payment.Amount,
            Description = $"",
            BuyerInfo = new Models.DTOs.PayOS.PayOSBuyerInfo
            {
                // Note: We don't have buyer info in the request, so we'll leave these empty
                // In a real scenario, you might want to fetch patient info from another service
            },
            Items = new List<Models.DTOs.PayOS.PayOSItemInfo>
            {
                new Models.DTOs.PayOS.PayOSItemInfo
                {
                    Name = "Phí khám bệnh",
                    Quantity = 1,
                    Price = (int)payment.Amount
                }
            }
        };

        var payOSResponse = await _payOSService.CreatePaymentLinkAsync(payOSRequest);

        return new CreateAppointmentPaymentResponse
        {
            Payment = payment,
            PaymentUrl = payOSResponse.CheckoutUrl,
            PaymentGateway = "PayOS",
            ExpireAt = payOSResponse.ExpireAt,
            PaymentReference = payOSResponse.OrderCode.ToString()
        };
    }

    /// <summary>
    /// Create VNPay payment URL for appointment payment
    /// </summary>
    private async Task<CreateAppointmentPaymentResponse> CreateVNPayPaymentUrl(
        PaymentResponse payment,
        CreateAppointmentPaymentRequest request)
    {
        var vnPayRequest = new Models.DTOs.VNPay.VNPayPaymentRequest
        {
            PaymentId = payment.Id,
            Amount = payment.Amount,
            OrderDescription = $"Thanh toán cuộc hẹn - Appointment ID: {request.AppointmentId}",
            ClientIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            CustomerInfo = $"Patient ID: {request.PatientId}" // Basic customer info
        };

        var vnPayResponse = await _vnPayService.CreatePaymentUrlAsync(vnPayRequest);

        return new CreateAppointmentPaymentResponse
        {
            Payment = payment,
            PaymentUrl = vnPayResponse.PaymentUrl,
            PaymentGateway = "VNPay",
            ExpireAt = vnPayResponse.ExpireTime,
            PaymentReference = vnPayResponse.TransactionRef
        };
    }

    /// <summary>
    /// Create subscription payment (clinic subscribes)
    /// </summary>
    [HttpPost("subscription")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateSubscriptionPayment(
        [FromBody] CreateSubscriptionPaymentRequest request
    )
    {
        try
        {
            // Validate request
            var validationResult = await _createSubscriptionValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(InvalidRequestDataMessage, errors);
            }

            var payment = await _paymentService.CreateSubscriptionPaymentAsync(request);
            return Created(payment, "Create subscription payment successful");
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
            return StatusCode(
                500,
                new { Message = "An error occurred while creating subscription payment" }
            );
        }
    }

    /// <summary>
    /// Update payment status
    /// </summary>
    [HttpPut("{id}/status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdatePaymentStatus(
        Guid id,
        [FromBody] UpdatePaymentStatusRequest request
    )
    {
        try
        {
            request.Id = id; // Ensure ID matches route parameter

            // Validate request
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(InvalidRequestDataMessage, errors);
            }

            var payment = await _paymentService.UpdateStatusAsync(request);
            return Success(payment, "Update payment status successful");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when updating payment status");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status for ID: {PaymentId}", id);
            return StatusCode(
                500,
                new { Message = "An error occurred while updating payment status" }
            );
        }
    }

    /// <summary>
    /// Delete payment
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeletePayment(Guid id)
    {
        try
        {
            var result = await _paymentService.DeleteAsync(id);
            if (!result)
            {
                return NotFound($"Payment with ID {id} was not found");
            }

            return Success("Delete payment successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment with ID: {PaymentId}", id);
            return StatusCode(500, new { Message = "An error occurred while deleting the payment" });
        }
    }

    /// <summary>
    /// Get payment statistics
    /// If FromDate/ToDate not provided: default to last 6 months and monthly statistics
    /// </summary>
    [HttpGet("statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPaymentStatistics([FromQuery] GetPaymentStatisticsRequest request)
    {
        try
        {
            // Log info about default values when not provided
            var fromDate = request.GetFromDate();
            var toDate = request.GetToDate();
            var usingDefaults = !request.FromDate.HasValue || !request.ToDate.HasValue;

            if (usingDefaults)
            {
                _logger.LogInformation("Using default date range for statistics: {FromDate} to {ToDate}, Period: {Period}",
                    fromDate, toDate, request.Period);
            }

            // Validate request
            var validationResult = await _statisticsValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(InvalidRequestDataMessage, errors);
            }

            var statistics = await _paymentService.GetPaymentStatisticsAsync(request);

            var message = usingDefaults
                ? $"Get payment statistics successful (default: {statistics.DateRange})"
                : "Get payment statistics successful";

            return Success(statistics, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment statistics");
            return StatusCode(500, new { Message = "An error occurred while retrieving payment statistics" });
        }
    }
}
