using AutoMapper;
using BookingCare.Services.Payment.Enums;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Services;
using Grpc.Core;
using DiscountProtos = BookingCare.Services.Discount.Protos;

namespace BookingCare.Services.Payment.Services.Implementations;

/// <summary>
/// Implementation of Payment Service
/// </summary>
public class PaymentService : BaseService, IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IAppointmentDetailsService _appointmentDetailsService;
    private readonly DiscountProtos.DiscountService.DiscountServiceClient _discountGrpcClient;
    private readonly IMapper _mapper;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IAppointmentDetailsService appointmentDetailsService,
        DiscountProtos.DiscountService.DiscountServiceClient discountGrpcClient,
        IMapper mapper,
        ILogger<PaymentService> logger
    )
        : base(logger)
    {
        _paymentRepository = paymentRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _appointmentDetailsService = appointmentDetailsService;
        _discountGrpcClient = discountGrpcClient;
        _mapper = mapper;
    }

    /// <summary>
    /// Get payment by ID - Simple read operation
    /// </summary>
    public async Task<PaymentResponse?> GetByIdAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);
        return payment != null ? _mapper.Map<PaymentResponse>(payment) : null;
    }

    /// <summary>
    /// Get payment by appointment ID - Simple read operation
    /// </summary>
    public async Task<PaymentResponse?> GetByAppointmentIdAsync(Guid appointmentId)
    {
        var payment = await _paymentRepository.GetByAppointmentIdAsync(appointmentId);
        return payment != null ? _mapper.Map<PaymentResponse>(payment) : null;
    }

    /// <summary>
    /// Get payment by subscription ID - Simple read operation
    /// </summary>
    public async Task<PaymentResponse?> GetBySubscriptionIdAsync(Guid subscriptionId)
    {
        var payment = await _paymentRepository.GetBySubscriptionIdAsync(subscriptionId);
        return payment != null ? _mapper.Map<PaymentResponse>(payment) : null;
    }

    /// <summary>
    /// Get list of payments by hospital ID - Simple read operation
    /// </summary>
    public async Task<IEnumerable<PaymentResponse>> GetByHospitalIdAsync(Guid hospitalId)
    {
        var payments = await _paymentRepository.GetByHospitalIdAsync(hospitalId);
        return _mapper.Map<IEnumerable<PaymentResponse>>(payments);
    }

    /// <summary>
    /// Get list of payments by patient ID - Simple read operation
    /// </summary>
    public async Task<IEnumerable<PaymentResponse>> GetByPatientIdAsync(Guid patientId)
    {
        var payments = await _paymentRepository.GetByPatientIdAsync(patientId);
        return _mapper.Map<IEnumerable<PaymentResponse>>(payments);
    }

    /// <summary>
    /// Get list of payments by hospital ID with pagination - Simple read operation
    /// </summary>
    public async Task<PagedResult<PaymentResponse>> GetPagedByHospitalIdAsync(
        Guid hospitalId,
        GetPaymentsPagedRequest request
    )
    {
        var pagedResult = await _paymentRepository.GetPagedByHospitalIdAsync(hospitalId, request);

        return new PagedResult<PaymentResponse>
        {
            Items = _mapper.Map<List<PaymentResponse>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
        };
    }

    /// <summary>
    /// Get list of payments by patient ID with pagination - Enhanced with appointment details
    /// </summary>
    public async Task<PagedResult<PaymentResponse>> GetPagedByPatientIdAsync(
        Guid patientId,
        GetPaymentsPagedRequest request
    )
    {
        var pagedResult = await _paymentRepository.GetPagedByPatientIdAsync(patientId, request);

        var paymentResponses = _mapper.Map<List<PaymentResponse>>(pagedResult.Items);

        // Enrich payments with appointment details for appointment-related payments
        await EnrichPaymentsWithAppointmentDetailsAsync(paymentResponses);

        return new PagedResult<PaymentResponse>
        {
            Items = paymentResponses,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
        };
    }

    /// <summary>
    /// Enrich payment responses with appointment details by calling Appointment Service via gRPC
    /// </summary>
    private async Task EnrichPaymentsWithAppointmentDetailsAsync(List<PaymentResponse> payments)
    {
        if (!payments.Any())
            return;

        // Filter payments that have appointment IDs
        var appointmentPayments = payments.Where(p => p.AppointmentId.HasValue).ToList();

        if (!appointmentPayments.Any())
            return;

        LogInfo(
            "Enriching {Count} payments with appointment details",
            null,
            appointmentPayments.Count
        );

        // Get appointment details for each payment concurrently
        var tasks = appointmentPayments.Select(async payment =>
        {
            try
            {
                var appointmentDetails =
                    await _appointmentDetailsService.GetAppointmentDetailsAsync(
                        payment.AppointmentId!.Value
                    );
                if (appointmentDetails != null)
                {
                    payment.AppointmentDate = appointmentDetails.AppointmentDate;
                    payment.AppointmentType = appointmentDetails.AppointmentType;
                }
                else
                {
                    LogWarning(
                        "Could not retrieve appointment details for AppointmentId: {AppointmentId}",
                        null,
                        payment.AppointmentId!.Value
                    );
                }
            }
            catch (Exception ex)
            {
                LogError(
                    ex,
                    "Error enriching payment {PaymentId} with appointment details for AppointmentId: {AppointmentId}",
                    null,
                    payment.Id,
                    payment.AppointmentId!.Value
                );
            }
        });

        await Task.WhenAll(tasks);

        LogInfo("Completed enriching payments with appointment details", null);
    }

    /// <summary>
    /// Create new payment - Business operation MUST use ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentResponse> CreateAsync(CreatePaymentRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                // Validation
                ValidateRequired(request, nameof(request));
                ValidateGuid(request.PaymentMethodId, nameof(request.PaymentMethodId));

                // Validate AppointmentId if present
                if (request.AppointmentId.HasValue)
                {
                    ValidateGuid(request.AppointmentId.Value, nameof(request.AppointmentId));
                }

                // Validate SubscriptionId if present
                if (request.SubscriptionId.HasValue)
                {
                    ValidateGuid(request.SubscriptionId.Value, nameof(request.SubscriptionId));
                }

                // Validate payment method exists
                var paymentMethodExists = await _paymentMethodRepository.ExistsAsync(
                    request.PaymentMethodId
                );
                if (!paymentMethodExists)
                {
                    LogError(
                        new ArgumentException(
                            $"Payment method with ID {request.PaymentMethodId} does not exist"
                        ),
                        "Payment method does not exist: {PaymentMethodId}",
                        null,
                        request.PaymentMethodId
                    );
                    throw new ArgumentException(
                        $"Payment method with ID {request.PaymentMethodId} does not exist"
                    );
                }

                // Check if payment already exists for this appointment (if AppointmentId is present)
                if (request.AppointmentId.HasValue)
                {
                    var existingPayment = await _paymentRepository.GetByAppointmentIdAsync(
                        request.AppointmentId.Value
                    );
                    if (existingPayment != null)
                    {
                        LogError(
                            new InvalidOperationException(
                                $"Payment already exists for appointment {request.AppointmentId}"
                            ),
                            "Payment already exists for appointment: {AppointmentId}",
                            null,
                            request.AppointmentId
                        );
                        throw new InvalidOperationException(
                            $"Payment already exists for appointment {request.AppointmentId}"
                        );
                    }
                }

                // Business logic
                var paymentEntity = _mapper.Map<PaymentEntity>(request);
                var createdPayment = await _paymentRepository.CreateAsync(paymentEntity);

                LogInfo(
                    "Payment created successfully with ID: {PaymentId}",
                    null,
                    createdPayment.Id
                );
                return _mapper.Map<PaymentResponse>(createdPayment);
            },
            "CreatePayment"
        );
    }

    /// <summary>
    /// Create payment for appointment (patient books appointment) - Business operation MUST use ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentResponse> CreateAppointmentPaymentAsync(
        CreateAppointmentPaymentRequest request
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Starting to create payment for appointment: {AppointmentId}",
                    null,
                    request.AppointmentId
                );

                // Validation
                ValidateRequired(request, nameof(request));
                ValidateGuid(request.AppointmentId, nameof(request.AppointmentId));
                ValidateGuid(request.PatientId, nameof(request.PatientId));
                ValidateGuid(request.PaymentMethodId, nameof(request.PaymentMethodId));

                // Validate payment method exists
                var paymentMethodExists = await _paymentMethodRepository.ExistsAsync(
                    request.PaymentMethodId
                );
                if (!paymentMethodExists)
                {
                    LogError(
                        new ArgumentException(
                            $"Payment method with ID {request.PaymentMethodId} does not exist"
                        ),
                        "Payment method does not exist: {PaymentMethodId}",
                        null,
                        request.PaymentMethodId
                    );
                    throw new ArgumentException(
                        $"Payment method with ID {request.PaymentMethodId} does not exist"
                    );
                }

                // Check if payment already exists for this appointment
                var existingPayment = await _paymentRepository.GetByAppointmentIdAsync(
                    request.AppointmentId
                );
                if (existingPayment != null)
                {
                    LogError(
                        new InvalidOperationException(
                            $"Payment already exists for appointment {request.AppointmentId}"
                        ),
                        "Payment already exists for appointment: {AppointmentId}",
                        null,
                        request.AppointmentId
                    );
                    throw new InvalidOperationException(
                        $"Payment already exists for appointment {request.AppointmentId}"
                    );
                }

                // Discount has already been validated by frontend, just store the ID and code
                if (request.DiscountId.HasValue)
                {
                    LogInfo(
                        "Payment with discount: DiscountId={DiscountId}, Code={Code}, AppointmentId={AppointmentId}",
                        null,
                        request.DiscountId.Value,
                        request.DiscountCode ?? "N/A",
                        request.AppointmentId
                    );
                }

                // Business logic - Map to PaymentEntity
                var paymentEntity = new PaymentEntity
                {
                    AppointmentId = request.AppointmentId,
                    PatientId = request.PatientId,
                    HospitalId = request.HospitalId,
                    SubscriptionId = null,
                    Amount = request.Amount,
                    TransactionType = TransactionType.APPOINTMENT,
                    PaymentMethodId = request.PaymentMethodId,
                    DiscountId = request.DiscountId,
                    DiscountCode = request.DiscountCode,
                    Status = PaymentStatus.PENDING,
                    CreatedAt = DateTime.UtcNow,
                };

                var createdPayment = await _paymentRepository.CreateAsync(paymentEntity);

                LogInfo(
                    "Payment for appointment created successfully with ID: {PaymentId}",
                    null,
                    createdPayment.Id
                );
                return _mapper.Map<PaymentResponse>(createdPayment);
            },
            "CreateAppointmentPayment"
        );
    }

    /// <summary>
    /// Create payment for subscription (hospital subscribes to package) - Business operation MUST use ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentResponse> CreateSubscriptionPaymentAsync(
        CreateSubscriptionPaymentRequest request
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Starting to create payment for subscription: {SubscriptionId}",
                    null,
                    request.SubscriptionId
                );

                // Validation
                ValidateRequired(request, nameof(request));
                ValidateGuid(request.SubscriptionId, nameof(request.SubscriptionId));
                ValidateGuid(request.HospitalId, nameof(request.HospitalId));
                ValidateGuid(request.PaymentMethodId, nameof(request.PaymentMethodId));

                // Validate payment method exists
                var paymentMethodExists = await _paymentMethodRepository.ExistsAsync(
                    request.PaymentMethodId
                );
                if (!paymentMethodExists)
                {
                    LogError(
                        new ArgumentException(
                            $"Payment method with ID {request.PaymentMethodId} does not exist"
                        ),
                        "Payment method does not exist: {PaymentMethodId}",
                        null,
                        request.PaymentMethodId
                    );
                    throw new ArgumentException(
                        $"Payment method with ID {request.PaymentMethodId} does not exist"
                    );
                }

                // Business logic - Map to PaymentEntity
                var paymentEntity = new PaymentEntity
                {
                    AppointmentId = null,
                    PatientId = null,
                    HospitalId = request.HospitalId,
                    SubscriptionId = request.SubscriptionId,
                    Amount = request.Amount,
                    TransactionType = TransactionType.SUBSCRIPTION,
                    PaymentMethodId = request.PaymentMethodId,
                    Status = PaymentStatus.PENDING,
                    CreatedAt = DateTime.UtcNow,
                };

                var createdPayment = await _paymentRepository.CreateAsync(paymentEntity);

                LogInfo(
                    "Payment for subscription created successfully with ID: {PaymentId}",
                    null,
                    createdPayment.Id
                );
                return _mapper.Map<PaymentResponse>(createdPayment);
            },
            "CreateSubscriptionPayment"
        );
    }

    /// <summary>
    /// Update payment status - Business operation MUST use ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentResponse> UpdateStatusAsync(UpdatePaymentStatusRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Starting to update payment status: {PaymentId} -> {Status}",
                    null,
                    request.Id,
                    request.Status
                );

                // Validation
                ValidateRequired(request, nameof(request));
                ValidateGuid(request.Id, nameof(request.Id));

                var payment = await _paymentRepository.GetByIdAsync(request.Id);
                if (payment == null)
                {
                    LogError(
                        new ArgumentException($"Payment with ID {request.Id} does not exist"),
                        "Payment does not exist: {PaymentId}",
                        null,
                        request.Id
                    );
                    throw new ArgumentException($"Payment with ID {request.Id} does not exist");
                }

                // Business logic
                payment.Status = request.Status;

                // Update amount if provided (for supplementary payments)
                if (request.Amount.HasValue)
                {
                    LogInfo(
                        "Updating payment amount from {OldAmount} to {NewAmount} for PaymentId: {PaymentId}",
                        null,
                        payment.Amount,
                        request.Amount.Value,
                        request.Id
                    );
                    payment.Amount = request.Amount.Value;
                }

                var updatedPayment = await _paymentRepository.UpdateAsync(payment);

                LogInfo("Payment status updated successfully: {PaymentId}", null, request.Id);
                return _mapper.Map<PaymentResponse>(updatedPayment);
            },
            "UpdatePaymentStatus"
        );
    }

    /// <summary>
    /// Update payment intent ID (for refund tracking)
    /// </summary>
    public async Task UpdatePaymentIntentAsync(Guid paymentId, string paymentIntentId)
    {
        await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Updating PaymentIntentId for PaymentId: {PaymentId}", null, paymentId);

                ValidateGuid(paymentId, nameof(paymentId));
                ValidateRequired(paymentIntentId, nameof(paymentIntentId));

                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    LogError(
                        new ArgumentException($"Payment with ID {paymentId} does not exist"),
                        "Payment does not exist: {PaymentId}",
                        null,
                        paymentId
                    );
                    throw new ArgumentException($"Payment with ID {paymentId} does not exist");
                }

                payment.PaymentIntentId = paymentIntentId;
                await _paymentRepository.UpdateAsync(payment);

                LogInfo(
                    "PaymentIntentId updated successfully for PaymentId: {PaymentId}",
                    null,
                    paymentId
                );
                return Task.CompletedTask;
            },
            "UpdatePaymentIntent"
        );
    }

    /// <summary>
    /// Delete payment - Business operation MUST use ExecuteWithErrorHandling
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Starting to delete payment: {PaymentId}", null, id);

                ValidateGuid(id, nameof(id));

                var exists = await _paymentRepository.ExistsAsync(id);
                if (!exists)
                {
                    LogWarning("Payment does not exist to delete: {PaymentId}", null, id);
                    return false;
                }

                // Business logic
                var result = await _paymentRepository.DeleteAsync(id);

                if (result)
                {
                    LogInfo("Payment deleted successfully: {PaymentId}", null, id);
                }
                else
                {
                    LogError(
                        new InvalidOperationException("Failed to delete payment"),
                        "Unable to delete payment: {PaymentId}",
                        null,
                        id
                    );
                }

                return result;
            },
            "DeletePayment"
        );
    }

    /// <summary>
    /// Get payment statistics - Business operation MUST use ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentStatisticsResponse> GetPaymentStatisticsAsync(
        GetPaymentStatisticsRequest request
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                // Use computed dates with default values
                var fromDate = request.GetFromDate();
                var toDate = request.GetToDate();

                LogInfo(
                    "Starting to get payment statistics - Period: {Period}, FromDate: {FromDate}, ToDate: {ToDate}",
                    null,
                    request.Period,
                    fromDate,
                    toDate
                );

                // Validation
                ValidateRequired(request, nameof(request));

                // Get raw payment data
                var payments = await _paymentRepository.GetPaymentStatisticsAsync(request);
                var paymentsList = payments.ToList();

                // Create updated request with computed dates for time series generation
                var computedRequest = new GetPaymentStatisticsRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Period = request.Period,
                    HospitalId = request.HospitalId,
                    PatientId = request.PatientId,
                    TransactionType = request.TransactionType,
                    Status = request.Status,
                };

                // Calculate statistics
                var response = new PaymentStatisticsResponse
                {
                    TimeSeries = GenerateTimeSeries(paymentsList, computedRequest),
                    Summary = GenerateSummaryStatistics(paymentsList, computedRequest),
                    PaymentMethodBreakdown = GeneratePaymentMethodStatistics(paymentsList),
                    StatusBreakdown = GenerateStatusStatistics(paymentsList),
                    TransactionTypeBreakdown = GenerateTransactionTypeStatistics(paymentsList),
                    Period = request.Period.ToString(),
                    DateRange = $"{fromDate:yyyy-MM-dd} - {toDate:yyyy-MM-dd}",
                };

                LogInfo(
                    "Payment statistics created successfully with {TimeSeriesCount} periods",
                    null,
                    response.TimeSeries.Count
                );
                return response;
            },
            "GetPaymentStatistics"
        );
    }

    private List<PaymentTimeSeriesData> GenerateTimeSeries(
        List<PaymentEntity> payments,
        GetPaymentStatisticsRequest request
    )
    {
        var result = new List<PaymentTimeSeriesData>();
        var current = request.GetFromDate().Date;
        var endDate = request.GetToDate().Date;

        while (current <= endDate)
        {
            var (periodStart, periodEnd, timeLabel) = StatisticsPeriodHelper.GetPeriodBounds(
                current,
                request.Period
            );

            var periodPayments = payments
                .Where(p => p.CreatedAt.Date >= periodStart && p.CreatedAt.Date <= periodEnd)
                .ToList();

            var completedPayments = periodPayments
                .Where(p => p.Status == PaymentStatus.COMPLETED)
                .ToList();

            result.Add(
                new PaymentTimeSeriesData
                {
                    TimeLabel = timeLabel,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    TotalCount = periodPayments.Count,
                    TotalAmount = periodPayments.Sum(p => p.Amount),
                    CompletedCount = completedPayments.Count,
                    CompletedAmount = completedPayments.Sum(p => p.Amount),
                    PendingCount = periodPayments.Count(p => p.Status == PaymentStatus.PENDING),
                    FailedCount = periodPayments.Count(p => p.Status == PaymentStatus.FAILED),
                    RefundedCount = periodPayments.Count(p => p.Status == PaymentStatus.REFUNDED),
                    AverageAmount = periodPayments.Any()
                        ? periodPayments.Average(p => p.Amount)
                        : 0,
                }
            );

            current = StatisticsPeriodHelper.GetNextPeriod(current, request.Period);
        }

        return result;
    }

    private PaymentSummaryStatistics GenerateSummaryStatistics(
        List<PaymentEntity> payments,
        GetPaymentStatisticsRequest request
    )
    {
        var completedPayments = payments.Where(p => p.Status == PaymentStatus.COMPLETED).ToList();
        var totalDays = (request.GetToDate() - request.GetFromDate()).TotalDays + 1;

        return new PaymentSummaryStatistics
        {
            TotalPayments = payments.Count,
            TotalAmount = payments.Sum(p => p.Amount),
            TotalCompletedAmount = completedPayments.Sum(p => p.Amount),
            SuccessRate = payments.Any()
                ? (decimal)completedPayments.Count / payments.Count * 100
                : 0,
            AveragePaymentAmount = payments.Any() ? payments.Average(p => p.Amount) : 0,
            MaxPaymentAmount = payments.Any() ? payments.Max(p => p.Amount) : 0,
            MinPaymentAmount = payments.Any() ? payments.Min(p => p.Amount) : 0,
            AveragePaymentsPerDay = totalDays > 0 ? payments.Count / (decimal)totalDays : 0,
            GrowthRate =
                0 // Growth rate calculation not implemented - future enhancement
            ,
        };
    }

    private List<PaymentMethodStatistics> GeneratePaymentMethodStatistics(
        List<PaymentEntity> payments
    )
    {
        var total = payments.Count;

        return payments
            .GroupBy(p => new { p.PaymentMethodId, p.PaymentMethod.Name })
            .Select(g => new PaymentMethodStatistics
            {
                PaymentMethodId = g.Key.PaymentMethodId,
                PaymentMethodName = g.Key.Name,
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount),
                Percentage = total > 0 ? (decimal)g.Count() / total * 100 : 0,
                AverageAmount = g.Average(p => p.Amount),
            })
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    private List<PaymentStatusStatistics> GenerateStatusStatistics(List<PaymentEntity> payments)
    {
        var total = payments.Count;

        return payments
            .GroupBy(p => p.Status)
            .Select(g => new PaymentStatusStatistics
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount),
                Percentage = total > 0 ? (decimal)g.Count() / total * 100 : 0,
            })
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    private List<TransactionTypeStatistics> GenerateTransactionTypeStatistics(
        List<PaymentEntity> payments
    )
    {
        var total = payments.Count;

        return payments
            .GroupBy(p => p.TransactionType)
            .Select(g => new TransactionTypeStatistics
            {
                TransactionType = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount),
                Percentage = total > 0 ? (decimal)g.Count() / total * 100 : 0,
                AverageAmount = g.Average(p => p.Amount),
            })
            .OrderByDescending(x => x.Count)
            .ToList();
    }
}
