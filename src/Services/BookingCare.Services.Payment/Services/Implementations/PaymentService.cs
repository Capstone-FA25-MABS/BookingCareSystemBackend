using AutoMapper;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;

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
    /// Lấy danh sách payments theo clinic ID với phân trang - Thao tác đọc đơn giản
    /// </summary>
    public async Task<PagedResult<PaymentResponse>> GetPagedByClinicIdAsync(Guid clinicId, GetPaymentsPagedRequest request)
    {
        var pagedResult = await _paymentRepository.GetPagedByClinicIdAsync(clinicId, request);

        return new PagedResult<PaymentResponse>
        {
            Items = _mapper.Map<List<PaymentResponse>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };
    }

    /// <summary>
    /// Lấy danh sách payments theo patient ID với phân trang - Thao tác đọc đơn giản
    /// </summary>
    public async Task<PagedResult<PaymentResponse>> GetPagedByPatientIdAsync(Guid patientId, GetPaymentsPagedRequest request)
    {
        var pagedResult = await _paymentRepository.GetPagedByPatientIdAsync(patientId, request);

        return new PagedResult<PaymentResponse>
        {
            Items = _mapper.Map<List<PaymentResponse>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };
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

    /// <summary>
    /// Lấy thống kê payments - Thao tác business BẮT BUỘC sử dụng ExecuteWithErrorHandling
    /// </summary>
    public async Task<PaymentStatisticsResponse> GetPaymentStatisticsAsync(GetPaymentStatisticsRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Sử dụng computed dates với default values
            var fromDate = request.GetFromDate();
            var toDate = request.GetToDate();

            LogInfo("Bắt đầu lấy thống kê payments - Period: {Period}, FromDate: {FromDate}, ToDate: {ToDate}",
                null, request.Period, fromDate, toDate);

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
                ClinicId = request.ClinicId,
                PatientId = request.PatientId,
                TransactionType = request.TransactionType,
                Status = request.Status
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
                DateRange = $"{fromDate:yyyy-MM-dd} - {toDate:yyyy-MM-dd}"
            };

            LogInfo("Thống kê payments được tạo thành công với {TimeSeriesCount} periods", null, response.TimeSeries.Count);
            return response;
        }, "GetPaymentStatistics");
    }

    private List<PaymentTimeSeriesData> GenerateTimeSeries(List<PaymentEntity> payments, GetPaymentStatisticsRequest request)
    {
        var result = new List<PaymentTimeSeriesData>();
        var current = request.GetFromDate().Date;
        var endDate = request.GetToDate().Date;

        while (current <= endDate)
        {
            var (periodStart, periodEnd, timeLabel) = GetPeriodBounds(current, request.Period);

            var periodPayments = payments.Where(p =>
                p.CreatedAt.Date >= periodStart && p.CreatedAt.Date <= periodEnd).ToList();

            var completedPayments = periodPayments.Where(p => p.Status == PaymentStatus.COMPLETED).ToList();

            result.Add(new PaymentTimeSeriesData
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
                AverageAmount = periodPayments.Any() ? periodPayments.Average(p => p.Amount) : 0
            });

            current = GetNextPeriod(current, request.Period);
        }

        return result;
    }

    private PaymentSummaryStatistics GenerateSummaryStatistics(List<PaymentEntity> payments, GetPaymentStatisticsRequest request)
    {
        var completedPayments = payments.Where(p => p.Status == PaymentStatus.COMPLETED).ToList();
        var totalDays = (request.GetToDate() - request.GetFromDate()).TotalDays + 1;

        return new PaymentSummaryStatistics
        {
            TotalPayments = payments.Count,
            TotalAmount = payments.Sum(p => p.Amount),
            TotalCompletedAmount = completedPayments.Sum(p => p.Amount),
            SuccessRate = payments.Any() ? (decimal)completedPayments.Count / payments.Count * 100 : 0,
            AveragePaymentAmount = payments.Any() ? payments.Average(p => p.Amount) : 0,
            MaxPaymentAmount = payments.Any() ? payments.Max(p => p.Amount) : 0,
            MinPaymentAmount = payments.Any() ? payments.Min(p => p.Amount) : 0,
            AveragePaymentsPerDay = totalDays > 0 ? payments.Count / (decimal)totalDays : 0,
            GrowthRate = 0 // TODO: Calculate growth rate compared to previous period
        };
    }

    private (DateTime start, DateTime end, string label) GetPeriodBounds(DateTime date, StatisticsPeriod period)
    {
        return period switch
        {
            StatisticsPeriod.Daily => (date, date, date.ToString("yyyy-MM-dd")),
            StatisticsPeriod.Weekly => GetWeeklyPeriod(date),
            StatisticsPeriod.Monthly => GetMonthlyPeriod(date),
            StatisticsPeriod.Quarterly => GetQuarterlyPeriod(date),
            StatisticsPeriod.Yearly => GetYearlyPeriod(date),
            _ => (date, date, date.ToString("yyyy-MM-dd"))
        };
    }

    private (DateTime start, DateTime end, string label) GetWeeklyPeriod(DateTime date)
    {
        var startOfWeek = date.AddDays(-(int)date.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);
        return (startOfWeek, endOfWeek, $"W{GetWeekOfYear(startOfWeek)}-{startOfWeek.Year}");
    }

    private (DateTime start, DateTime end, string label) GetMonthlyPeriod(DateTime date)
    {
        var startOfMonth = new DateTime(date.Year, date.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        return (startOfMonth, endOfMonth, date.ToString("yyyy-MM"));
    }

    private (DateTime start, DateTime end, string label) GetQuarterlyPeriod(DateTime date)
    {
        var quarter = (date.Month - 1) / 3 + 1;
        var startOfQuarter = new DateTime(date.Year, (quarter - 1) * 3 + 1, 1);
        var endOfQuarter = startOfQuarter.AddMonths(3).AddDays(-1);
        return (startOfQuarter, endOfQuarter, $"{date.Year}-Q{quarter}");
    }

    private (DateTime start, DateTime end, string label) GetYearlyPeriod(DateTime date)
    {
        var startOfYear = new DateTime(date.Year, 1, 1);
        var endOfYear = new DateTime(date.Year, 12, 31);
        return (startOfYear, endOfYear, date.Year.ToString());
    }

    private DateTime GetNextPeriod(DateTime current, StatisticsPeriod period)
    {
        return period switch
        {
            StatisticsPeriod.Daily => current.AddDays(1),
            StatisticsPeriod.Weekly => current.AddDays(7),
            StatisticsPeriod.Monthly => current.AddMonths(1),
            StatisticsPeriod.Quarterly => current.AddMonths(3),
            StatisticsPeriod.Yearly => current.AddYears(1),
            _ => current.AddDays(1)
        };
    }

    private int GetWeekOfYear(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }

    private List<PaymentMethodStatistics> GeneratePaymentMethodStatistics(List<PaymentEntity> payments)
    {
        var total = payments.Count;
        var totalAmount = payments.Sum(p => p.Amount);

        return payments
            .GroupBy(p => new { p.PaymentMethodId, p.PaymentMethod.Name })
            .Select(g => new PaymentMethodStatistics
            {
                PaymentMethodId = g.Key.PaymentMethodId,
                PaymentMethodName = g.Key.Name,
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount),
                Percentage = total > 0 ? (decimal)g.Count() / total * 100 : 0,
                AverageAmount = g.Average(p => p.Amount)
            })
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    private List<PaymentStatusStatistics> GenerateStatusStatistics(List<PaymentEntity> payments)
    {
        var total = payments.Count;
        var totalAmount = payments.Sum(p => p.Amount);

        return payments
            .GroupBy(p => p.Status)
            .Select(g => new PaymentStatusStatistics
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount),
                Percentage = total > 0 ? (decimal)g.Count() / total * 100 : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    private List<TransactionTypeStatistics> GenerateTransactionTypeStatistics(List<PaymentEntity> payments)
    {
        var total = payments.Count;
        var totalAmount = payments.Sum(p => p.Amount);

        return payments
            .GroupBy(p => p.TransactionType)
            .Select(g => new TransactionTypeStatistics
            {
                TransactionType = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount),
                Percentage = total > 0 ? (decimal)g.Count() / total * 100 : 0,
                AverageAmount = g.Average(p => p.Amount)
            })
            .OrderByDescending(x => x.Count)
            .ToList();
    }
}