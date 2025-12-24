using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for payment operations
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class PaymentsController(
    IPaymentService paymentService,
    IPaymentMethodService paymentMethodService,
    PaymentGatewayServices gatewayServices,
    IPaymentValidationService validationService,
    BookingCare.Services.Hospital.HospitalSubscriptionGrpc.HospitalSubscriptionGrpcClient hospitalSubscriptionClient,
    ILogger<PaymentsController> logger
) : BaseApiController
{
    private const string InvalidRequestDataMessage = "Invalid request data";
    private const string PayOSGateway = "PAYOS";
    private const string VNPayGateway = "VNPAY";
    private const string StripeGateway = "STRIPE";

    private readonly IPaymentService _paymentService = paymentService;
    private readonly IPaymentMethodService _paymentMethodService = paymentMethodService;
    private readonly IPayOSService _payOSService = gatewayServices.PayOSService;
    private readonly IVNPayService _vnPayService = gatewayServices.VNPayService;
    private readonly IStripeService _stripeService = gatewayServices.StripeService;
    private readonly IPaymentValidationService _validationService = validationService;
    private readonly BookingCare.Services.Hospital.HospitalSubscriptionGrpc.HospitalSubscriptionGrpcClient _hospitalSubscriptionClient =
        hospitalSubscriptionClient;
    private readonly ILogger<PaymentsController> _logger = logger;

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
            return StatusCode(
                500,
                new { Message = "An error occurred while retrieving the payment" }
            );
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
            return StatusCode(
                500,
                new { Message = "An error occurred while retrieving the payment" }
            );
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
            return StatusCode(
                500,
                new { Message = "An error occurred while retrieving the payment" }
            );
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
            var validationResult = await _validationService.ValidateGetPaymentsPagedAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(InvalidRequestDataMessage, errors);
            }

            var pagedResult = await _paymentService.GetPagedByHospitalIdAsync(hospitalId, request);
            return Paginated(pagedResult, "Get paged payments by hospital successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting paged payments for hospital ID: {HospitalId}",
                hospitalId
            );
            return StatusCode(
                500,
                new { Message = "An error occurred while retrieving the list of payments" }
            );
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
            var validationResult = await _validationService.ValidateGetPaymentsPagedAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(InvalidRequestDataMessage, errors);
            }

            var pagedResult = await _paymentService.GetPagedByPatientIdAsync(patientId, request);
            return Paginated(pagedResult, "Get paged payments by patient successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting paged payments for patient ID: {PatientId}",
                patientId
            );
            return StatusCode(
                500,
                new { Message = "An error occurred while retrieving the list of payments" }
            );
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
            var validationResult = await _validationService.ValidateCreateAppointmentPaymentAsync(
                request
            );
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
                _logger.LogError(
                    "Payment was created but could not be retrieved: {PaymentId}",
                    payment.Id
                );
                return StatusCode(
                    500,
                    new
                    {
                        Message = "Payment created but could not retrieve payment method information",
                    }
                );
            }

            // Generate payment URL based on payment method
            var paymentMethodName = paymentWithMethod.PaymentMethodName.ToUpper();
            CreateAppointmentPaymentResponse response;

            switch (paymentMethodName)
            {
                case PayOSGateway:
                    try
                    {
                        response = await CreatePayOSPaymentUrl(payment);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "PayOS payment failed, falling back to VNPay for PaymentId: {PaymentId}",
                            payment.Id
                        );
                        // Fallback to VNPay if PayOS fails
                        response = await CreateVNPayPaymentUrl(payment, request);
                    }
                    break;
                case VNPayGateway:
                    response = await CreateVNPayPaymentUrl(payment, request);
                    break;
                case StripeGateway:
                    response = await CreateStripePaymentUrl(payment, request);
                    break;
                default:
                    _logger.LogWarning(
                        "Unsupported payment method: {PaymentMethod}",
                        paymentMethodName
                    );
                    return BadRequest(
                        $"Payment method '{paymentMethodName}' is not supported for online payment"
                    );
            }

            return Created(response, "Create appointment payment with payment URL successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment payment");
            return HandlePaymentCreationError(ex, "appointment");
        }
    }

    /// <summary>
    /// Create PayOS payment URL for appointment payment
    /// </summary>
    private async Task<CreateAppointmentPaymentResponse> CreatePayOSPaymentUrl(
        PaymentResponse payment
    )
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
                    Price = (int)payment.Amount,
                },
            },
        };

        var payOSResponse = await _payOSService.CreatePaymentLinkAsync(payOSRequest);

        return new CreateAppointmentPaymentResponse
        {
            Payment = payment,
            PaymentUrl = payOSResponse.CheckoutUrl,
            PaymentGateway = "PayOS",
            ExpireAt = payOSResponse.ExpireAt,
            PaymentReference = payOSResponse.OrderCode.ToString(),
        };
    }

    /// <summary>
    /// Create VNPay payment URL for appointment payment
    /// </summary>
    private async Task<CreateAppointmentPaymentResponse> CreateVNPayPaymentUrl(
        PaymentResponse payment,
        CreateAppointmentPaymentRequest request
    )
    {
        var vnPayRequest = new Models.DTOs.VNPay.VNPayPaymentRequest
        {
            PaymentId = payment.Id,
            Amount = payment.Amount,
            OrderDescription = $"Thanh toán cuộc hẹn - Appointment ID: {request.AppointmentId}",
            ClientIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            CustomerInfo =
                $"Patient ID: {request.PatientId}" // Basic customer info
            ,
        };

        var vnPayResponse = await _vnPayService.CreatePaymentUrlAsync(vnPayRequest);

        return new CreateAppointmentPaymentResponse
        {
            Payment = payment,
            PaymentUrl = vnPayResponse.PaymentUrl,
            PaymentGateway = "VNPay",
            ExpireAt = vnPayResponse.ExpireTime,
            PaymentReference = vnPayResponse.TransactionRef,
        };
    }

    /// <summary>
    /// Create Stripe payment URL for appointment payment
    /// </summary>
    private async Task<CreateAppointmentPaymentResponse> CreateStripePaymentUrl(
        PaymentResponse payment,
        CreateAppointmentPaymentRequest request
    )
    {
        var stripeRequest = new Models.DTOs.Stripe.StripePaymentRequest
        {
            PaymentId = payment.Id,
            Amount = payment.Amount,
            Description = $"Thanh toán cuộc hẹn - Appointment ID: {request.AppointmentId}",
            AppointmentId = request.AppointmentId,
            PatientId = request.PatientId,
            LineItems = new List<Models.DTOs.Stripe.StripeLineItem>
            {
                new Models.DTOs.Stripe.StripeLineItem
                {
                    Name = "Phí khám bệnh",
                    Quantity = 1,
                    Price = payment.Amount,
                    Description = $"Appointment ID: {request.AppointmentId}",
                },
            },
        };

        var stripeResponse = await _stripeService.CreateCheckoutSessionAsync(stripeRequest);

        return new CreateAppointmentPaymentResponse
        {
            Payment = payment,
            PaymentUrl = stripeResponse.CheckoutUrl,
            PaymentGateway = "Stripe",
            ExpireAt = stripeResponse.ExpireAt,
            PaymentReference = stripeResponse.SessionId,
        };
    }

    /// <summary>
    /// Create PayOS payment URL for supplementary payment
    /// </summary>
    private async Task<CreateSupplementaryPaymentResponse> CreatePayOSSupplementaryPaymentUrl(
        CreateSupplementaryPaymentRequest request,
        string supplementaryPaymentId
    )
    {
        var existingPayment = await _paymentService.GetByAppointmentIdAsync(request.AppointmentId);
        var gatewayPaymentId = existingPayment?.Id ?? Guid.NewGuid(); // fallback only if truly no base payment

        var payOSRequest = new Models.DTOs.PayOS.PayOSPaymentRequest
        {
            PaymentId = gatewayPaymentId,
            Amount = request.AdditionalAmount,
            // Add supplementary payment identifier and IsStaffAssigned flag in Description for callback handling
            Description =
                $"SUPP_PAYMENT:{supplementaryPaymentId}:APPT:{request.AppointmentId}:STAFF_ASSIGNED:{request.IsStaffAssigned}",
            BuyerInfo = new Models.DTOs.PayOS.PayOSBuyerInfo
            {
                // Note: We don't have buyer info in the request, so we'll leave these empty
                // In a real scenario, you might want to fetch patient info from another service
            },
            Items = new List<Models.DTOs.PayOS.PayOSItemInfo>
            {
                new Models.DTOs.PayOS.PayOSItemInfo
                {
                    Name = "Phí khám bệnh bổ sung",
                    Quantity = 1,
                    Price = (int)request.AdditionalAmount,
                },
            },
        };

        var payOSResponse = await _payOSService.CreatePaymentLinkAsync(payOSRequest);

        return new CreateSupplementaryPaymentResponse
        {
            AppointmentId = request.AppointmentId,
            AdditionalAmount = request.AdditionalAmount,
            PaymentUrl = payOSResponse.CheckoutUrl,
            PaymentGateway = "PayOS",
            ExpireAt = payOSResponse.ExpireAt,
            PaymentReference = payOSResponse.OrderCode.ToString(),
            SupplementaryPaymentId = supplementaryPaymentId,
        };
    }

    /// <summary>
    /// Create VNPay payment URL for supplementary payment
    /// </summary>
    private async Task<CreateSupplementaryPaymentResponse> CreateVNPaySupplementaryPaymentUrl(
        CreateSupplementaryPaymentRequest request,
        string supplementaryPaymentId
    )
    {
        var existingPayment = await _paymentService.GetByAppointmentIdAsync(request.AppointmentId);
        var gatewayPaymentId = existingPayment?.Id ?? Guid.NewGuid(); // fallback only if truly no base payment

        var vnPayRequest = new Models.DTOs.VNPay.VNPayPaymentRequest
        {
            PaymentId = gatewayPaymentId, // Temporary ID for VNPay
            Amount = request.AdditionalAmount,
            OrderDescription =
                $"Thanh toán bổ sung - Appointment ID: {request.AppointmentId} - {request.Reason}",
            ClientIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            CustomerInfo = $"Patient ID: {request.PatientId}",
            // Add supplementary payment identifier and IsStaffAssigned flag for callback handling
            OrderInfo =
                $"SUPP_PAYMENT:{supplementaryPaymentId}:APPT:{request.AppointmentId}:STAFF_ASSIGNED:{request.IsStaffAssigned}",
        };

        var vnPayResponse = await _vnPayService.CreatePaymentUrlAsync(vnPayRequest);

        return new CreateSupplementaryPaymentResponse
        {
            AppointmentId = request.AppointmentId,
            AdditionalAmount = request.AdditionalAmount,
            PaymentUrl = vnPayResponse.PaymentUrl,
            PaymentGateway = "VNPay",
            ExpireAt = vnPayResponse.ExpireTime,
            PaymentReference = vnPayResponse.TransactionRef,
            SupplementaryPaymentId = supplementaryPaymentId,
        };
    }

    /// <summary>
    /// Create Stripe payment URL for supplementary payment
    /// </summary>
    private async Task<CreateSupplementaryPaymentResponse> CreateStripeSupplementaryPaymentUrl(
        CreateSupplementaryPaymentRequest request,
        string supplementaryPaymentId
    )
    {
        var existingPayment = await _paymentService.GetByAppointmentIdAsync(request.AppointmentId);
        var gatewayPaymentId = existingPayment?.Id ?? Guid.NewGuid();

        var stripeRequest = new Models.DTOs.Stripe.StripePaymentRequest
        {
            PaymentId = gatewayPaymentId,
            Amount = request.AdditionalAmount,
            Description =
                $"Thanh toán bổ sung - Appointment ID: {request.AppointmentId} - {request.Reason}",
            AppointmentId = request.AppointmentId,
            PatientId = request.PatientId,
            LineItems = new List<Models.DTOs.Stripe.StripeLineItem>
            {
                new Models.DTOs.Stripe.StripeLineItem
                {
                    Name = "Phí khám bệnh bổ sung",
                    Quantity = 1,
                    Price = request.AdditionalAmount,
                    Description = request.Reason,
                },
            },
            Metadata = new Dictionary<string, string>
            {
                { "SupplementaryPaymentId", supplementaryPaymentId },
                { "IsStaffAssigned", request.IsStaffAssigned.ToString() },
                { "AppointmentId", request.AppointmentId.ToString() },
            },
        };

        var stripeResponse = await _stripeService.CreateCheckoutSessionAsync(stripeRequest);

        return new CreateSupplementaryPaymentResponse
        {
            AppointmentId = request.AppointmentId,
            AdditionalAmount = request.AdditionalAmount,
            PaymentUrl = stripeResponse.CheckoutUrl,
            PaymentGateway = "Stripe",
            ExpireAt = stripeResponse.ExpireAt,
            PaymentReference = stripeResponse.SessionId,
            SupplementaryPaymentId = supplementaryPaymentId,
        };
    }

    /// <summary>
    /// Create supplementary payment for appointment price difference (Option 3)
    /// Used when patient chooses new doctor with higher price
    /// Does not create new Payment entity, only generates payment URL for price difference
    /// </summary>
    [HttpPost("supplementary")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateSupplementaryPayment(
        [FromBody] CreateSupplementaryPaymentRequest request
    )
    {
        try
        {
            var validationResult = await _validationService.ValidateCreateSupplementaryPaymentAsync(
                request
            );
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(InvalidRequestDataMessage, errors);
            }

            // Generate unique supplementary payment ID
            var supplementaryPaymentId =
                $"SUPP_{request.AppointmentId}_{DateTime.UtcNow:yyyyMMddHHmmss}";

            // Get payment method to determine which gateway to use
            var paymentMethod = await _paymentMethodService.GetByIdAsync(request.PaymentMethodId);
            if (paymentMethod == null)
            {
                _logger.LogError(
                    "Payment method not found for ID: {PaymentMethodId}",
                    request.PaymentMethodId
                );
                return BadRequest($"Payment method with ID {request.PaymentMethodId} not found");
            }

            var paymentMethodName = paymentMethod.Name?.ToUpperInvariant() ?? "VNPAY";

            // Generate payment URL based on payment method
            CreateSupplementaryPaymentResponse response;

            switch (paymentMethodName)
            {
                case PayOSGateway:
                    try
                    {
                        response = await CreatePayOSSupplementaryPaymentUrl(
                            request,
                            supplementaryPaymentId
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "PayOS supplementary payment failed, falling back to VNPay for PaymentMethodId: {PaymentMethodId}",
                            request.PaymentMethodId
                        );
                        // Fallback to VNPay if PayOS fails
                        response = await CreateVNPaySupplementaryPaymentUrl(
                            request,
                            supplementaryPaymentId
                        );
                    }
                    break;
                case VNPayGateway:
                    response = await CreateVNPaySupplementaryPaymentUrl(
                        request,
                        supplementaryPaymentId
                    );
                    break;
                case StripeGateway:
                    response = await CreateStripeSupplementaryPaymentUrl(
                        request,
                        supplementaryPaymentId
                    );
                    break;
                default:
                    _logger.LogWarning(
                        "Unsupported payment method for supplementary payment: {PaymentMethod}",
                        paymentMethodName
                    );
                    return BadRequest(
                        $"Payment method '{paymentMethodName}' is not supported for supplementary payment"
                    );
            }

            return Created(response, "Create supplementary payment URL successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplementary payment");
            return HandlePaymentCreationError(ex, "supplementary");
        }
    }

    /// <summary>
    /// Create subscription payment (hospital subscribes to a plan) and generate payment URL
    /// After successful payment, the subscription will be created/upgraded via gRPC
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
            var validationResult = await _validationService.ValidateCreateSubscriptionPaymentAsync(
                request
            );
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(InvalidRequestDataMessage, errors);
            }

            // Additional validation for upgrade scenario
            if (request.IsUpgrade && !request.CurrentHospitalSubscriptionId.HasValue)
            {
                return BadRequest(
                    "CurrentHospitalSubscriptionId is required when IsUpgrade is true"
                );
            }

            // Create payment first
            var payment = await _paymentService.CreateSubscriptionPaymentAsync(request);

            // Get payment method to determine which gateway to use
            var paymentWithMethod = await _paymentService.GetByIdAsync(payment.Id);
            if (paymentWithMethod == null)
            {
                _logger.LogError(
                    "Payment was created but could not be retrieved: {PaymentId}",
                    payment.Id
                );
                return StatusCode(
                    500,
                    new
                    {
                        Message = "Payment created but could not retrieve payment method information",
                    }
                );
            }

            // Generate payment URL based on payment method
            var paymentMethodName = paymentWithMethod.PaymentMethodName.ToUpper();
            CreateSubscriptionPaymentResponse response;

            switch (paymentMethodName)
            {
                case PayOSGateway:
                    try
                    {
                        response = await CreatePayOSSubscriptionPaymentUrl(payment, request);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "PayOS payment failed, falling back to VNPay for PaymentId: {PaymentId}",
                            payment.Id
                        );
                        // Fallback to VNPay if PayOS fails
                        response = await CreateVNPaySubscriptionPaymentUrl(payment, request);
                    }
                    break;
                case VNPayGateway:
                    response = await CreateVNPaySubscriptionPaymentUrl(payment, request);
                    break;
                case StripeGateway:
                    response = await CreateStripeSubscriptionPaymentUrl(payment, request);
                    break;
                default:
                    _logger.LogWarning(
                        "Unsupported payment method for subscription: {PaymentMethod}",
                        paymentMethodName
                    );
                    return BadRequest(
                        $"Payment method '{paymentMethodName}' is not supported for subscription payment"
                    );
            }

            return Created(response, "Create subscription payment with payment URL successful");
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
            return HandlePaymentCreationError(ex, "subscription");
        }
    }

    /// <summary>
    /// Create PayOS payment URL for subscription payment
    /// </summary>
    private async Task<CreateSubscriptionPaymentResponse> CreatePayOSSubscriptionPaymentUrl(
        PaymentResponse payment,
        CreateSubscriptionPaymentRequest request
    )
    {
        var paymentType = request.IsUpgrade ? "Nâng cấp gói đăng ký" : "Đăng ký gói dịch vụ";

        var payOSRequest = new Models.DTOs.PayOS.PayOSPaymentRequest
        {
            PaymentId = payment.Id,
            Amount = payment.Amount,
            Description = $"",
            BuyerInfo = new Models.DTOs.PayOS.PayOSBuyerInfo
            {
                // Hospital info - could be fetched from Hospital service if needed
            },
            Items = new List<Models.DTOs.PayOS.PayOSItemInfo>
            {
                new Models.DTOs.PayOS.PayOSItemInfo
                {
                    Name = paymentType,
                    Quantity = 1,
                    Price = (int)payment.Amount,
                },
            },
            // Subscription metadata
            SubscriptionPlanId = request.SubscriptionId,
            HospitalId = request.HospitalId,
            IsSubscriptionUpgrade = request.IsUpgrade,
            CurrentHospitalSubscriptionId = request.CurrentHospitalSubscriptionId,
            PlanType = request.PlanType,
        };

        var payOSResponse = await _payOSService.CreatePaymentLinkAsync(payOSRequest);

        return new CreateSubscriptionPaymentResponse
        {
            Payment = payment,
            PaymentUrl = payOSResponse.CheckoutUrl,
            PaymentGateway = "PayOS",
            ExpireAt = payOSResponse.ExpireAt,
            PaymentReference = payOSResponse.OrderCode.ToString(),
            IsUpgrade = request.IsUpgrade,
            SubscriptionId = request.SubscriptionId,
            HospitalId = request.HospitalId,
            CurrentSubscriptionId = request.CurrentHospitalSubscriptionId,
        };
    }

    /// <summary>
    /// Create VNPay payment URL for subscription payment
    /// </summary>
    private async Task<CreateSubscriptionPaymentResponse> CreateVNPaySubscriptionPaymentUrl(
        PaymentResponse payment,
        CreateSubscriptionPaymentRequest request
    )
    {
        var paymentType = request.IsUpgrade ? "Nâng cấp gói đăng ký" : "Đăng ký gói dịch vụ";

        // Build OrderInfo with optional CURRENT and PLAN_TYPE fields
        var orderInfo =
            $"SUBSCRIPTION:{request.SubscriptionId}:HOSPITAL:{request.HospitalId}:UPGRADE:{request.IsUpgrade}";
        if (request.IsUpgrade && request.CurrentHospitalSubscriptionId.HasValue)
        {
            orderInfo += $":CURRENT:{request.CurrentHospitalSubscriptionId.Value}";
        }
        if (!string.IsNullOrEmpty(request.PlanType))
        {
            orderInfo += $":PLAN_TYPE:{request.PlanType}";
        }

        var vnPayRequest = new Models.DTOs.VNPay.VNPayPaymentRequest
        {
            PaymentId = payment.Id,
            Amount = payment.Amount,
            OrderDescription = $"{paymentType} - Hospital ID: {request.HospitalId}",
            ClientIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            CustomerInfo = $"Hospital ID: {request.HospitalId}",
            OrderInfo = orderInfo,
        };

        var vnPayResponse = await _vnPayService.CreatePaymentUrlAsync(vnPayRequest);

        return new CreateSubscriptionPaymentResponse
        {
            Payment = payment,
            PaymentUrl = vnPayResponse.PaymentUrl,
            PaymentGateway = "VNPay",
            ExpireAt = vnPayResponse.ExpireTime,
            PaymentReference = vnPayResponse.TransactionRef,
            IsUpgrade = request.IsUpgrade,
            SubscriptionId = request.SubscriptionId,
            HospitalId = request.HospitalId,
            CurrentSubscriptionId = request.CurrentHospitalSubscriptionId,
        };
    }

    /// <summary>
    /// Create Stripe payment URL for subscription payment
    /// </summary>
    private async Task<CreateSubscriptionPaymentResponse> CreateStripeSubscriptionPaymentUrl(
        PaymentResponse payment,
        CreateSubscriptionPaymentRequest request
    )
    {
        var paymentType = request.IsUpgrade ? "Nâng cấp gói đăng ký" : "Đăng ký gói dịch vụ";

        var stripeRequest = new Models.DTOs.Stripe.StripePaymentRequest
        {
            PaymentId = payment.Id,
            Amount = payment.Amount,
            Description = $"{paymentType} - Hospital ID: {request.HospitalId}",
            SubscriptionPlanId = request.SubscriptionId,
            HospitalId = request.HospitalId,
            IsSubscriptionUpgrade = request.IsUpgrade,
            CurrentHospitalSubscriptionId = request.CurrentHospitalSubscriptionId,
            PlanType = request.PlanType,
            LineItems = new List<Models.DTOs.Stripe.StripeLineItem>
            {
                new Models.DTOs.Stripe.StripeLineItem
                {
                    Name = paymentType,
                    Quantity = 1,
                    Price = payment.Amount,
                    Description = $"Hospital ID: {request.HospitalId}",
                },
            },
        };

        var stripeResponse = await _stripeService.CreateCheckoutSessionAsync(stripeRequest);

        return new CreateSubscriptionPaymentResponse
        {
            Payment = payment,
            PaymentUrl = stripeResponse.CheckoutUrl,
            PaymentGateway = "Stripe",
            ExpireAt = stripeResponse.ExpireAt,
            PaymentReference = stripeResponse.SessionId,
            IsUpgrade = request.IsUpgrade,
            SubscriptionId = request.SubscriptionId,
            HospitalId = request.HospitalId,
            CurrentSubscriptionId = request.CurrentHospitalSubscriptionId,
        };
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
            var validationResult = await _validationService.ValidateUpdatePaymentStatusAsync(
                request
            );
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
            return StatusCode(
                500,
                new { Message = "An error occurred while deleting the payment" }
            );
        }
    }

    /// <summary>
    /// Get payment statistics for subscription revenue
    /// Admin dashboard shows ONLY subscription revenue (not appointment payments)
    /// If FromDate/ToDate not provided: default to last 6 months and monthly statistics
    /// </summary>
    [HttpGet("statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPaymentStatistics(
        [FromQuery] GetPaymentStatisticsRequest request
    )
    {
        try
        {
            // Log info about default values when not provided
            var fromDate = request.GetFromDate();
            var toDate = request.GetToDate();
            var usingDefaults = !request.FromDate.HasValue || !request.ToDate.HasValue;

            if (usingDefaults)
            {
                _logger.LogInformation(
                    "Using default date range for subscription revenue statistics: {FromDate} to {ToDate}, Period: {Period}",
                    fromDate,
                    toDate,
                    request.Period
                );
            }

            // Validate request
            var validationResult = await _validationService.ValidateGetPaymentStatisticsAsync(
                request
            );
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(InvalidRequestDataMessage, errors);
            }

            var statistics = await _paymentService.GetPaymentStatisticsAsync(request);

            var message = usingDefaults
                ? $"Get subscription revenue statistics successful (default: {statistics.DateRange})"
                : "Get subscription revenue statistics successful";

            return Success(statistics, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription revenue statistics");
            return StatusCode(
                500,
                new
                {
                    Message = "An error occurred while retrieving subscription revenue statistics",
                }
            );
        }
    }

    /// <summary>
    /// Handle common payment creation errors
    /// </summary>
    private IActionResult HandlePaymentCreationError(Exception ex, string operationType)
    {
        return ex switch
        {
            ArgumentException argEx => BadRequest(argEx.Message),
            InvalidOperationException invOpEx => Conflict(invOpEx.Message),
            _ => StatusCode(
                500,
                new { Message = $"An error occurred while creating {operationType} payment" }
            ),
        };
    }
}
