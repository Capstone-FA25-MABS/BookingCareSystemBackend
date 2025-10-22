using AutoMapper;
using BookingCare.Services.Appointment.Exceptions;
using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Services.Appointment.Models.Entities;
using BookingBasicInfo = BookingCare.Services.Appointment.Models.Internal.DoctorBasicInfo;
using BookingCare.Services.Appointment.Models.Internal;
using BookingCare.Services.Appointment.Repositories;
using BookingCare.Services.Appointment.Enums;
using BookingCare.Services.Appointment.Helpers;
using BookingCare.Services.Appointment.Configuration;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using BookingCare.Services.User.Protos;
using BookingCare.Services.Payment.Protos;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Extensions;
using GrpcCore = Grpc.Core; // Use alias to avoid namespace conflict
using Microsoft.Extensions.Options;
using BookingCare.Shared.Cache.Constants;
using RedisClient = StackExchange.Redis;

namespace BookingCare.Services.Appointment.Services;

/// <summary>
/// Service implementation for Appointment service operations
/// </summary>
public class AppointmentService : BaseService, IAppointmentService
{
    private const string DateFormat = "yyyy-MM-dd";
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly GrpcClientWrapper _grpcClients;
    private readonly RedisClient.IConnectionMultiplexer _redisConnection;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly FrontendConfiguration _frontendConfig;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IMapper mapper,
        IEventBus eventBus,
        GrpcClientWrapper grpcClients,
        RedisClient.IConnectionMultiplexer redisConnection,
        IHttpContextAccessor httpContextAccessor,
        IOptions<FrontendConfiguration> frontendConfig,
        ILogger<AppointmentService> logger) : base(logger)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
        _eventBus = eventBus;
        _grpcClients = grpcClients;
        _redisConnection = redisConnection;
        _httpContextAccessor = httpContextAccessor;
        _frontendConfig = frontendConfig.Value;
    }

    #region Helper Methods

    /// <summary>
    /// Get payment information for an appointment via gRPC
    /// </summary>
    private async Task<decimal?> GetPaymentAmountAsync(Guid appointmentId)
    {
        try
        {
            LogInfo("Fetching payment info for appointment {AppointmentId}", null, appointmentId);

            var request = new GetPaymentByAppointmentIdRequest
            {
                AppointmentId = appointmentId.ToString()
            };

            var response = await _grpcClients.PaymentClient.GetPaymentByAppointmentIdAsync(request);

            if (response.Success && response.Payment != null)
            {
                LogInfo("Successfully retrieved payment amount {Amount} for appointment {AppointmentId}",
                    null, response.Payment.Amount, appointmentId);
                return (decimal)response.Payment.Amount;
            }

            LogWarning("No payment found for appointment {AppointmentId}: {Message}",
                null, appointmentId, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error fetching payment info for appointment {AppointmentId}", null, appointmentId);
            return null;
        }
    }

    /// <summary>
    /// Get appointment payment amount (alias for GetPaymentAmountAsync for clarity in Option 3 flow)
    /// </summary>
    private async Task<decimal> GetAppointmentPaymentAmountAsync(Guid appointmentId)
    {
        var amount = await GetPaymentAmountAsync(appointmentId);
        return amount ?? 0m; // Return 0 if no payment found
    }

    /// <summary>
    /// Get doctor price from Doctor Service via gRPC
    /// </summary>
    private async Task<decimal> GetDoctorPriceAsync(Guid priceId)
    {
        try
        {
            LogInfo("Fetching doctor price {PriceId}", null, priceId);

            var request = new GetDoctorPriceRequest
            {
                PriceId = priceId.ToString()
            };

            var response = await _grpcClients.DoctorClient.GetDoctorPriceAsync(request);

            if (response == null)
            {
                throw new AppointmentException($"Doctor price not found for ID {priceId}");
            }

            LogInfo("Retrieved doctor price {Amount} VND for price ID {PriceId}",
                null, response.Amount, priceId);

            return (decimal)response.Amount;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error fetching doctor price {PriceId}", null, priceId);
            throw new AppointmentException($"Failed to get doctor price: {ex.Message}");
        }
    }

    /// <summary>
    /// Update appointment with new doctor information
    /// </summary>
    private async Task UpdateAppointmentWithNewDoctorAsync(AppointmentEntity appointment, ChooseNewDoctorRequest request)
    {
        appointment.DoctorId = request.NewDoctorId;
        appointment.AppointmentDate = request.NewAppointmentDate;
        appointment.AppointmentTimeId = request.NewAppointmentTimeId;
        appointment.Status = AppointmentStatus.CONFIRMED; // Reactivate appointment
        appointment.AssignedDoctorId = null; // Clear soft reservation
        appointment.SoftReservedUntil = null;
        // Clear pending fields after applying
        appointment.PendingNewDoctorId = null;
        appointment.PendingNewAppointmentDate = null;
        appointment.PendingNewAppointmentTimeId = null;
        appointment.IsRescheduled = true;
        appointment.RescheduleToken = null; // Clear token after use
        appointment.RescheduleTokenExpiry = null;

        await _appointmentRepository.UpdateAppointmentAsync(appointment);
        LogInfo("Updated appointment {AppointmentId} with new doctor {DoctorId}",
            null, appointment.Id, request.NewDoctorId);
    }

    /// <summary>
    /// Confirm staff-assigned doctor (Option 2 flow)
    /// Similar to UpdateAppointmentWithNewDoctorAsync but also clears soft reservation fields
    /// </summary>
    private async Task ConfirmNewDoctorAsync(AppointmentEntity appointment)
    {
        appointment.DoctorId = appointment.AssignedDoctorId;
        appointment.AssignedDoctorId = null; // Clear soft reservation
        appointment.SoftReservedUntil = null;
        // Clear pending fields after applying
        appointment.PendingNewDoctorId = null;
        appointment.PendingNewAppointmentDate = null;
        appointment.PendingNewAppointmentTimeId = null;
        appointment.Status = AppointmentStatus.CONFIRMED;
        appointment.IsRescheduled = true;
        appointment.RescheduleToken = null; // Clear token after use
        appointment.RescheduleTokenExpiry = null;

        await _appointmentRepository.UpdateAppointmentAsync(appointment);
    }

    #endregion

    #region Appointment Operations

    public async Task<Guid> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating appointment for patient {PatientId} on {AppointmentDate}",
                null, request.PatientId, request.AppointmentDate);

            // Validate the appointment
            await ValidateAppointmentAsync(request);

            // Check for conflicts
            var hasConflict = await _appointmentRepository.HasConflictingAppointmentAsync(
                request.PatientId, request.AppointmentDate, request.AppointmentTimeId);
            if (hasConflict)
            {
                throw new AppointmentConflictException(request.PatientId, request.AppointmentDate);
            }

            // Check doctor availability if doctor is specified
            if (request.DoctorId.HasValue)
            {
                var isDoctorAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
                    request.DoctorId.Value, request.AppointmentDate, request.AppointmentTimeId);
                if (!isDoctorAvailable)
                {
                    throw new DoctorNotAvailableException(request.DoctorId.Value, request.AppointmentDate);
                }
                request.Status = AppointmentStatus.CONFIRMED;
            }
            else
            {
                request.Status = AppointmentStatus.PENDING;
            }

            var appointmentEntity = _mapper.Map<AppointmentEntity>(request);
            await _appointmentRepository.CreateAppointmentAsync(appointmentEntity);

            // Invalidate available slots cache after successful appointment creation
            if (request.DoctorId.HasValue)
            {
                await InvalidateAvailableSlotsCacheAsync(
                    request.DoctorId.Value,
                    request.AppointmentDate,
                    request.ServiceId);
            }

            LogInfo("Successfully created appointment {AppointmentId}", null, appointmentEntity.Id);
            return appointmentEntity.Id;
        }, "CreateAppointment");
    }

    public async Task<AppointmentResponse?> GetAppointmentByIdForPatientAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting appointment by ID for patient: {AppointmentId}", null, id);

            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                LogWarning("Appointment not found: {AppointmentId}", null, id);
                return null;
            }

            var response = _mapper.Map<AppointmentResponse>(appointment);

            // Get payment information via gRPC
            response.ConsultationFees = await GetPaymentAmountAsync(appointment.Id);

            // Enrich appointment with external data for patient view
            await EnrichAppointmentsWithExternalDataAsync(
                new List<AppointmentResponse> { response },
                new List<AppointmentEntity> { appointment },
                Role.PATIENT
            );

            LogInfo("Appointment found and enriched: {AppointmentId}", null, appointment.Id);
            return response;
        }, "GetAppointmentByIdForPatient");
    }

    public async Task<AppointmentListResponse> GetAppointmentsByPatientAsync(AppointmentQueryRequest query)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting appointments with query: Page={PageNumber}, Size={PageSize}, IncludeStatusCounts={IncludeStatusCounts}",
                null, query.PageNumber, query.PageSize, query.IncludeStatusCounts);

            var (appointments, totalCount) = await _appointmentRepository.GetAppointmentsAsync(query, Role.PATIENT);
            var responses = _mapper.Map<List<AppointmentResponse>>(appointments);

            // Enrich appointments with additional information via gRPC calls based on user role
            await EnrichAppointmentsWithExternalDataAsync(responses, appointments, Role.PATIENT);

            var response = new AppointmentListResponse
            {
                Appointments = responses,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };

            // Include status counts if requested
            if (query.IncludeStatusCounts && query.PatientId.HasValue)
            {
                response.StatusCounts = await GetStatusCountsAsync(query.PatientId.Value, Role.PATIENT, null);
                LogInfo("Included status counts for patient {PatientId}", null, query.PatientId.Value);
            }

            LogInfo("Retrieved {Count} appointments out of {TotalCount}",
                null, appointments.Count, totalCount);
            return response;
        }, "GetAppointmentsByPatient");
    }

    public async Task<AppointmentListResponse> GetAppointmentsForManagementAsync(AppointmentQueryRequest query)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting management appointments with query: Page={PageNumber}, Size={PageSize}",
                null, query.PageNumber, query.PageSize);

            // Get user roles from JWT token
            var userRoles = JwtHelper.GetUserRoles(_httpContextAccessor.HttpContext!);
            var managementRole = GetManagementRole(userRoles);

            LogInfo("User has roles: {Roles}, Management role determined: {ManagementRole}",
                null, string.Join(",", userRoles), managementRole);

            var (appointments, totalCount) = await _appointmentRepository.GetAppointmentsAsync(query, managementRole);
            var responses = _mapper.Map<List<AppointmentResponse>>(appointments);

            // Enrich appointments with additional information via gRPC calls based on user role
            await EnrichAppointmentsWithExternalDataAsync(responses, appointments, managementRole);

            var response = new AppointmentListResponse
            {
                Appointments = responses,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };

            // Include status counts if requested - use role-based logic
            if (query.IncludeStatusCounts)
            {
                response.StatusCounts = await GetRoleBasedStatusCountsAsync(query, managementRole);
                LogInfo("Included status counts for management role {Role}", null, managementRole);
            }

            LogInfo("Retrieved {Count} appointments out of {TotalCount} for management role {Role}",
                null, appointments.Count, totalCount, managementRole);
            return response;
        }, "GetAppointmentsForManagement");
    }

    /// <summary>
    /// Enrich appointments with external data via gRPC calls based on user role
    /// Performance optimized with batch requests to avoid N+1 problem
    /// </summary>
    private async Task EnrichAppointmentsWithExternalDataAsync(List<AppointmentResponse> responses, List<AppointmentEntity> entities, Role role)
    {
        if (!responses.Any()) return;

        try
        {
            switch (role)
            {
                case Role.PATIENT:
                    await EnrichForPatientRoleAsync(responses, entities);
                    break;

                case Role.DOCTOR:
                    await EnrichForDoctorRoleAsync(responses, entities);
                    break;

                case Role.STAFF:
                case Role.ADMIN:
                    await EnrichForStaffRoleAsync(responses, entities);
                    break;
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to enrich appointments with external data: {Error}", null, ex.Message);
            // Don't throw - continue with un-enriched data
        }
    }

    /// <summary>
    /// Enrich appointments for Patient role - shows doctor, service, and hospital info
    /// </summary>
    private async Task EnrichForPatientRoleAsync(List<AppointmentResponse> responses, List<AppointmentEntity> entities)
    {
        await FetchAndMapDoctorInfoAsync(responses, entities, "appointments enrichment");

        // TODO: Batch fetch Service info when available
        var serviceIds = entities
            .Where(e => e.ServiceId.HasValue)
            .Select(e => e.ServiceId!.Value)
            .Distinct()
            .ToList();

        if (serviceIds.Any())
        {
            LogInfo("TODO: Batch fetch {Count} services for appointments enrichment", null, serviceIds.Count);
        }

        // Batch fetch Hospital info to avoid N+1 problem
        var hospitalIds = entities
            .Where(e => e.HospitalId.HasValue)
            .Select(e => e.HospitalId!.Value)
            .Distinct()
            .ToList();

        if (hospitalIds.Any())
        {
            try
            {
                var hospitalRequest = new GetHospitalsBasicInfoRequest();
                hospitalRequest.Ids.AddRange(hospitalIds.Select(id => id.ToString()));

                var hospitalsResponse = await _grpcClients.HospitalClient.GetHospitalsBasicInfoAsync(hospitalRequest);
                var hospitalDict = hospitalsResponse.Hospitals.ToDictionary(
                    h => Guid.Parse(h.Id),
                    h => h
                );

                LogInfo("Batch fetched {Count} hospitals for appointments enrichment", null, hospitalDict.Count);

                // Map hospital info to appointments
                for (int i = 0; i < responses.Count; i++)
                {
                    var entity = entities[i];
                    if (entity.HospitalId.HasValue && hospitalDict.TryGetValue(entity.HospitalId.Value, out var hospitalInfo))
                    {
                        responses[i].HospitalInfo = new HospitalInfo
                        {
                            Id = Guid.Parse(hospitalInfo.Id),
                            Name = hospitalInfo.Name,
                            Address = hospitalInfo.Address,
                            Phone = hospitalInfo.Phone,
                            Email = hospitalInfo.Email,
                            AvatarUrl = hospitalInfo.AvatarUrl
                        };
                    }
                }
            }
            catch (GrpcCore.RpcException rpcEx)
            {
                LogWarning("gRPC error batch fetching hospitals: {Error}", null, rpcEx.Status.Detail);
            }
        }
    }

    /// <summary>
    /// Enrich appointments for Doctor role - shows patient info
    /// </summary>
    private async Task EnrichForDoctorRoleAsync(List<AppointmentResponse> responses, List<AppointmentEntity> entities)
    {
        await FetchAndMapPatientInfoAsync(responses, entities, "doctor view");
    }

    /// <summary>
    /// Enrich appointments for Staff/Admin role - shows both patient and doctor info
    /// </summary>
    private async Task EnrichForStaffRoleAsync(List<AppointmentResponse> responses, List<AppointmentEntity> entities)
    {
        await FetchAndMapPatientInfoAsync(responses, entities, "staff view");
        await FetchAndMapDoctorInfoAsync(responses, entities, "staff view");
    }

    #endregion

    #region Cancel Operations

    public async Task<RescheduleResponse?> CancelAppointmentAsync(CancelAppointmentRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Cancelling appointment {AppointmentId}", null, request.AppointmentId);

            // Get and validate appointment
            var appointment = await GetAndValidateAppointmentForCancellationAsync(request.AppointmentId);

            // Calculate cancellation details
            var cancellationDetails = CalculateCancellationDetails(request, appointment);

            // Generate reschedule token if staff cancellation and reschedule options enabled
            RescheduleResponse? rescheduleResponse = null;
            if (cancellationDetails.IsStaffCancellation && request.EnableRescheduleOptions)
            {
                // Pass selected options to generate only relevant URLs
                rescheduleResponse = await GenerateRescheduleResponseAsync(appointment, request.RescheduleOptions);

                // Store token in appointment entity
                appointment.RescheduleToken = rescheduleResponse.RescheduleToken;
                appointment.RescheduleTokenExpiry = rescheduleResponse.TokenExpiry;
            }

            // Cancel appointment in repository
            var cancelled = await _appointmentRepository.CancelAppointmentAsync(
                appointment,
                request.CancellationReason,
                cancellationDetails.CancelledBy);

            if (!cancelled)
            {
                throw new AppointmentException("Failed to cancel appointment");
            }

            // Publish appropriate event based on refund percentage and reschedule options
            await PublishCancellationEventAsync(appointment, request, cancellationDetails, rescheduleResponse);

            return rescheduleResponse;
        }, "CancelAppointment");
    }

    /// <summary>
    /// Get appointment and validate it can be cancelled
    /// </summary>
    private async Task<AppointmentEntity> GetAndValidateAppointmentForCancellationAsync(Guid appointmentId)
    {
        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
        if (appointment == null)
        {
            throw new AppointmentNotFoundException(appointmentId);
        }

        // Validate current status
        if (appointment.Status != AppointmentStatus.PENDING &&
            appointment.Status != AppointmentStatus.CONFIRMED)
        {
            throw new AppointmentException(
                $"Cannot cancel appointment with status {appointment.Status}. Only PENDING or CONFIRMED appointments can be cancelled.");
        }

        // Validate appointment is in the future
        if (!RefundPolicyHelper.IsCancellationAllowed(appointment.AppointmentDate, DateTime.UtcNow))
        {
            throw new AppointmentException(
                $"Cannot cancel appointment that has already passed. " +
                $"Appointment was scheduled for {appointment.AppointmentDate:yyyy-MM-dd HH:mm} UTC.");
        }

        return appointment;
    }

    /// <summary>
    /// Calculate cancellation details including refund percentage
    /// </summary>
    private CancellationDetails CalculateCancellationDetails(CancelAppointmentRequest request, AppointmentEntity appointment)
    {
        var now = DateTime.UtcNow;
        var isStaffCancellation = request.CancelledByStaffId.HasValue;
        var cancelledBy = isStaffCancellation ? "Staff" : "Patient";
        var refundPercentage = RefundPolicyHelper.CalculateRefundPercentage(appointment.AppointmentDate, now, isStaffCancellation);
        var hoursUntilAppointment = (appointment.AppointmentDate - now).TotalHours;

        LogInfo("Appointment {AppointmentId} cancellation: {Hours} hours before appointment, {Refund}% refund, IsStaffCancellation: {IsStaff}",
            null, appointment.Id, hoursUntilAppointment, refundPercentage, isStaffCancellation);

        return new CancellationDetails
        {
            IsStaffCancellation = isStaffCancellation,
            CancelledBy = cancelledBy,
            RefundPercentage = refundPercentage
        };
    }

    /// <summary>
    /// Generate reschedule response with token and deep links for all 4 options
    /// Conditionally generates URLs based on appointment type (doctor-based vs service-based)
    /// </summary>
    private async Task<RescheduleResponse> GenerateRescheduleResponseAsync(
        AppointmentEntity appointment,
        RescheduleOptionsSelection? selectedOptions = null)
    {
        var token = Guid.NewGuid().ToString("N");
        var expiry = CalculateRescheduleTokenExpiry(appointment.AppointmentDate);
        var urls = GenerateRescheduleUrls(appointment, token, selectedOptions);

        await Task.CompletedTask;

        return new RescheduleResponse
        {
            AppointmentId = appointment.Id,
            RescheduleToken = token,
            TokenExpiry = expiry,
            Message = appointment.DoctorId.HasValue
                ? "Appointment cancelled. You can reschedule or request a refund."
                : "Appointment cancelled. You can book a new service or request a refund.",
            SameDoctorRescheduleUrl = urls.SameDoctorUrl,
            ConfirmNewDoctorUrl = urls.ConfirmDoctorUrl,
            ChooseNewDoctorUrl = urls.ChooseNewDoctorUrl,
            RefundRequestUrl = urls.RefundUrl
        };
    }

    /// <summary>
    /// Calculate reschedule token expiry based on appointment date
    /// </summary>
    private DateTime CalculateRescheduleTokenExpiry(DateTime appointmentDate)
    {
        var currentTime = DateTime.UtcNow;

        if (appointmentDate > currentTime)
        {
            var appointmentDateOnly = appointmentDate.Date;
            var endOfDayBeforeAppointment = appointmentDateOnly.AddSeconds(-1);
            var maxExpiry = currentTime.AddDays(7);
            var expiry = endOfDayBeforeAppointment < maxExpiry ? endOfDayBeforeAppointment : maxExpiry;

            LogInfo("Reschedule token expiry calculated for future appointment: {AppointmentDate} -> {Expiry} (end of day before appointment)",
                null, appointmentDate, expiry);
            return expiry;
        }

        var endOfToday = currentTime.Date.AddDays(1).AddSeconds(-1);
        LogInfo("Reschedule token expiry calculated for past/today appointment: {AppointmentDate} -> {Expiry} (end of today)",
            null, appointmentDate, endOfToday);
        return endOfToday;
    }

    /// <summary>
    /// Generate reschedule URLs based on selected options
    /// </summary>
    private (string? SameDoctorUrl, string? ConfirmDoctorUrl, string? ChooseNewDoctorUrl, string? RefundUrl) GenerateRescheduleUrls(
        AppointmentEntity appointment, string token, RescheduleOptionsSelection? selectedOptions)
    {
        var frontendBaseUrl = _frontendConfig.BaseUrl;
        var generateAll = selectedOptions == null;

        var sameDoctorUrl = ShouldGenerateSameDoctorUrl(appointment, generateAll, selectedOptions)
            ? $"{frontendBaseUrl}/booking/reschedule/{appointment.Id}?token={token}"
            : null;

        var confirmDoctorUrl = ShouldGenerateConfirmDoctorUrl(appointment, generateAll, selectedOptions)
            ? $"{frontendBaseUrl}/booking/confirm-doctor/{appointment.Id}?token={token}&newDoctorId={appointment.AssignedDoctorId}"
            : null;

        var chooseNewDoctorUrl = (generateAll || selectedOptions!.EnableDoctorSelection)
            ? $"{frontendBaseUrl}/doctors?hospitalId={appointment.HospitalId}&specialtyId={appointment.SpecialtyId}&rescheduleFor={appointment.Id}&token={token}"
            : null;

        var refundUrl = (generateAll || selectedOptions!.EnableRefundRequest)
            ? $"{frontendBaseUrl}/booking/refund/{appointment.Id}?token={token}"
            : null;

        return (sameDoctorUrl, confirmDoctorUrl, chooseNewDoctorUrl, refundUrl);
    }

    /// <summary>
    /// Check if should generate same doctor reschedule URL
    /// </summary>
    private static bool ShouldGenerateSameDoctorUrl(AppointmentEntity appointment, bool generateAll, RescheduleOptionsSelection? selectedOptions)
    {
        return appointment.DoctorId.HasValue && (generateAll || selectedOptions!.EnableSameDoctorReschedule);
    }

    /// <summary>
    /// Check if should generate confirm new doctor URL
    /// </summary>
    private static bool ShouldGenerateConfirmDoctorUrl(AppointmentEntity appointment, bool generateAll, RescheduleOptionsSelection? selectedOptions)
    {
        return appointment.DoctorId.HasValue &&
               appointment.AssignedDoctorId.HasValue &&
               (generateAll || selectedOptions!.EnableNewDoctorAssignment);
    }

    /// <summary>
    /// Publish appropriate cancellation event based on staff/patient cancellation and reschedule options
    /// </summary>
    private async Task PublishCancellationEventAsync(AppointmentEntity appointment, CancelAppointmentRequest request, CancellationDetails details, RescheduleResponse? rescheduleResponse)
    {
        var patientInfo = await GetPatientInfoForNotificationAsync(appointment.PatientId);

        // CASE 1: Staff cancellation WITH reschedule options → Send notification ONLY (patient chooses later)
        if (details.IsStaffCancellation && rescheduleResponse != null)
        {
            await PublishStaffCancellationWithOptionsNotificationAsync(appointment, request, details, rescheduleResponse, patientInfo);
        }
        // CASE 2: Patient cancellation OR staff cancellation WITHOUT options → Immediate refund processing
        else
        {
            if (details.RefundPercentage > 0)
            {
                await PublishImmediateRefundEventAsync(appointment, request, details, patientInfo);
            }
            else
            {
                await PublishNoRefundNotificationAsync(appointment, request, patientInfo);
            }
        }
    }

    /// <summary>
    /// CASE 1: Publish notification event for staff cancellation WITH reschedule options
    /// Patient will receive email/SMS and choose: reschedule (Option 1/2/3) or refund (Option 4)
    /// This does NOT trigger Payment Service - just notification
    /// </summary>
    private async Task PublishStaffCancellationWithOptionsNotificationAsync(
        AppointmentEntity appointment,
        CancelAppointmentRequest request,
        CancellationDetails details,
        RescheduleResponse rescheduleResponse,
        PatientNotificationInfo patientInfo)
    {
        // Get payment amount to calculate potential refund
        var paymentAmount = await GetPaymentAmountAsync(appointment.Id);
        var potentialRefundAmount = paymentAmount.HasValue
            ? paymentAmount.Value * details.RefundPercentage / 100
            : (decimal?)null;

        // Get doctor and hospital info for notification context
        string? doctorName = null;
        string? hospitalName = null;

        if (appointment.DoctorId.HasValue)
        {
            try
            {
                var doctorRequest = new GetDoctorBasicInfoRequest { Id = appointment.DoctorId.Value.ToString() };
                var doctorResponse = await _grpcClients.DoctorClient.GetDoctorBasicInfoAsync(doctorRequest);
                doctorName = doctorResponse.FullName;
            }
            catch (Exception ex)
            {
                LogWarning("Failed to get doctor name for notification: {Error}", null, ex.Message);
            }
        }

        if (appointment.HospitalId.HasValue)
        {
            try
            {
                var hospitalRequest = new GetHospitalBasicInfoRequest { Id = appointment.HospitalId.Value.ToString() };
                var hospitalResponse = await _grpcClients.HospitalClient.GetHospitalBasicInfoAsync(hospitalRequest);
                hospitalName = hospitalResponse.Name;
            }
            catch (Exception ex)
            {
                LogWarning("Failed to get hospital name for notification: {Error}", null, ex.Message);
            }
        }

        var notificationEvent = new AppointmentCancelledWithOptionsNotificationEvent
        {
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            AppointmentDate = appointment.AppointmentDate,
            CancellationReason = request.CancellationReason,
            CancelledAt = DateTime.UtcNow,

            // Patient contact
            PatientEmail = patientInfo.Email,
            PatientPhone = patientInfo.Phone,
            PatientFullName = patientInfo.FullName,

            // Context info
            DoctorName = doctorName,
            HospitalName = hospitalName,

            // Reschedule options with deep links (4 options)
            RescheduleToken = rescheduleResponse.RescheduleToken,
            RescheduleTokenExpiry = rescheduleResponse.TokenExpiry,
            SameDoctorRescheduleUrl = rescheduleResponse.SameDoctorRescheduleUrl,
            ConfirmNewDoctorUrl = rescheduleResponse.ConfirmNewDoctorUrl,
            ChooseNewDoctorUrl = rescheduleResponse.ChooseNewDoctorUrl,
            RefundRequestUrl = rescheduleResponse.RefundRequestUrl,

            // Potential refund info (for display only)
            PotentialRefundPercentage = details.RefundPercentage,
            PotentialRefundAmount = potentialRefundAmount
        };

        await _eventBus.PublishAsync(notificationEvent);
        LogInfo("Published staff cancellation NOTIFICATION with options for appointment {AppointmentId} - patient will choose action",
            null, appointment.Id);
    }

    /// <summary>
    /// CASE 2: Publish immediate refund event for patient cancellation or staff cancellation WITHOUT options
    /// This directly triggers Payment Service to process refund automatically
    /// </summary>
    private async Task PublishImmediateRefundEventAsync(
        AppointmentEntity appointment,
        CancelAppointmentRequest request,
        CancellationDetails details,
        PatientNotificationInfo patientInfo)
    {
        var cancelledEvent = new AppointmentCancelledIntegrationEvent
        {
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            HospitalId = appointment.HospitalId,
            AppointmentDate = appointment.AppointmentDate,
            AppointmentType = (int)appointment.AppointmentType,
            CancellationReason = request.CancellationReason,
            CancelledByStaffId = request.CancelledByStaffId,
            CancelledByPatientId = request.CancelledByPatientId,
            CancelledAt = DateTime.UtcNow,
            RefundPercentage = details.RefundPercentage,

            // Patient info for notification
            PatientEmail = patientInfo.Email,
            PatientPhone = patientInfo.Phone,
            PatientFullName = patientInfo.FullName
        };

        await _eventBus.PublishAsync(cancelledEvent);

        // Invalidate available slots cache after cancellation
        // The cancelled slot should become available again
        if (appointment.DoctorId.HasValue)
        {
            await InvalidateAvailableSlotsCacheAsync(
                appointment.DoctorId.Value,
                appointment.AppointmentDate,
                appointment.ServiceId);
        }

        LogInfo("Published refund event for appointment {AppointmentId} with {Refund}% refund",
            null, appointment.Id, details.RefundPercentage);
    }

    /// <summary>
    /// Publish immediate refund event for doctor change scenario (Option 3: Lower price)
    /// </summary>
    private async Task PublishDoctorChangeRefundEventAsync(
        AppointmentEntity appointment,
        decimal refundAmount,
        decimal originalPrice,
        decimal newPrice,
        BookingBasicInfo originalDoctor,
        BookingBasicInfo newDoctor,
        PatientNotificationInfo patientInfo)
    {
        var cancelledEvent = new AppointmentCancelledIntegrationEvent
        {
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId, // New doctor ID after update
            HospitalId = appointment.HospitalId,
            AppointmentDate = appointment.AppointmentDate,
            AppointmentType = (int)appointment.AppointmentType,
            CancellationReason = $"Chuyển từ {originalDoctor.FullName} (Cọc: {originalPrice:N0} VND) sang {newDoctor.FullName} (Cọc: {newPrice:N0} VND)",
            CancelledAt = DateTime.UtcNow,
            RefundPercentage = 100m, // Full refund of the difference

            // Doctor change context
            CancellationSource = "DOCTOR_CHANGE_REFUND",
            OriginalDoctorId = originalDoctor.DoctorId,
            OriginalDoctorName = originalDoctor.FullName,
            OriginalConsultationFee = originalPrice,
            NewDoctorId = newDoctor.DoctorId,
            NewDoctorName = newDoctor.FullName,
            NewConsultationFee = newPrice,
            RefundAmount = refundAmount,

            // Patient info for notification
            PatientEmail = patientInfo.Email,
            PatientPhone = patientInfo.Phone,
            PatientFullName = patientInfo.FullName
        };

        await _eventBus.PublishAsync(cancelledEvent);
        LogInfo("Published doctor change refund event for appointment {AppointmentId} - Refund: {RefundAmount} VND (from {OldDoctor} to {NewDoctor})",
            null, appointment.Id, refundAmount, originalDoctor.FullName, newDoctor.FullName);
    }

    /// <summary>
    /// Publish no-refund notification event directly to Notification Service
    /// </summary>
    private async Task PublishNoRefundNotificationAsync(AppointmentEntity appointment, CancelAppointmentRequest request, PatientNotificationInfo patientInfo)
    {
        var noRefundEvent = new AppointmentNoRefundNotificationEvent
        {
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            AppointmentDate = appointment.AppointmentDate,
            CancellationReason = request.CancellationReason,
            CancelledAt = DateTime.UtcNow,
            PatientEmail = patientInfo.Email,
            PatientPhone = patientInfo.Phone,
            PatientFullName = patientInfo.FullName
        };

        await _eventBus.PublishAsync(noRefundEvent);
        LogInfo("Published no-refund notification event for appointment {AppointmentId} - no refund due to late cancellation",
            null, appointment.Id);
    }

    /// <summary>
    /// Get doctor basic information for refund history
    /// </summary>
    private async Task<BookingBasicInfo> GetDoctorBasicInfoAsync(Guid doctorId)
    {
        try
        {
            var doctorRequest = new GetDoctorBasicInfoRequest { Id = doctorId.ToString() };
            var doctorResponse = await _grpcClients.DoctorClient.GetDoctorBasicInfoAsync(doctorRequest);

            return new BookingBasicInfo
            {
                DoctorId = doctorId,
                FullName = $"{doctorResponse.PositionName} {doctorResponse.FirstName} {doctorResponse.LastName}".Trim(),
                SpecialtyName = doctorResponse.SpecialtyName
            };
        }
        catch (GrpcCore.RpcException ex)
        {
            LogWarning("Failed to get doctor info for refund: {Error}", null, ex.Message);
            return new BookingBasicInfo
            {
                DoctorId = doctorId,
                FullName = "Bác sĩ",
                SpecialtyName = ""
            };
        }
    }

    /// <summary>
    /// Get patient information for notification
    /// </summary>
    private async Task<PatientNotificationInfo> GetPatientInfoForNotificationAsync(Guid patientId)
    {
        try
        {
            var patientRequest = new GetUserBasicInfoRequest { Id = patientId.ToString() };
            var patientResponse = await _grpcClients.UserClient.GetUserBasicInfoAsync(patientRequest);

            var fullName = $"{patientResponse.FirstName} {patientResponse.LastName}".Trim();
            if (string.IsNullOrEmpty(fullName))
            {
                fullName = "Quý khách";
            }

            return new PatientNotificationInfo
            {
                Email = patientResponse.Email,
                Phone = patientResponse.Phone,
                FullName = fullName
            };
        }
        catch (GrpcCore.RpcException ex)
        {
            LogWarning("Failed to get patient info for no-refund notification: {Error}", null, ex.Message);
            return new PatientNotificationInfo
            {
                Email = null,
                Phone = null,
                FullName = "Quý khách"
            };
        }
    }

    /// <summary>
    /// Reschedule appointment with same doctor (Option 1)
    /// </summary>
    public async Task<bool> RescheduleSameDoctorAsync(RescheduleSameDoctorRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Rescheduling appointment {AppointmentId} with same doctor", null, request.AppointmentId);

            // Get and validate appointment (common validation logic extracted)
            var appointment = await ValidateRescheduleEligibilityAsync(request.AppointmentId, request.RescheduleToken, "be rescheduled");

            // Check doctor availability
            if (appointment.DoctorId.HasValue)
            {
                var isDoctorAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
                    appointment.DoctorId.Value, request.NewAppointmentDate, request.NewAppointmentTimeId);
                if (!isDoctorAvailable)
                {
                    throw new DoctorNotAvailableException(appointment.DoctorId.Value, request.NewAppointmentDate);
                }
            }

            // Update appointment
            appointment.AppointmentDate = request.NewAppointmentDate;
            appointment.AppointmentTimeId = request.NewAppointmentTimeId;
            appointment.Status = AppointmentStatus.CONFIRMED;
            appointment.AssignedDoctorId = null; // Clear soft reservation
            appointment.SoftReservedUntil = null;
            // Clear pending fields after applying
            appointment.PendingNewDoctorId = null;
            appointment.PendingNewAppointmentDate = null;
            appointment.PendingNewAppointmentTimeId = null;
            appointment.IsRescheduled = true;
            appointment.RescheduleToken = null; // Clear token after use
            appointment.RescheduleTokenExpiry = null;

            await _appointmentRepository.UpdateAppointmentAsync(appointment);

            LogInfo("Successfully rescheduled appointment {AppointmentId}", null, request.AppointmentId);
            return true;
        }, "RescheduleSameDoctor");
    }

    /// <summary>
    /// Staff assigns new doctor (Option 2 - Step 1: Create soft reservation)
    /// Creates a soft lock on the doctor's schedule until patient confirms or expires
    /// Returns the confirmation URL for patient
    /// </summary>
    public async Task<string> AssignNewDoctorAsync(AssignNewDoctorRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Staff {StaffId} assigning doctor {DoctorId} to appointment {AppointmentId}",
                null, request.AssignedByStaffId, request.NewDoctorId, request.AppointmentId);

            // Get and validate appointment
            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(request.AppointmentId);
            if (appointment == null)
            {
                throw new AppointmentNotFoundException(request.AppointmentId);
            }

            // STEP 1: Assign doctor and create soft reservation
            await AssignDoctorWithSoftReservationAsync(appointment, request);

            // STEP 2: Cancel appointment with reschedule options (if needed)
            appointment = await EnsureAppointmentIsCancelledAsync(appointment, request);

            // STEP 3: Update appointment with assigned doctor info
            await _appointmentRepository.UpdateAppointmentAsync(appointment);

            // Generate confirmation URL for patient
            var confirmUrl = GenerateDoctorConfirmationUrl(appointment);

            LogInfo("Successfully assigned doctor {DoctorId} to appointment {AppointmentId} with soft reservation until {Expiry}",
                null, request.NewDoctorId, request.AppointmentId, appointment.SoftReservedUntil!);

            return confirmUrl;
        }, "AssignNewDoctor");
    }

    /// <summary>
    /// Assign doctor and create soft reservation for the appointment
    /// </summary>
    private async Task AssignDoctorWithSoftReservationAsync(AppointmentEntity appointment, AssignNewDoctorRequest request)
    {
        LogInfo("Assigning doctor {DoctorId} to appointment {AppointmentId} before cancellation",
            null, request.NewDoctorId, appointment.Id);

        // Determine final date/time (use provided or keep original)
        var finalDate = request.NewAppointmentDate ?? appointment.AppointmentDate;
        var finalTime = request.NewAppointmentTimeId ?? appointment.AppointmentTimeId;

        // Check doctor availability (respects soft reservations)
        var isDoctorAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
            request.NewDoctorId, finalDate, finalTime);
        if (!isDoctorAvailable)
        {
            throw new DoctorNotAvailableException(request.NewDoctorId, finalDate);
        }

        // Assign doctor and create soft reservation (48 hours)
        appointment.AssignedDoctorId = request.NewDoctorId;
        appointment.SoftReservedUntil = DateTime.UtcNow.AddHours(48);

        // Update date/time if changed
        if (request.NewAppointmentDate.HasValue)
        {
            appointment.AppointmentDate = request.NewAppointmentDate.Value;
        }
        if (request.NewAppointmentTimeId.HasValue)
        {
            appointment.AppointmentTimeId = request.NewAppointmentTimeId.Value;
        }
    }

    /// <summary>
    /// Ensure appointment is cancelled with reschedule options
    /// </summary>
    private async Task<AppointmentEntity> EnsureAppointmentIsCancelledAsync(AppointmentEntity appointment, AssignNewDoctorRequest request)
    {
        if (appointment.Status != AppointmentStatus.CANCELLED)
        {
            LogInfo("Appointment {AppointmentId} is not cancelled yet, cancelling now with assigned doctor...", null, appointment.Id);

            // Cancel the appointment with reschedule options enabled
            var cancelRequest = new CancelAppointmentRequest
            {
                AppointmentId = appointment.Id,
                CancellationReason = request.CancellationReason ?? "Staff is assigning a new doctor",
                CancelledByStaffId = request.AssignedByStaffId,
                EnableRescheduleOptions = true
            };

            await CancelAppointmentAsync(cancelRequest);

            // Reload appointment to get updated status and reschedule token
            var updatedAppointment = await _appointmentRepository.GetAppointmentByIdAsync(appointment.Id);
            if (updatedAppointment == null)
            {
                throw new AppointmentNotFoundException(appointment.Id);
            }
            return updatedAppointment;
        }

        // Appointment already cancelled, just validate reschedule token exists
        ValidateRescheduleToken(appointment);
        return appointment;
    }

    /// <summary>
    /// Validate reschedule token for cancelled appointment
    /// </summary>
    private static void ValidateRescheduleToken(AppointmentEntity appointment)
    {
        if (string.IsNullOrEmpty(appointment.RescheduleToken) ||
            appointment.RescheduleTokenExpiry == null ||
            appointment.RescheduleTokenExpiry < DateTime.UtcNow)
        {
            throw new AppointmentException("Reschedule token is missing or expired for cancelled appointment");
        }
    }

    /// <summary>
    /// Generate confirmation URL for patient to confirm assigned doctor
    /// </summary>
    private string GenerateDoctorConfirmationUrl(AppointmentEntity appointment)
    {
        var frontendBaseUrl = _frontendConfig.BaseUrl;
        return $"{frontendBaseUrl}/booking/confirm-doctor/{appointment.Id}?token={appointment.RescheduleToken}&newDoctorId={appointment.AssignedDoctorId}";
    }

    /// <summary>
    /// Request refund for cancelled appointment (Option 4)
    /// Patient explicitly chooses refund instead of rescheduling
    /// Reuses the same refund logic as immediate refund (PublishImmediateRefundEventAsync)
    /// </summary>
    public async Task<bool> RequestRefundAsync(RequestRefundRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Patient requesting refund for appointment {AppointmentId}", null, request.AppointmentId);

            // Get and validate appointment (common validation logic extracted)
            var appointment = await ValidateRescheduleEligibilityAsync(request.AppointmentId, request.RescheduleToken, "request refund");

            // Calculate refund percentage
            var now = DateTime.UtcNow;
            var isStaffCancellation = !string.IsNullOrEmpty(appointment.CancelledBy) && appointment.CancelledBy == "Staff";
            var refundPercentage = RefundPolicyHelper.CalculateRefundPercentage(appointment.AppointmentDate, now, isStaffCancellation);

            if (refundPercentage <= 0)
            {
                throw new AppointmentException("No refund available for this appointment");
            }

            // Get patient info for notification
            var patientInfo = await GetPatientInfoForNotificationAsync(appointment.PatientId);

            // Create cancellation details for refund event
            var cancellationDetails = new CancellationDetails
            {
                IsStaffCancellation = isStaffCancellation,
                CancelledBy = appointment.CancelledBy ?? "Patient",
                RefundPercentage = refundPercentage
            };

            // Create request for refund event
            var cancelRequest = new CancelAppointmentRequest
            {
                AppointmentId = appointment.Id,
                CancellationReason = appointment.Reason ?? "Patient requested refund",
                CancelledByStaffId = null,
                CancelledByPatientId = appointment.PatientId
            };

            // Reuse the same refund logic - publish to Payment Service
            await PublishImmediateRefundEventAsync(appointment, cancelRequest, cancellationDetails, patientInfo);

            // Clear reschedule token after use
            appointment.AssignedDoctorId = null; // Clear soft reservation
            appointment.SoftReservedUntil = null;
            // Clear pending fields after applying
            appointment.PendingNewDoctorId = null;
            appointment.PendingNewAppointmentDate = null;
            appointment.PendingNewAppointmentTimeId = null;
            appointment.RescheduleToken = null;
            appointment.RescheduleTokenExpiry = null;
            await _appointmentRepository.UpdateAppointmentAsync(appointment);

            LogInfo("Successfully published refund event for appointment {AppointmentId} - patient chose refund option",
                null, request.AppointmentId);
            return true;
        }, "RequestRefund");
    }

    /// <summary>
    /// Choose new doctor (Option 3)
    /// Handles 3 scenarios: same price, higher price, lower price
    /// </summary>
    public async Task<ChooseNewDoctorResponse> ChooseNewDoctorAsync(ChooseNewDoctorRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Patient choosing new doctor {DoctorId} for appointment {AppointmentId}",
                null, request.NewDoctorId, request.AppointmentId);

            // Validate and get appointment
            var appointment = await ValidateChooseNewDoctorRequestAsync(request);

            // Calculate price difference
            var (originalPrice, newPrice, priceDifference) = await CalculatePriceDifferenceAsync(appointment, request);

            // Create response object
            var response = new ChooseNewDoctorResponse
            {
                AppointmentId = appointment.Id,
                OriginalPrice = originalPrice,
                NewPrice = newPrice,
                PriceDifference = Math.Abs(priceDifference)
            };

            // Handle different price scenarios
            if (priceDifference == 0 || originalPrice == 0)
            {
                await HandleSamePriceScenarioAsync(appointment, request, response);
            }
            else if (priceDifference > 0)
            {
                await HandleHigherPriceScenarioAsync(appointment, request, priceDifference, response);
            }
            else
            {
                await HandleLowerPriceScenarioAsync(appointment, request, originalPrice, newPrice, priceDifference, response);
            }

            LogInfo("Successfully processed choose new doctor for appointment {AppointmentId} - Action: {Action}",
                null, request.AppointmentId, response.Action);

            return response;
        }, "ChooseNewDoctor");
    }

    /// <summary>
    /// Validate choose new doctor request and return appointment
    /// </summary>
    private async Task<AppointmentEntity> ValidateChooseNewDoctorRequestAsync(ChooseNewDoctorRequest request)
    {
        // Get and validate appointment
        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(request.AppointmentId);
        if (appointment == null)
        {
            throw new AppointmentNotFoundException(request.AppointmentId);
        }

        // Validate appointment is cancelled
        if (appointment.Status != AppointmentStatus.CANCELLED)
        {
            throw new AppointmentException("Only cancelled appointments can choose new doctor");
        }

        // Validate reschedule token
        if (string.IsNullOrEmpty(appointment.RescheduleToken) ||
            appointment.RescheduleToken != request.RescheduleToken ||
            appointment.RescheduleTokenExpiry == null ||
            appointment.RescheduleTokenExpiry < DateTime.UtcNow)
        {
            throw new AppointmentException("Invalid or expired reschedule token");
        }

        // Check new doctor availability (only for patient-chosen doctors)
        if (!request.IsStaffAssigned)
        {
            var isDoctorAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
                request.NewDoctorId, request.NewAppointmentDate, request.NewAppointmentTimeId);
            if (!isDoctorAvailable)
            {
                throw new DoctorNotAvailableException(request.NewDoctorId, request.NewAppointmentDate);
            }
        }

        return appointment;
    }

    /// <summary>
    /// Calculate price difference between original and new doctor
    /// </summary>
    private async Task<(decimal originalPrice, decimal newPrice, decimal priceDifference)> CalculatePriceDifferenceAsync(
        AppointmentEntity appointment, ChooseNewDoctorRequest request)
    {
        // Get original payment amount from Payment Service
        var originalPrice = await GetAppointmentPaymentAmountAsync(appointment.Id);

        // Get new doctor price from Doctor Service
        var newPrice = await GetDoctorPriceAsync(request.DoctorPriceId);

        var priceDifference = (newPrice * (decimal)0.3) - originalPrice;

        return (originalPrice, newPrice, priceDifference);
    }

    /// <summary>
    /// Handle same price scenario - Direct update
    /// </summary>
    private async Task HandleSamePriceScenarioAsync(
        AppointmentEntity appointment, ChooseNewDoctorRequest request, ChooseNewDoctorResponse response)
    {
        // Use appropriate method based on IsStaffAssigned flag
        if (request.IsStaffAssigned)
        {
            await ConfirmNewDoctorAsync(appointment);
            LogInfo("Confirmed staff-assigned doctor {DoctorId} for appointment {AppointmentId}",
                null, request.NewDoctorId, appointment.Id);
        }
        else
        {
            await UpdateAppointmentWithNewDoctorAsync(appointment, request);
        }

        response.Action = "direct_update";
        response.Message = "Appointment updated successfully with new doctor";
    }

    /// <summary>
    /// Handle higher price scenario - Need additional payment
    /// </summary>
    private async Task HandleHigherPriceScenarioAsync(
        AppointmentEntity appointment, ChooseNewDoctorRequest request, decimal priceDifference, ChooseNewDoctorResponse response)
    {
        if (!request.IsStaffAssigned)
        {
            // Store pending new doctor info (will be applied after successful payment callback)
            appointment.PendingNewDoctorId = request.NewDoctorId;
            appointment.PendingNewAppointmentDate = request.NewAppointmentDate;
            appointment.PendingNewAppointmentTimeId = request.NewAppointmentTimeId;
            await _appointmentRepository.UpdateAppointmentAsync(appointment);

            LogInfo("Saved pending doctor change for appointment {AppointmentId} (IsStaffAssigned={IsStaffAssigned}) - will apply after payment",
                null, appointment.Id, request.IsStaffAssigned);
        }
        response.Action = "payment_required";
        response.Message = $"Additional payment required: {priceDifference:N0} VND. Appointment will be updated after successful payment.";
    }

    /// <summary>
    /// Handle lower price scenario - Publish immediate refund event
    /// </summary>
    private async Task HandleLowerPriceScenarioAsync(
        AppointmentEntity appointment, ChooseNewDoctorRequest request,
        decimal originalPrice, decimal newPrice, decimal priceDifference, ChooseNewDoctorResponse response)
    {
        var refundAmount = Math.Abs(priceDifference);

        // Get doctor information for refund history transparency
        var originalDoctorInfo = await GetDoctorBasicInfoAsync(appointment.DoctorId!.Value);
        var newDoctorInfo = await GetDoctorBasicInfoAsync(request.NewDoctorId);
        var patientInfo = await GetPatientInfoForNotificationAsync(appointment.PatientId);

        // Update appointment with new doctor (use appropriate method based on IsStaffAssigned)
        if (request.IsStaffAssigned)
        {
            await ConfirmNewDoctorAsync(appointment);
            LogInfo("Confirmed staff-assigned doctor {DoctorId} with refund for appointment {AppointmentId}",
                null, request.NewDoctorId, appointment.Id);
        }
        else
        {
            await UpdateAppointmentWithNewDoctorAsync(appointment, request);
        }

        // Publish immediate refund event with doctor change context
        await PublishDoctorChangeRefundEventAsync(
            appointment,
            refundAmount,
            originalPrice,
            (newPrice * (decimal)0.3),
            originalDoctorInfo,
            newDoctorInfo,
            patientInfo);

        response.Action = "refund_created";
        response.Message = $"Appointment updated. Refund of {refundAmount:N0} VND will be processed automatically";
    }

    #endregion

    #region Status Operations

    public async Task<bool> UpdateAppointmentStatusAsync(UpdateAppointmentStatusRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating appointment {AppointmentId} status to {Status}",
                null, request.Id, request.Status);

            var existingAppointment = await _appointmentRepository.GetAppointmentByIdAsync(request.Id);
            if (existingAppointment == null)
            {
                throw new AppointmentNotFoundException(request.Id);
            }

            // Validate status transition
            var canUpdate = existingAppointment.Status switch
            {
                AppointmentStatus.PENDING => request.Status is AppointmentStatus.CONFIRMED or AppointmentStatus.CANCELLED,
                AppointmentStatus.CONFIRMED => request.Status is AppointmentStatus.COMPLETED or AppointmentStatus.CANCELLED,
                AppointmentStatus.COMPLETED => false, // Cannot change from completed
                AppointmentStatus.CANCELLED => false, // Cannot change from cancelled
                _ => false
            };
            if (!canUpdate)
            {
                throw new InvalidAppointmentStatusTransitionException(
                    existingAppointment.Status.ToString(), request.Status.ToString());
            }

            var updated = await _appointmentRepository.UpdateAppointmentStatusAsync(
                request.Id, request.Status, request.Result);
            if (!updated)
            {
                throw new AppointmentException("Failed to update appointment status");
            }

            // Invalidate available slots cache if status changes affect availability
            // When CANCELLED or COMPLETED, the slot should become available again
            if ((request.Status == AppointmentStatus.CANCELLED ||
                 request.Status == AppointmentStatus.COMPLETED)
                && existingAppointment.DoctorId.HasValue)
            {
                await InvalidateAvailableSlotsCacheAsync(
                    existingAppointment.DoctorId.Value,
                    existingAppointment.AppointmentDate,
                    existingAppointment.ServiceId);
            }

            LogInfo("Successfully updated appointment {AppointmentId} status to {Status}",
                null, request.Id, request.Status);

            return true;
        }, "UpdateAppointmentStatus");
    }


    #endregion

    #region Validation Operations

    public async Task<bool> ValidateAppointmentAsync(CreateAppointmentRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            await Task.CompletedTask; // Satisfy async requirement

            LogInfo("Validating appointment for patient {PatientId}", null, request.PatientId);

            // Validate appointment date is not in the past
            if (request.AppointmentDate.Date < DateTime.Today)
            {
                throw new AppointmentDateInPastException(request.AppointmentDate);
            }

            LogInfo("Appointment validation successful for patient {PatientId}", null, request.PatientId);
            return true;
        }, "ValidateAppointment");
    }

    #endregion

    #region Email Notification Operations

    public async Task<bool> SendAppointmentBookingSuccessEmailAsync(Guid appointmentId, Guid patientId, decimal amount = 0)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Sending appointment booking success email for AppointmentId: {AppointmentId}, PatientId: {PatientId}, Amount: {Amount}",
                null, appointmentId, patientId, amount);

            // 1. Get appointment details using existing method (already includes patient info)
            var appointment = await GetAppointmentByIdForPatientAsync(appointmentId);
            if (appointment == null)
            {
                LogWarning("Appointment not found for email notification: {AppointmentId}", null, appointmentId);
                return false;
            }

            // 2. Get patient information from enriched appointment or fallback to gRPC
            PatientInfo? patientInfo = appointment.PatientInfo;
            if (patientInfo == null || string.IsNullOrEmpty(patientInfo.Email))
            {
                LogWarning("Patient info not available in appointment, fetching from User Service: {PatientId}", null, patientId);
                var userResponse = await GetUserBasicInfoAsync(patientId);
                if (userResponse == null || string.IsNullOrEmpty(userResponse.Email))
                {
                    LogWarning("Patient info or email not found: {PatientId}", null, patientId);
                    return false;
                }

                // Map gRPC response to PatientInfo
                patientInfo = new PatientInfo
                {
                    Id = Guid.Parse(userResponse.Id),
                    Email = userResponse.Email,
                    Phone = userResponse.Phone,
                    FirstName = userResponse.FirstName,
                    LastName = userResponse.LastName,
                    AvatarUrl = userResponse.AvatarUrl
                };
            }

            // 3. Prepare email data
            var patientName = $"{patientInfo.FirstName} {patientInfo.LastName}".Trim();
            if (string.IsNullOrEmpty(patientName))
            {
                patientName = "Quý khách";
            }

            // Use extension method to format appointment time
            var appointmentTimeText = appointment.AppointmentTimeId.ToDisplayString();

            // Get doctor name and specialty from appointment
            var doctorName = appointment.DoctorInfo?.FullName;
            var doctorSpecialty = appointment.DoctorInfo?.SpecialtyName;

            // Get hospital info from appointment
            var hospitalName = appointment.HospitalInfo?.Name;
            var hospitalAddress = appointment.HospitalInfo?.Address;

            // Get service name (if available)
            var serviceName = appointment.ServiceInfo?.Name;

            // Determine appointment type display name
            var appointmentTypeText = appointment.AppointmentType.ToString();

            // 4. Publish event for Notification Service to send email
            var emailEvent = new AppointmentBookingSuccessNotificationEvent
            {
                AppointmentId = appointmentId,
                PatientId = patientId,
                PatientEmail = patientInfo.Email,
                AppointmentData = new AppointmentData
                {
                    PatientName = patientName,
                    AppointmentDate = appointment.AppointmentDate,
                    AppointmentTime = appointmentTimeText,
                    DoctorName = doctorName,
                    DoctorSpecialty = doctorSpecialty,
                    HospitalName = hospitalName,
                    HospitalAddress = hospitalAddress,
                    ServiceName = serviceName,
                    Amount = amount, // Use the actual payment amount from the event
                    AppointmentType = appointmentTypeText
                },
                EmailSubject = "Đặt lịch hẹn thành công - BookingCare",
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _eventBus.PublishAsync(emailEvent);

            LogInfo("Successfully published appointment booking success email event for AppointmentId: {AppointmentId} with Amount: {Amount}",
                null, appointmentId, amount);
            return true;
        }, "SendAppointmentBookingSuccessEmail");
    }

    /// <summary>
    /// Get user basic information using User Service gRPC
    /// </summary>
    private async Task<BookingCare.Services.User.Protos.UserBasicInfoResponse?> GetUserBasicInfoAsync(Guid userId)
    {
        try
        {
            var request = new GetUserBasicInfoRequest
            {
                Id = userId.ToString()
            };

            var response = await _grpcClients.UserClient.GetUserBasicInfoAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to get user basic info for UserId: {UserId}", null, userId);
            return null;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Determine management role from user roles, excluding Patient role
    /// </summary>
    private static Role GetManagementRole(List<string> userRoles)
    {
        // Remove Patient role if present
        var managementRoles = userRoles.Where(role =>
            !string.Equals(role, "Patient", StringComparison.OrdinalIgnoreCase)).ToList();

        if (!managementRoles.Any())
        {
            throw new UnauthorizedAccessException("User does not have management roles");
        }

        // Priority order: Admin > Doctor > Staff
        if (managementRoles.Any(role => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)))
        {
            return Role.ADMIN;
        }

        if (managementRoles.Any(role => string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase)))
        {
            return Role.DOCTOR;
        }

        if (managementRoles.Any(role => string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase)))
        {
            return Role.STAFF;
        }

        // If no recognized management role, default to ADMIN for full access
        return Role.ADMIN;
    }

    /// <summary>
    /// Batch fetch and map doctor information to appointments to avoid N+1 problem
    /// </summary>
    private async Task FetchAndMapDoctorInfoAsync(List<AppointmentResponse> responses, List<AppointmentEntity> entities, string context)
    {
        var doctorIds = entities
            .Where(e => e.DoctorId.HasValue)
            .Select(e => e.DoctorId!.Value)
            .Distinct()
            .ToList();

        if (!doctorIds.Any())
        {
            return;
        }

        try
        {
            var doctorRequest = new GetDoctorsBasicInfoRequest();
            doctorRequest.Ids.AddRange(doctorIds.Select(id => id.ToString()));

            var doctorsResponse = await _grpcClients.DoctorClient.GetDoctorsBasicInfoAsync(doctorRequest);
            var doctorDict = doctorsResponse.Doctors.ToDictionary(
                d => Guid.Parse(d.Id),
                d => d
            );

            LogInfo("Batch fetched {Count} doctors for {Context}", null, doctorDict.Count, context);

            // Map doctor info to appointments
            for (int i = 0; i < responses.Count; i++)
            {
                var entity = entities[i];
                if (entity.DoctorId.HasValue && doctorDict.TryGetValue(entity.DoctorId.Value, out var doctorInfo))
                {
                    responses[i].DoctorInfo = new DoctorInfo
                    {
                        Id = Guid.Parse(doctorInfo.Id),
                        Email = doctorInfo.Email,
                        FirstName = doctorInfo.FirstName,
                        LastName = doctorInfo.LastName,
                        FullName = doctorInfo.FullName,
                        PositionName = doctorInfo.PositionName,
                        SpecialtyName = doctorInfo.SpecialtyName,
                        AvatarUrl = doctorInfo.AvatarUrl,
                        HospitalId = !string.IsNullOrEmpty(doctorInfo.HospitalId)
                            ? Guid.Parse(doctorInfo.HospitalId)
                            : null
                    };
                }
            }
        }
        catch (GrpcCore.RpcException rpcEx)
        {
            LogWarning("gRPC error batch fetching doctors for {Context}: {Error}", null, context, rpcEx.Status.Detail);
        }
    }

    /// <summary>
    /// Batch fetch and map patient information to appointments to avoid N+1 problem
    /// </summary>
    private async Task FetchAndMapPatientInfoAsync(List<AppointmentResponse> responses, List<AppointmentEntity> entities, string context)
    {
        var patientIds = entities
            .Select(e => e.PatientId)
            .Distinct()
            .ToList();

        if (!patientIds.Any())
        {
            return;
        }

        try
        {
            var patientRequest = new GetUsersBasicInfoRequest();
            patientRequest.Ids.AddRange(patientIds.Select(id => id.ToString()));

            var patientsResponse = await _grpcClients.UserClient.GetUsersBasicInfoAsync(patientRequest);
            var patientDict = patientsResponse.Users.ToDictionary(
                p => Guid.Parse(p.Id),
                p => p
            );

            LogInfo("Batch fetched {Count} patients for {Context}", null, patientDict.Count, context);

            // Map patient info to appointments
            for (int i = 0; i < responses.Count; i++)
            {
                var entity = entities[i];
                if (patientDict.TryGetValue(entity.PatientId, out var patientInfo))
                {
                    responses[i].PatientInfo = new PatientInfo
                    {
                        Id = Guid.Parse(patientInfo.Id),
                        Email = patientInfo.Email,
                        Phone = patientInfo.Phone,
                        FirstName = patientInfo.FirstName,
                        LastName = patientInfo.LastName,
                        AvatarUrl = patientInfo.AvatarUrl
                    };
                }
            }
        }
        catch (GrpcCore.RpcException rpcEx)
        {
            LogWarning("gRPC error batch fetching patients for {Context}: {Error}", null, context, rpcEx.Status.Detail);
        }
    }

    /// <summary>
    /// Invalidate available slots cache for a specific doctor and date
    /// This ensures that after creating/cancelling/updating appointments,
    /// the cached available slots are refreshed to reflect current availability
    /// IMPORTANT: Available slots cache is stored by Schedule Service with prefix "BookingCare:Schedule:"
    /// We need to invalidate using the SAME prefix, not our Appointment Service prefix
    /// </summary>
    private async Task InvalidateAvailableSlotsCacheAsync(
        Guid doctorId,
        DateTime appointmentDate,
        Guid? serviceId)
    {
        try
        {
            // Convert DateTime to DateOnly format to match ScheduleService cache key format
            // ScheduleService uses DateOnly.ToString("yyyy-MM-dd") for cache keys
            var dateStr = DateOnly.FromDateTime(appointmentDate).ToString(DateFormat);

            // CRITICAL: Schedule Service uses "BookingCare:Schedule:" prefix
            // We need to use the FULL cache key including Schedule Service's prefix
            // Otherwise we'll try to delete "BookingCare:Appointment:available_slots:..." 
            // but the actual cache is "BookingCare:Schedule:available_slots:..."

            // Invalidate cache with specific serviceId (if provided)
            if (serviceId.HasValue)
            {
                var serviceIdStr = serviceId.Value.ToString();
                var cacheKey = CacheKeys.Format(CacheKeys.AvailableSlots, doctorId, dateStr, serviceIdStr);
                // Add Schedule Service prefix to match where cache was created
                var fullCacheKey = $"BookingCare:Schedule:{cacheKey}";
                await RemoveCacheDirectlyAsync(fullCacheKey);
                LogInfo("Invalidated available slots cache for doctor {DoctorId} on {Date} with serviceId {ServiceId}",
                    null, doctorId, dateStr, serviceIdStr);
            }

            // Invalidate cache without service filter (serviceId = "null")
            var nullServiceCacheKey = CacheKeys.Format(CacheKeys.AvailableSlots, doctorId, dateStr, "null");
            var fullNullCacheKey = $"BookingCare:Schedule:{nullServiceCacheKey}";
            await RemoveCacheDirectlyAsync(fullNullCacheKey);

            // Invalidate all variations using pattern matching
            var patternCacheKey = CacheKeys.Format(CacheKeys.AvailableSlots, doctorId, dateStr, "*");
            var fullPatternCacheKey = $"BookingCare:Schedule:{patternCacheKey}";
            await RemoveCacheByPatternDirectlyAsync(fullPatternCacheKey);

            LogInfo("Successfully invalidated available slots cache for doctor {DoctorId} on {Date}",
                null, doctorId, dateStr);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - cache invalidation failure should not fail appointment operations
            LogError(ex, "Failed to invalidate available slots cache for doctor {DoctorId} on {Date}: {Error}",
                null, doctorId, DateOnly.FromDateTime(appointmentDate).ToString(DateFormat), ex.Message);
        }
    }

    /// <summary>
    /// Remove cache directly without adding this service's prefix
    /// Used to delete cache keys created by other services (like Schedule Service)
    /// </summary>
    private async Task RemoveCacheDirectlyAsync(string fullCacheKey)
    {
        try
        {
            // Direct Redis operation using injected IConnectionMultiplexer
            var db = _redisConnection.GetDatabase(0);
            await db.KeyDeleteAsync(fullCacheKey);
            LogInfo("Directly removed cache key: {CacheKey}", null, fullCacheKey);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error directly removing cache key: {CacheKey}", null, fullCacheKey);
        }
    }

    /// <summary>
    /// Remove cache by pattern directly without adding this service's prefix
    /// Used to delete cache patterns created by other services (like Schedule Service)
    /// </summary>
    private async Task RemoveCacheByPatternDirectlyAsync(string fullPattern)
    {
        try
        {
            // Direct Redis operation using injected IConnectionMultiplexer
            var server = _redisConnection.GetServer(_redisConnection.GetEndPoints()[0]);
            var db = _redisConnection.GetDatabase(0);

            var keys = server.Keys(0, fullPattern).ToArray();
            foreach (var key in keys)
            {
                await db.KeyDeleteAsync(key);
            }

            LogInfo("Directly removed {Count} cache keys matching pattern: {Pattern}",
                null, keys.Length, fullPattern);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error directly removing cache by pattern: {Pattern}", null, fullPattern);
        }
    }

    /// <summary>
    /// Get role-based status counts from query parameters
    /// Determines the appropriate userId and hospitalId based on management role
    /// </summary>
    private async Task<AppointmentStatusCounts> GetRoleBasedStatusCountsAsync(AppointmentQueryRequest query, Role managementRole)
    {
        switch (managementRole)
        {
            case Role.DOCTOR:
                // Doctor: count by DoctorId from query
                if (query.DoctorId.HasValue)
                {
                    return await GetStatusCountsAsync(query.DoctorId.Value, Role.DOCTOR, null);
                }
                LogWarning("Doctor role but no DoctorId provided in query for status counts", null);
                return new AppointmentStatusCounts();

            case Role.STAFF:
                // Staff: count by HospitalId from query
                if (query.HospitalId.HasValue)
                {
                    return await GetStatusCountsAsync(null, Role.STAFF, query.HospitalId.Value);
                }
                LogWarning("Staff role but no HospitalId provided in query for status counts", null);
                return new AppointmentStatusCounts();

            case Role.ADMIN:
                // Admin: count all appointments
                return await GetStatusCountsAsync(null, Role.ADMIN, null);

            default:
                LogWarning("Unknown management role {Role} for status counts", null, managementRole);
                return new AppointmentStatusCounts();
        }
    }

    /// <summary>
    /// Get counts for all statuses for a specific user or organization
    /// Uses optimized repository method with single DB query
    /// Supports Patient, Doctor, Staff (by Hospital), and Admin (all) roles
    /// </summary>
    private async Task<AppointmentStatusCounts> GetStatusCountsAsync(Guid? userId, Role role, Guid? hospitalId = null)
    {
        try
        {
            // Determine parameters based on role
            Guid? patientId = null;
            Guid? doctorId = null;
            Guid? staffHospitalId = null;
            bool countAll = false;

            switch (role)
            {
                case Role.PATIENT:
                    patientId = userId;
                    LogInfo("Getting status counts for Patient {PatientId}", null, patientId!);
                    break;

                case Role.DOCTOR:
                    doctorId = userId;
                    LogInfo("Getting status counts for Doctor {DoctorId}", null, doctorId!);
                    break;

                case Role.STAFF:
                    staffHospitalId = hospitalId;
                    LogInfo("Getting status counts for Staff in Hospital {HospitalId}", null, staffHospitalId!);
                    break;

                case Role.ADMIN:
                    countAll = true;
                    LogInfo("Getting status counts for Admin (all appointments)", null);
                    break;

                default:
                    LogWarning("Unknown role {Role} for status counts", null, role);
                    return new AppointmentStatusCounts();
            }

            // Get counts using optimized repository method (single query with GROUP BY)
            var statusCountsDict = await _appointmentRepository.GetStatusCountsByUserAsync(
                patientId, doctorId, staffHospitalId, countAll);

            var counts = new AppointmentStatusCounts
            {
                Pending = statusCountsDict.GetValueOrDefault(AppointmentStatus.PENDING, 0),
                Confirmed = statusCountsDict.GetValueOrDefault(AppointmentStatus.CONFIRMED, 0),
                Cancelled = statusCountsDict.GetValueOrDefault(AppointmentStatus.CANCELLED, 0),
                Completed = statusCountsDict.GetValueOrDefault(AppointmentStatus.COMPLETED, 0)
            };

            counts.Total = counts.Pending + counts.Confirmed + counts.Cancelled + counts.Completed;

            LogInfo("Retrieved status counts for role {Role}: Total={Total}, Pending={Pending}, Confirmed={Confirmed}, Cancelled={Cancelled}, Completed={Completed}",
                null, role, counts.Total, counts.Pending, counts.Confirmed, counts.Cancelled, counts.Completed);

            return counts;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to get status counts for role {Role}: {Error}", null, role, ex.Message);
            // Return empty counts on error
            return new AppointmentStatusCounts();
        }
    }

    /// <summary>
    /// Get available doctors for staff to assign (Option 2)
    /// Queries Doctor Service via gRPC for doctors by hospital + specialty, then filters by availability
    /// </summary>
    public async Task<AvailableDoctorsResponse> GetAvailableDoctorsAsync(
        Guid hospitalId,
        Guid specialtyId,
        DateTime? appointmentDate,
        AppointmentTime? appointmentTimeId,
        bool checkAvailability = true)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Fetching doctors via gRPC for hospital {HospitalId}, specialty {SpecialtyId}, checkAvailability {CheckAvailability}",
                null, hospitalId, specialtyId, checkAvailability);

            // Step 1: Fetch doctors from Doctor Service
            var grpcResponse = await FetchDoctorsFromGrpcAsync(hospitalId, specialtyId);
            if (grpcResponse.Doctors == null || grpcResponse.Doctors.Count == 0)
            {
                LogInfo("No doctors found for hospital {HospitalId}, specialty {SpecialtyId}", null, hospitalId, specialtyId);
                return new AvailableDoctorsResponse();
            }

            // Step 2: Filter doctors based on availability
            var availableDoctors = await FilterDoctorsByAvailabilityAsync(
                grpcResponse.Doctors, checkAvailability, appointmentDate, appointmentTimeId);

            LogInfo("Found {Count} doctors out of {Total} for hospital {HospitalId}, specialty {SpecialtyId} (checkAvailability: {CheckAvailability})",
                null, availableDoctors.Count, grpcResponse.Doctors?.Count ?? 0, hospitalId, specialtyId, checkAvailability);

            return new AvailableDoctorsResponse
            {
                Doctors = availableDoctors,
                TotalCount = availableDoctors.Count
            };

        }, "GetAvailableDoctors");
    }

    /// <summary>
    /// Fetch doctors from gRPC Doctor Service
    /// </summary>
    private async Task<GetAvailableDoctorsResponse> FetchDoctorsFromGrpcAsync(Guid hospitalId, Guid specialtyId)
    {
        var grpcRequest = new GetAvailableDoctorsRequest
        {
            HospitalId = hospitalId.ToString(),
            SpecialtyId = specialtyId.ToString()
        };

        return await _grpcClients.DoctorClient.GetAvailableDoctorsAsync(grpcRequest);
    }

    /// <summary>
    /// Filter doctors by availability
    /// </summary>
    private async Task<List<AvailableDoctors>> FilterDoctorsByAvailabilityAsync(
        IEnumerable<AvailableDoctorInfo> doctors,
        bool checkAvailability,
        DateTime? appointmentDate,
        AppointmentTime? appointmentTimeId)
    {
        var availableDoctors = new List<AvailableDoctors>();

        foreach (var doctor in doctors)
        {
            if (!Guid.TryParse(doctor.Id, out var doctorId))
                continue;

            // If no availability check needed, add all doctors
            if (!checkAvailability)
            {
                availableDoctors.Add(MapToDoctorResponse(doctor, doctorId));
                continue;
            }

            // Check availability for specific date/time
            if (appointmentDate.HasValue && appointmentTimeId.HasValue)
            {
                var isAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
                    doctorId, appointmentDate.Value, appointmentTimeId.Value);

                if (isAvailable)
                {
                    availableDoctors.Add(MapToDoctorResponse(doctor, doctorId));
                }
            }
        }

        return availableDoctors;
    }

    /// <summary>
    /// Map gRPC doctor to response DTO
    /// </summary>
    private static AvailableDoctors MapToDoctorResponse(AvailableDoctorInfo doctor, Guid doctorId)
    {
        return new AvailableDoctors
        {
            Id = doctorId,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            FullName = doctor.FullName,
            AvatarUrl = doctor.AvatarUrl,
            PositionName = doctor.PositionName,
            SpecialtyName = doctor.SpecialtyName,
            YearsOfExperience = doctor.YearsOfExperience
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Validate appointment exists, is cancelled, and has valid reschedule token
    /// </summary>
    private async Task<AppointmentEntity> ValidateRescheduleEligibilityAsync(Guid appointmentId, string rescheduleToken, string errorContext)
    {
        // Get and validate appointment
        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
        if (appointment == null)
        {
            throw new AppointmentNotFoundException(appointmentId);
        }

        // Validate appointment is cancelled
        if (appointment.Status != AppointmentStatus.CANCELLED)
        {
            throw new AppointmentException($"Only cancelled appointments can {errorContext}");
        }

        // Validate reschedule token
        if (string.IsNullOrEmpty(appointment.RescheduleToken) ||
            appointment.RescheduleToken != rescheduleToken ||
            appointment.RescheduleTokenExpiry == null ||
            appointment.RescheduleTokenExpiry < DateTime.UtcNow)
        {
            throw new AppointmentException("Invalid or expired reschedule token");
        }

        return appointment;
    }

    #endregion
}