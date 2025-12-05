using AutoMapper;
using BookingCare.Services.Appointment.Configuration;
using BookingCare.Services.Appointment.Enums;
using BookingCare.Services.Appointment.Exceptions;
using BookingCare.Services.Appointment.Helpers;
using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Services.Appointment.Models.Entities;
using BookingCare.Services.Appointment.Models.Internal;
using BookingCare.Services.Appointment.Repositories;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using BookingCare.Services.Payment.Protos;
using BookingCare.Services.User.Protos;
using BookingCare.Shared.Cache.Constants;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.FileUpload.Services;
using BookingBasicInfo = BookingCare.Services.Appointment.Models.Internal.DoctorBasicInfo;
using GrpcCore = Grpc.Core; // Use alias to avoid namespace conflict
using RedisClient = StackExchange.Redis;

namespace BookingCare.Services.Appointment.Services;

/// <summary>
/// Service implementation for Appointment service operations
/// </summary>
public class AppointmentService : BaseService, IAppointmentService
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string NoInformationText = "Không có thông tin";
    private const string STAFF_VIEW_CONTEXT = "staff view";
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly GrpcClientWrapper _grpcClients;
    private readonly RedisClient.IConnectionMultiplexer _redisConnection;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly FrontendConfiguration _frontendConfig;
    private readonly IFileUploadService _fileUploadService;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IMapper mapper,
        IEventBus eventBus,
        AppointmentServiceDependencies dependencies,
        IFileUploadService fileUploadService,
        ILogger<AppointmentService> logger
    )
        : base(logger)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
        _eventBus = eventBus;
        _grpcClients = dependencies.GrpcClients;
        _redisConnection = dependencies.RedisConnection;
        _httpContextAccessor = dependencies.HttpContextAccessor;
        _frontendConfig = dependencies.FrontendConfig;
        _fileUploadService = fileUploadService;
    }

    #region Helper Methods

    /// <summary>
    /// Check and validate appointment limit before creating appointment
    /// </summary>
    private async Task CheckAndValidateAppointmentLimitAsync(Guid hospitalId)
    {
        try
        {
            var request = new CheckAppointmentLimitRequest
            {
                HospitalId = hospitalId.ToString(),
                AdditionalAppointments = 1,
            };
            var response = await _grpcClients.SubscriptionUsageClient.CheckAppointmentLimitAsync(
                request
            );
            if (!response.CanAdd)
            {
                throw new AppointmentException(
                    response.Message
                        ?? "Bạn đã đạt giới hạn số lượng lịch hẹn cho phép trong gói đăng ký. Vui lòng nâng cấp gói để thêm lịch hẹn."
                );
            }
        }
        catch (GrpcCore.RpcException ex) when (ex.StatusCode == GrpcCore.StatusCode.NotFound)
        {
            throw new AppointmentException("Không tìm thấy gói đăng ký cho bệnh viện này.");
        }
        catch (GrpcCore.RpcException ex)
        {
            LogError(
                ex,
                "gRPC error checking appointment limit for hospital {HospitalId}: {Error}",
                null,
                hospitalId,
                ex.Status.Detail
            );
            throw new AppointmentException(
                $"Không thể kiểm tra giới hạn lịch hẹn: {ex.Status.Detail}"
            );
        }
    }

    /// <summary>
    /// Increment appointment count after successful creation
    /// </summary>
    private async Task IncrementAppointmentCountAsync(Guid hospitalId)
    {
        try
        {
            var request = new IncrementAppointmentRequest
            {
                HospitalId = hospitalId.ToString(),
                Count = 1,
            };
            var response =
                await _grpcClients.SubscriptionUsageClient.IncrementAppointmentCountAsync(request);
            if (!response.Success)
            {
                LogWarning(
                    "Failed to increment appointment count for hospital {HospitalId}: {Message}",
                    null,
                    hospitalId,
                    response.Message
                );
            }
            else
            {
                LogInfo(
                    "Successfully incremented appointment count for hospital {HospitalId}",
                    null,
                    hospitalId
                );
            }
        }
        catch (GrpcCore.RpcException ex)
        {
            // Log error but don't throw - count update failure should not fail appointment creation
            LogError(
                ex,
                "gRPC error incrementing appointment count for hospital {HospitalId}: {Error}",
                null,
                hospitalId,
                ex.Status.Detail
            );
        }
    }

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
                AppointmentId = appointmentId.ToString(),
            };

            var response = await _grpcClients.PaymentClient.GetPaymentByAppointmentIdAsync(request);

            if (response.Success && response.Payment != null)
            {
                LogInfo(
                    "Successfully retrieved payment amount {Amount} for appointment {AppointmentId}",
                    null,
                    response.Payment.Amount,
                    appointmentId
                );
                return (decimal)response.Payment.Amount;
            }

            LogWarning(
                "No payment found for appointment {AppointmentId}: {Message}",
                null,
                appointmentId,
                response.Message
            );
            return null;
        }
        catch (Exception ex)
        {
            LogError(
                ex,
                "Error fetching payment info for appointment {AppointmentId}",
                null,
                appointmentId
            );
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

            var request = new GetDoctorPriceRequest { PriceId = priceId.ToString() };

            var response = await _grpcClients.DoctorClient.GetDoctorPriceAsync(request);

            if (response == null)
            {
                throw new AppointmentException($"Doctor price not found for ID {priceId}");
            }

            LogInfo(
                "Retrieved doctor price {Amount} VND for price ID {PriceId}",
                null,
                response.Amount,
                priceId
            );

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
    private async Task UpdateAppointmentWithNewDoctorAsync(
        AppointmentEntity appointment,
        ChooseNewDoctorRequest request
    )
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
        LogInfo(
            "Updated appointment {AppointmentId} with new doctor {DoctorId}",
            null,
            appointment.Id,
            request.NewDoctorId
        );
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

    /// <summary>
    /// Release held slot via Redis cache for Doctor booking
    /// </summary>
    private async Task ReleaseHeldSlotAsync(
        Guid doctorId,
        DateTime appointmentDate,
        AppointmentTime appointmentTimeId,
        Guid userId
    )
    {
        await ReleaseHeldSlotAsync(doctorId, "doctor", appointmentDate, appointmentTimeId, userId);
    }

    /// <summary>
    /// Release held slot via Redis cache (generic for both Doctor and ServiceMedical)
    /// </summary>
    private async Task ReleaseHeldSlotAsync(
        Guid targetId,
        string targetTypePrefix,
        DateTime appointmentDate,
        AppointmentTime appointmentTimeId,
        Guid userId
    )
    {
        try
        {
            LogInfo(
                "Releasing held slot for {TargetType} {TargetId} on {Date} at {AppointmentTimeId} by user {UserId}",
                null,
                targetTypePrefix,
                targetId,
                appointmentDate,
                appointmentTimeId,
                userId
            );

            var database = _redisConnection.GetDatabase();
            var dateStr = appointmentDate.ToString(DateFormat);

            // Cache key format must match HoldSlotService: held_slot:{targetTypePrefix}_{targetId}:{date}:{appointmentTimeId}:{userId}
            // HoldSlotService uses: CacheKeys.Format(CacheKeys.HeldSlot, $"{targetTypePrefix}_{request.TargetId}", date, appointmentTimeId, userId)
            var cacheKey = CacheKeys.Format(
                CacheKeys.HeldSlot,
                $"{targetTypePrefix}_{targetId}",
                dateStr,
                (int)appointmentTimeId,
                userId
            );

            // IMPORTANT: Add Schedule Service prefix to match where cache was created
            // Hold slots are created by Schedule Service with prefix "BookingCare:Schedule:"
            var fullCacheKey = $"BookingCare:Schedule:{cacheKey}";
            await database.KeyDeleteAsync(fullCacheKey);

            LogInfo("Successfully released held slot: {CacheKey}", null, fullCacheKey);
        }
        catch (Exception ex)
        {
            LogError(
                ex,
                "Error releasing held slot for {TargetType} {TargetId} on {Date} at {AppointmentTimeId} by user {UserId}",
                null,
                targetTypePrefix,
                targetId,
                appointmentDate,
                appointmentTimeId,
                userId
            );
            // Don't throw - this is not critical for appointment creation
        }
    }

    /// <summary>
    /// Release held slot via Redis cache for ServiceMedical booking
    /// </summary>
    private async Task ReleaseServiceMedicalHeldSlotAsync(
        Guid serviceMedicalId,
        DateTime appointmentDate,
        AppointmentTime appointmentTimeId,
        Guid userId
    )
    {
        await ReleaseHeldSlotAsync(
            serviceMedicalId,
            "service",
            appointmentDate,
            appointmentTimeId,
            userId
        );
    }

    /// <summary>
    /// Release held slot via Redis cache for Specialty booking (hospital assigns doctor mode)
    /// </summary>
    private async Task ReleaseSpecialtyHeldSlotAsync(
        Guid hospitalId,
        Guid specialtyId,
        DateTime appointmentDate,
        AppointmentTime appointmentTimeId,
        Guid userId
    )
    {
        try
        {
            LogInfo(
                "Releasing specialty held slot for hospital {HospitalId}, specialty {SpecialtyId} on {Date} at {AppointmentTimeId} by user {UserId}",
                null,
                hospitalId,
                specialtyId,
                appointmentDate,
                appointmentTimeId,
                userId
            );

            var database = _redisConnection.GetDatabase();
            var dateStr = appointmentDate.ToString(DateFormat);

            // Cache key format: holdslot:specialty:{hospitalId}:{specialtyId}:{date}:{appointmentTimeId}:{userId}
            var cacheKey = CacheKeys.Format(
                CacheKeys.SpecialtyHeldSlot,
                hospitalId,
                specialtyId,
                dateStr,
                (int)appointmentTimeId,
                userId
            );

            // IMPORTANT: Add Schedule Service prefix to match where cache was created
            // Hold slots are created by Schedule Service with prefix "BookingCare:Schedule:"
            var fullCacheKey = $"BookingCare:Schedule:{cacheKey}";
            await database.KeyDeleteAsync(fullCacheKey);

            LogInfo("Successfully released specialty held slot: {CacheKey}", null, fullCacheKey);
        }
        catch (Exception ex)
        {
            LogError(
                ex,
                "Error releasing specialty held slot for hospital {HospitalId}, specialty {SpecialtyId} on {Date}",
                null,
                hospitalId,
                specialtyId,
                appointmentDate
            );
            // Don't throw - this is not critical for appointment creation
        }
    }

    #endregion

    #region Appointment Operations

    /// <summary>
    /// Validate appointment before creation
    /// </summary>
    private async Task ValidateAppointmentBeforeCreationAsync(CreateAppointmentRequest request)
    {
        await ValidateAppointmentAsync(request);

        if (request.HospitalId.HasValue)
        {
            await CheckAndValidateAppointmentLimitAsync(request.HospitalId.Value);
        }
    }

    /// <summary>
    /// Check for appointment conflicts (for both self and relative bookings)
    /// </summary>
    private async Task CheckAppointmentConflictsAsync(CreateAppointmentRequest request)
    {
        var hasConflict = await _appointmentRepository.HasConflictingAppointmentAsync(
            request.PatientId,
            request.AppointmentDate,
            request.AppointmentTimeId,
            request.RelativeId // Pass relativeId to check correct conflict
        );
        if (hasConflict)
        {
            if (request.RelativeId.HasValue)
            {
                throw new AppointmentConflictException(
                    request.RelativeId.Value,
                    request.AppointmentDate,
                    "Người thân đã có lịch hẹn vào thời gian này"
                );
            }
            throw new AppointmentConflictException(request.PatientId, request.AppointmentDate);
        }
    }

    /// <summary>
    /// Check availability and set appointment status
    /// For Doctor booking: Check doctor availability in database
    /// For ServiceMedical booking: Check service availability in database
    /// This is the final check to prevent race conditions (held slot is just a soft lock)
    /// </summary>
    private async Task CheckAvailabilityAndSetStatusAsync(CreateAppointmentRequest request)
    {
        // Doctor booking - check doctor availability
        if (request.DoctorId.HasValue)
        {
            var isDoctorAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
                request.DoctorId.Value,
                request.AppointmentDate,
                request.AppointmentTimeId
            );
            if (!isDoctorAvailable)
            {
                throw new DoctorNotAvailableException(
                    request.DoctorId.Value,
                    request.AppointmentDate
                );
            }

            request.Status = AppointmentStatus.CONFIRMED;
            return;
        }

        // ServiceMedical booking (no doctor) - check service availability
        // This is the final check to prevent race conditions when multiple users book same slot
        if (request.ServiceId.HasValue)
        {
            var isServiceAvailable = await _appointmentRepository.IsServiceMedicalAvailableAsync(
                request.ServiceId.Value,
                request.AppointmentDate,
                request.AppointmentTimeId
            );
            if (!isServiceAvailable)
            {
                throw new ServiceMedicalNotAvailableException(
                    request.ServiceId.Value,
                    request.AppointmentDate
                );
            }

            request.Status = AppointmentStatus.CONFIRMED;
            return;
        }

        // No doctor and no service - set to pending (should not happen in normal flow)
        request.Status = AppointmentStatus.PENDING;
    }

    /// <summary>
    /// Perform post-creation tasks
    /// </summary>
    private async Task PerformPostCreationTasksAsync(
        AppointmentEntity appointmentEntity,
        CreateAppointmentRequest request,
        bool skipPayment
    )
    {
        if (request.HospitalId.HasValue)
        {
            await IncrementAppointmentCountAsync(request.HospitalId.Value);
        }

        // Handle Doctor booking
        if (request.DoctorId.HasValue)
        {
            await InvalidateAvailableSlotsCacheAsync(
                request.DoctorId.Value,
                request.AppointmentDate,
                request.ServiceId
            );

            await ReleaseHeldSlotAsync(
                request.DoctorId.Value,
                request.AppointmentDate,
                request.AppointmentTimeId,
                request.PatientAccountId
            );

            // If this is a specialty booking with doctor assigned, also invalidate specialty cache
            if (request.HospitalId.HasValue && request.SpecialtyId.HasValue)
            {
                await InvalidateSpecialtySlotsCacheAsync(
                    request.HospitalId.Value,
                    request.SpecialtyId.Value,
                    request.AppointmentDate,
                    request.AppointmentType
                );

                await ReleaseSpecialtyHeldSlotAsync(
                    request.HospitalId.Value,
                    request.SpecialtyId.Value,
                    request.AppointmentDate,
                    request.AppointmentTimeId,
                    request.PatientAccountId
                );
            }
        }
        // Handle Specialty booking (hospital assigns doctor mode - no doctor yet)
        // This is when patient books by specialty and hospital will assign doctor later
        else if (
            request.HospitalId.HasValue
            && request.SpecialtyId.HasValue
            && !request.ServiceId.HasValue
        )
        {
            await InvalidateSpecialtySlotsCacheAsync(
                request.HospitalId.Value,
                request.SpecialtyId.Value,
                request.AppointmentDate,
                request.AppointmentType
            );

            await ReleaseSpecialtyHeldSlotAsync(
                request.HospitalId.Value,
                request.SpecialtyId.Value,
                request.AppointmentDate,
                request.AppointmentTimeId,
                request.PatientAccountId
            );
        }
        // Handle ServiceMedical booking (when no doctor is specified but service is)
        else if (request.ServiceId.HasValue)
        {
            await InvalidateServiceMedicalSlotsCacheAsync(
                request.ServiceId.Value,
                request.AppointmentDate
            );

            await ReleaseServiceMedicalHeldSlotAsync(
                request.ServiceId.Value,
                request.AppointmentDate,
                request.AppointmentTimeId,
                request.PatientAccountId
            );
        }

        if (skipPayment)
        {
            await SendBookingSuccessEmailAsync(appointmentEntity.Id, request.PatientId);
        }
    }

    /// <summary>
    /// Send booking success email
    /// </summary>
    private async Task SendBookingSuccessEmailAsync(Guid appointmentId, Guid patientId)
    {
        LogInfo(
            "No payment flow - sending booking success email immediately for appointment {AppointmentId}",
            null,
            appointmentId
        );

        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(_httpContextAccessor.HttpContext!);
        await SendAppointmentBookingSuccessEmailAsync(
            appointmentId,
            patientId,
            accountId.ToString(),
            0
        );
    }

    public async Task<Guid> CreateAppointmentAsync(
        CreateAppointmentRequest request,
        bool skipPayment = false
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Creating appointment for patient {PatientId} on {AppointmentDate}, SkipPayment: {SkipPayment}",
                    null,
                    request.PatientId,
                    request.AppointmentDate,
                    skipPayment
                );

                await ValidateAppointmentBeforeCreationAsync(request);
                await CheckAppointmentConflictsAsync(request);
                await CheckAvailabilityAndSetStatusAsync(request);

                var appointmentEntity = _mapper.Map<AppointmentEntity>(request);
                await _appointmentRepository.CreateAppointmentAsync(appointmentEntity);

                await PerformPostCreationTasksAsync(appointmentEntity, request, skipPayment);

                LogInfo(
                    "Successfully created appointment {AppointmentId}",
                    null,
                    appointmentEntity.Id
                );
                return appointmentEntity.Id;
            },
            "CreateAppointment"
        );
    }

    public async Task<AppointmentResponse?> GetAppointmentByIdForPatientAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(
            async () =>
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
            },
            "GetAppointmentByIdForPatient"
        );
    }

    public async Task<AppointmentListResponse> GetAppointmentsByPatientAsync(
        AppointmentQueryRequest query
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Getting appointments with query: Page={PageNumber}, Size={PageSize}, IncludeStatusCounts={IncludeStatusCounts}",
                    null,
                    query.PageNumber,
                    query.PageSize,
                    query.IncludeStatusCounts
                );

                var (appointments, totalCount) = await _appointmentRepository.GetAppointmentsAsync(
                    query,
                    Role.PATIENT
                );
                var responses = _mapper.Map<List<AppointmentResponse>>(appointments);

                // Enrich appointments with additional information via gRPC calls based on user role
                await EnrichAppointmentsWithExternalDataAsync(
                    responses,
                    appointments,
                    Role.PATIENT
                );

                var response = new AppointmentListResponse
                {
                    Appointments = responses,
                    TotalCount = totalCount,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                };

                // Include status counts if requested
                // Pass all filters to ensure counts match the filtered results
                if (query.IncludeStatusCounts && query.PatientId.HasValue)
                {
                    response.StatusCounts = await GetStatusCountsAsync(
                        new StatusCountsRequest
                        {
                            UserId = query.PatientId.Value,
                            Role = Role.PATIENT,
                            FromDate = query.FromDate,
                            ToDate = query.ToDate,
                            AppointmentType = query.AppointmentType,
                            ForRelative = query.ForRelative,
                            SearchTerm = query.SearchTerm,
                        }
                    );
                    LogInfo(
                        "Included status counts for patient {PatientId} with filters",
                        null,
                        query.PatientId.Value
                    );
                }

                LogInfo(
                    "Retrieved {Count} appointments out of {TotalCount}",
                    null,
                    appointments.Count,
                    totalCount
                );
                return response;
            },
            "GetAppointmentsByPatient"
        );
    }

    public async Task<AppointmentListResponse> GetAppointmentsForManagementAsync(
        AppointmentQueryRequest query
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Getting management appointments with query: Page={PageNumber}, Size={PageSize}",
                    null,
                    query.PageNumber,
                    query.PageSize
                );

                // Get user roles from JWT token
                var userRoles = JwtHelper.GetUserRoles(_httpContextAccessor.HttpContext!);
                var managementRole = GetManagementRole(userRoles);

                LogInfo(
                    "User has roles: {Roles}, Management role determined: {ManagementRole}",
                    null,
                    string.Join(",", userRoles),
                    managementRole
                );

                var (appointments, totalCount) = await _appointmentRepository.GetAppointmentsAsync(
                    query,
                    managementRole
                );
                var responses = _mapper.Map<List<AppointmentResponse>>(appointments);

                // Enrich appointments with additional information via gRPC calls based on user role
                await EnrichAppointmentsWithExternalDataAsync(
                    responses,
                    appointments,
                    managementRole
                );

                var response = new AppointmentListResponse
                {
                    Appointments = responses,
                    TotalCount = totalCount,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                };

                // Include status counts if requested - use role-based logic
                if (query.IncludeStatusCounts)
                {
                    response.StatusCounts = await GetRoleBasedStatusCountsAsync(
                        query,
                        managementRole
                    );
                    LogInfo(
                        "Included status counts for management role {Role}",
                        null,
                        managementRole
                    );
                }

                LogInfo(
                    "Retrieved {Count} appointments out of {TotalCount} for management role {Role}",
                    null,
                    appointments.Count,
                    totalCount,
                    managementRole
                );
                return response;
            },
            "GetAppointmentsForManagement"
        );
    }

    /// <summary>
    /// Enrich appointments with external data via gRPC calls based on user role
    /// Performance optimized with batch requests to avoid N+1 problem
    /// </summary>
    private async Task EnrichAppointmentsWithExternalDataAsync(
        List<AppointmentResponse> responses,
        List<AppointmentEntity> entities,
        Role role
    )
    {
        if (!responses.Any())
            return;

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
            LogError(
                ex,
                "Failed to enrich appointments with external data: {Error}",
                null,
                ex.Message
            );
            // Don't throw - continue with un-enriched data
        }
    }

    /// <summary>
    /// Enrich appointments for Patient role - shows doctor, service, and hospital info
    /// </summary>
    private async Task EnrichForPatientRoleAsync(
        List<AppointmentResponse> responses,
        List<AppointmentEntity> entities
    )
    {
        await FetchAndMapDoctorInfoAsync(responses, entities, "appointments enrichment");

        // Batch fetch Service info for service medical appointments
        await FetchAndMapServiceInfoAsync(responses, entities, "appointments enrichment");

        // Batch fetch Specialty info for specialty booking (hospital assigns doctor mode)
        await FetchAndMapSpecialtyInfoAsync(responses, entities, "appointments enrichment");

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

                var hospitalsResponse =
                    await _grpcClients.HospitalClient.GetHospitalsBasicInfoAsync(hospitalRequest);
                var hospitalDict = hospitalsResponse.Hospitals.ToDictionary(
                    h => Guid.Parse(h.Id),
                    h => h
                );

                LogInfo(
                    "Batch fetched {Count} hospitals for appointments enrichment",
                    null,
                    hospitalDict.Count
                );

                // Map hospital info to appointments
                for (int i = 0; i < responses.Count; i++)
                {
                    var entity = entities[i];
                    if (
                        entity.HospitalId.HasValue
                        && hospitalDict.TryGetValue(entity.HospitalId.Value, out var hospitalInfo)
                    )
                    {
                        responses[i].HospitalInfo = new HospitalInfo
                        {
                            Id = Guid.Parse(hospitalInfo.Id),
                            Name = hospitalInfo.Name,
                            Address = hospitalInfo.Address,
                            Phone = hospitalInfo.Phone,
                            Email = hospitalInfo.Email,
                            AvatarUrl = hospitalInfo.AvatarUrl,
                        };
                    }
                }
            }
            catch (GrpcCore.RpcException rpcEx)
            {
                LogWarning(
                    "gRPC error batch fetching hospitals: {Error}",
                    null,
                    rpcEx.Status.Detail
                );
            }
        }
    }

    /// <summary>
    /// Enrich appointments for Doctor role - shows patient info
    /// </summary>
    private async Task EnrichForDoctorRoleAsync(
        List<AppointmentResponse> responses,
        List<AppointmentEntity> entities
    )
    {
        await FetchAndMapPatientInfoAsync(responses, entities, "doctor view");
        await FetchAndMapRelativeInfoAsync(responses, entities, "doctor view");
    }

    /// <summary>
    /// Enrich appointments for Staff/Admin role - shows patient, relative, doctor, and service info
    /// </summary>
    private async Task EnrichForStaffRoleAsync(
        List<AppointmentResponse> responses,
        List<AppointmentEntity> entities
    )
    {
        await FetchAndMapPatientInfoAsync(responses, entities, STAFF_VIEW_CONTEXT);
        await FetchAndMapRelativeInfoAsync(responses, entities, STAFF_VIEW_CONTEXT);
        await FetchAndMapDoctorInfoAsync(responses, entities, STAFF_VIEW_CONTEXT);
        await FetchAndMapServiceInfoAsync(responses, entities, STAFF_VIEW_CONTEXT);
        await FetchAndMapSpecialtyInfoAsync(responses, entities, STAFF_VIEW_CONTEXT);

        // Fetch payment information and calculate remaining payment (ConsultationFees) for Staff
        await FetchAndCalculateRemainingPaymentAsync(responses, entities);
    }

    #endregion

    #region Cancel Operations

    public async Task<RescheduleResponse?> CancelAppointmentAsync(CancelAppointmentRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Cancelling appointment {AppointmentId}", null, request.AppointmentId);

                // Get and validate appointment
                var appointment = await GetAndValidateAppointmentForCancellationAsync(
                    request.AppointmentId
                );

                // Calculate cancellation details
                var cancellationDetails = CalculateCancellationDetails(request, appointment);

                // Generate reschedule token if staff cancellation and reschedule options enabled
                var rescheduleResponse = await GenerateRescheduleTokenIfNeededAsync(
                    request,
                    appointment,
                    cancellationDetails
                );

                await CancelAppointmentInRepositoryAsync(
                    appointment,
                    request.CancellationReason,
                    cancellationDetails.CancelledBy
                );

                await InvalidateCachesAfterCancellationAsync(appointment);

                // Publish appropriate event based on refund percentage and reschedule options
                await PublishCancellationEventAsync(
                    appointment,
                    request,
                    cancellationDetails,
                    rescheduleResponse
                );

                return rescheduleResponse;
            },
            "CancelAppointment"
        );
    }

    private async Task<RescheduleResponse?> GenerateRescheduleTokenIfNeededAsync(
        CancelAppointmentRequest request,
        AppointmentEntity appointment,
        CancellationDetails cancellationDetails
    )
    {
        if (!cancellationDetails.IsStaffCancellation || !request.EnableRescheduleOptions)
        {
            return null;
        }

        var rescheduleResponse = await GenerateRescheduleResponseAsync(
            appointment,
            request.RescheduleOptions
        );

        appointment.RescheduleToken = rescheduleResponse.RescheduleToken;
        appointment.RescheduleTokenExpiry = rescheduleResponse.TokenExpiry;

        return rescheduleResponse;
    }

    private async Task CancelAppointmentInRepositoryAsync(
        AppointmentEntity appointment,
        string cancellationReason,
        string cancelledBy
    )
    {
        var cancelled = await _appointmentRepository.CancelAppointmentAsync(
            appointment,
            cancellationReason,
            cancelledBy
        );

        if (!cancelled)
        {
            throw new AppointmentException("Failed to cancel appointment");
        }
    }

    private async Task InvalidateCachesAfterCancellationAsync(AppointmentEntity appointment)
    {
        // Only invalidate if appointment date is in the future (slot can be booked again)
        if (appointment.AppointmentDate.Date < DateTime.UtcNow.Date)
        {
            return;
        }

        if (appointment.DoctorId.HasValue)
        {
            await InvalidateDoctorAndSpecialtyCachesAsync(appointment);
            return;
        }

        if (appointment.ServiceId.HasValue)
        {
            await InvalidateServiceMedicalSlotsCacheAsync(
                appointment.ServiceId.Value,
                appointment.AppointmentDate
            );

            LogInfo(
                "Invalidated service medical available slots cache after cancelling appointment {AppointmentId} - slot becomes available again",
                null,
                appointment.Id
            );
        }
    }

    private async Task InvalidateDoctorAndSpecialtyCachesAsync(AppointmentEntity appointment)
    {
        if (!appointment.DoctorId.HasValue)
        {
            return;
        }

        await InvalidateAvailableSlotsCacheAsync(
            appointment.DoctorId.Value,
            appointment.AppointmentDate,
            appointment.ServiceId
        );

        LogInfo(
            "Invalidated doctor available slots cache after cancelling appointment {AppointmentId} - slot becomes available again",
            null,
            appointment.Id
        );

        if (!appointment.HospitalId.HasValue || !appointment.SpecialtyId.HasValue)
        {
            return;
        }

        await InvalidateSpecialtySlotsCacheAsync(
            appointment.HospitalId.Value,
            appointment.SpecialtyId.Value,
            appointment.AppointmentDate,
            appointment.AppointmentType
        );

        LogInfo(
            "Invalidated specialty available slots cache after cancelling appointment {AppointmentId} - slot becomes available again",
            null,
            appointment.Id
        );
    }

    /// <summary>
    /// Generate reschedule token without cancelling appointment (lazy token generation)
    /// Creates token and stores PendingRescheduleAction, but keeps appointment status unchanged
    /// </summary>
    public async Task<GenerateRescheduleTokenResponse> GenerateRescheduleTokenAsync(
        GenerateRescheduleTokenRequest request
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Generating reschedule token for appointment {AppointmentId} with action {Action}",
                    null,
                    request.AppointmentId,
                    request.RescheduleAction
                );

                // Validate appointment
                var appointment = await GetAndValidateAppointmentForRescheduleAsync(
                    request.AppointmentId,
                    request.PatientId
                );

                // Validate reschedule timing and action
                ValidateRescheduleTimingAndAction(appointment, request.RescheduleAction);

                // Generate and store token
                var (token, expiry) = GenerateAndStoreRescheduleToken(
                    appointment,
                    request.RescheduleAction
                );
                await _appointmentRepository.UpdateAppointmentAsync(appointment);

                // Build redirect URL
                var redirectUrl = BuildRescheduleRedirectUrl(
                    appointment,
                    token,
                    request.RescheduleAction
                );

                LogInfo(
                    "Generated reschedule token for appointment {AppointmentId}, expires at {Expiry}",
                    null,
                    appointment.Id,
                    expiry
                );

                return new GenerateRescheduleTokenResponse
                {
                    RescheduleToken = token,
                    TokenExpiry = expiry,
                    RedirectUrl = redirectUrl,
                    Message = "Reschedule token generated successfully",
                };
            },
            "GenerateRescheduleToken"
        );
    }

    /// <summary>
    /// Get and validate appointment for reschedule token generation
    /// </summary>
    private async Task<AppointmentEntity> GetAndValidateAppointmentForRescheduleAsync(
        Guid appointmentId,
        Guid patientId
    )
    {
        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
        if (appointment == null)
        {
            throw new AppointmentNotFoundException(appointmentId);
        }

        // Validate appointment status
        if (
            appointment.Status != AppointmentStatus.PENDING
            && appointment.Status != AppointmentStatus.CONFIRMED
        )
        {
            throw new AppointmentException(
                $"Cannot generate reschedule token for appointment with status {appointment.Status}"
            );
        }

        // Validate patient ownership
        if (appointment.PatientId != patientId)
        {
            throw new AppointmentException("You are not authorized to reschedule this appointment");
        }

        return appointment;
    }

    /// <summary>
    /// Validate reschedule timing (24 hours rule) and action
    /// </summary>
    private static void ValidateRescheduleTimingAndAction(
        AppointmentEntity appointment,
        string rescheduleAction
    )
    {
        // Validate timing
        if (
            !RefundPolicyHelper.IsRescheduleAllowed(
                appointment.AppointmentDate,
                appointment.AppointmentTimeId
            )
        )
        {
            var policyMessage = RefundPolicyHelper.GetReschedulePolicyMessage(
                appointment.AppointmentDate,
                appointment.AppointmentTimeId
            );
            throw new AppointmentException($"Không thể đổi lịch hẹn này. {policyMessage}");
        }

        // Validate action type
        if (
            rescheduleAction != PendingRescheduleAction.SAME_DOCTOR
            && rescheduleAction != PendingRescheduleAction.NEW_DOCTOR
        )
        {
            throw new AppointmentException($"Invalid reschedule action: {rescheduleAction}");
        }

        // Validate doctor is assigned for SAME_DOCTOR action
        if (
            rescheduleAction == PendingRescheduleAction.SAME_DOCTOR
            && !appointment.DoctorId.HasValue
        )
        {
            throw new AppointmentException(
                "Cannot reschedule with same doctor when no doctor is assigned"
            );
        }
    }

    /// <summary>
    /// Generate token and store in appointment entity
    /// </summary>
    private (string token, DateTime expiry) GenerateAndStoreRescheduleToken(
        AppointmentEntity appointment,
        string rescheduleAction
    )
    {
        var token = Guid.NewGuid().ToString("N");
        var expiry = CalculateRescheduleTokenExpiry(appointment.AppointmentDate);

        appointment.RescheduleToken = token;
        appointment.RescheduleTokenExpiry = expiry;
        appointment.PendingRescheduleAction = rescheduleAction;

        return (token, expiry);
    }

    /// <summary>
    /// Build redirect URL based on reschedule action
    /// </summary>
    private string BuildRescheduleRedirectUrl(
        AppointmentEntity appointment,
        string token,
        string rescheduleAction
    )
    {
        var frontendBaseUrl = _frontendConfig.BaseUrl;

        return rescheduleAction == PendingRescheduleAction.SAME_DOCTOR
            ? $"{frontendBaseUrl}/booking/reschedule/{appointment.Id}?token={token}&doctorId={appointment.DoctorId}"
            : $"{frontendBaseUrl}/doctors?hospitalId={appointment.HospitalId}&specialtyId={appointment.SpecialtyId}&rescheduleFor={appointment.Id}&token={token}";
    }

    /// <summary>
    /// Get appointment and validate it can be cancelled
    /// </summary>
    private async Task<AppointmentEntity> GetAndValidateAppointmentForCancellationAsync(
        Guid appointmentId
    )
    {
        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
        if (appointment == null)
        {
            throw new AppointmentNotFoundException(appointmentId);
        }

        // Validate current status
        if (
            appointment.Status != AppointmentStatus.PENDING
            && appointment.Status != AppointmentStatus.CONFIRMED
        )
        {
            throw new AppointmentException(
                $"Cannot cancel appointment with status {appointment.Status}. Only PENDING or CONFIRMED appointments can be cancelled."
            );
        }

        // Validate appointment is in the future
        // Use full appointment DateTime (date + time slot) for accurate comparison
        if (
            !RefundPolicyHelper.IsCancellationAllowed(
                appointment.AppointmentDate,
                appointment.AppointmentTimeId,
                DateTime.UtcNow
            )
        )
        {
            throw new AppointmentException(
                $"Cannot cancel appointment that has already passed. "
                    + $"Appointment was scheduled for {appointment.AppointmentDate:yyyy-MM-dd} at {appointment.AppointmentTimeId} UTC."
            );
        }

        return appointment;
    }

    /// <summary>
    /// Calculate cancellation details including refund percentage
    /// Uses full appointment DateTime (date + time slot) for accurate calculation
    /// </summary>
    private CancellationDetails CalculateCancellationDetails(
        CancelAppointmentRequest request,
        AppointmentEntity appointment
    )
    {
        var now = DateTime.UtcNow;
        var isStaffCancellation = request.CancelledByStaffId.HasValue;
        var cancelledBy = isStaffCancellation ? "Staff" : "Patient";

        // Use full appointment DateTime (date + time slot) for accurate refund calculation
        var refundPercentage = RefundPolicyHelper.CalculateRefundPercentage(
            appointment.AppointmentDate,
            appointment.AppointmentTimeId,
            now,
            isStaffCancellation
        );

        // Get refund info with full DateTime for accurate hours calculation
        var refundInfo = RefundPolicyHelper.GetRefundInfo(
            appointment.AppointmentDate,
            appointment.AppointmentTimeId,
            now,
            isStaffCancellation
        );

        LogInfo(
            "Appointment {AppointmentId} cancellation: {Hours:F1} hours before appointment, {Refund}% refund, IsStaffCancellation: {IsStaff}",
            null,
            appointment.Id,
            refundInfo.HoursUntilAppointment,
            refundPercentage,
            isStaffCancellation
        );

        return new CancellationDetails
        {
            IsStaffCancellation = isStaffCancellation,
            CancelledBy = cancelledBy,
            RefundPercentage = refundPercentage,
        };
    }

    /// <summary>
    /// Generate reschedule response with token and deep links for all 4 options
    /// Conditionally generates URLs based on appointment type (doctor-based vs service-based)
    /// </summary>
    private async Task<RescheduleResponse> GenerateRescheduleResponseAsync(
        AppointmentEntity appointment,
        RescheduleOptionsSelection? selectedOptions = null
    )
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
            RefundRequestUrl = urls.RefundUrl,
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
            var expiry =
                endOfDayBeforeAppointment < maxExpiry ? endOfDayBeforeAppointment : maxExpiry;

            LogInfo(
                "Reschedule token expiry calculated for future appointment: {AppointmentDate} -> {Expiry} (end of day before appointment)",
                null,
                appointmentDate,
                expiry
            );
            return expiry;
        }

        var endOfToday = currentTime.Date.AddDays(1).AddSeconds(-1);
        LogInfo(
            "Reschedule token expiry calculated for past/today appointment: {AppointmentDate} -> {Expiry} (end of today)",
            null,
            appointmentDate,
            endOfToday
        );
        return endOfToday;
    }

    /// <summary>
    /// Generate reschedule URLs based on selected options
    /// </summary>
    private (
        string? SameDoctorUrl,
        string? ConfirmDoctorUrl,
        string? ChooseNewDoctorUrl,
        string? RefundUrl
    ) GenerateRescheduleUrls(
        AppointmentEntity appointment,
        string token,
        RescheduleOptionsSelection? selectedOptions
    )
    {
        var frontendBaseUrl = _frontendConfig.BaseUrl;
        var generateAll = selectedOptions == null;

        var sameDoctorUrl = ShouldGenerateSameDoctorUrl(appointment, generateAll, selectedOptions)
            ? $"{frontendBaseUrl}/booking/reschedule/{appointment.Id}?token={token}"
            : null;

        var confirmDoctorUrl = ShouldGenerateConfirmDoctorUrl(
            appointment,
            generateAll,
            selectedOptions
        )
            ? $"{frontendBaseUrl}/booking/confirm-doctor/{appointment.Id}?token={token}&newDoctorId={appointment.AssignedDoctorId}"
            : null;

        var chooseNewDoctorUrl =
            (generateAll || selectedOptions!.EnableDoctorSelection)
                ? $"{frontendBaseUrl}/doctors?hospitalId={appointment.HospitalId}&specialtyId={appointment.SpecialtyId}&rescheduleFor={appointment.Id}&token={token}"
                : null;

        var refundUrl =
            (generateAll || selectedOptions!.EnableRefundRequest)
                ? $"{frontendBaseUrl}/booking/refund/{appointment.Id}?token={token}"
                : null;

        return (sameDoctorUrl, confirmDoctorUrl, chooseNewDoctorUrl, refundUrl);
    }

    /// <summary>
    /// Check if should generate same doctor reschedule URL
    /// </summary>
    private static bool ShouldGenerateSameDoctorUrl(
        AppointmentEntity appointment,
        bool generateAll,
        RescheduleOptionsSelection? selectedOptions
    )
    {
        return appointment.DoctorId.HasValue
            && (generateAll || selectedOptions!.EnableSameDoctorReschedule);
    }

    /// <summary>
    /// Check if should generate confirm new doctor URL
    /// </summary>
    private static bool ShouldGenerateConfirmDoctorUrl(
        AppointmentEntity appointment,
        bool generateAll,
        RescheduleOptionsSelection? selectedOptions
    )
    {
        return appointment.DoctorId.HasValue
            && appointment.AssignedDoctorId.HasValue
            && (generateAll || selectedOptions!.EnableNewDoctorAssignment);
    }

    /// <summary>
    /// Publish appropriate cancellation event based on staff/patient cancellation and reschedule options
    /// </summary>
    private async Task PublishCancellationEventAsync(
        AppointmentEntity appointment,
        CancelAppointmentRequest request,
        CancellationDetails details,
        RescheduleResponse? rescheduleResponse
    )
    {
        var patientInfo = await GetPatientInfoForNotificationAsync(appointment.PatientId);

        // CASE 1: Staff cancellation WITH reschedule options → Send notification ONLY (patient chooses later)
        if (details.IsStaffCancellation && rescheduleResponse != null)
        {
            await PublishStaffCancellationWithOptionsNotificationAsync(
                appointment,
                request,
                details,
                rescheduleResponse,
                patientInfo
            );
        }
        // CASE 2: Patient cancellation OR staff cancellation WITHOUT options → Check payment before processing
        else
        {
            if (details.RefundPercentage > 0)
            {
                // Check if appointment has payment by trying to get payment amount
                var paymentAmount = await GetPaymentAmountAsync(appointment.Id);

                if (paymentAmount.HasValue)
                {
                    // Has payment → Process refund via Payment Service
                    await PublishImmediateRefundEventAsync(
                        appointment,
                        request,
                        details,
                        patientInfo
                    );
                }
                else
                {
                    // No payment → Send cancellation success notification directly
                    await PublishCancellationSuccessNotificationAsync(
                        appointment,
                        request,
                        patientInfo
                    );
                }
            }
            else
            {
                // No refund (late cancellation) → Send no-refund notification
                await PublishNoRefundNotificationAsync(appointment, request, patientInfo);
            }
        }
    }

    /// <summary>
    /// Get doctor and hospital names for notification context
    /// Extracted to avoid code duplication across notification methods
    /// </summary>
    private async Task<(
        string? doctorName,
        string? hospitalName
    )> GetDoctorAndHospitalNamesForNotificationAsync(AppointmentEntity appointment)
    {
        string? doctorName = null;
        string? hospitalName = null;

        if (appointment.DoctorId.HasValue)
        {
            try
            {
                var doctorRequest = new GetDoctorBasicInfoRequest
                {
                    Id = appointment.DoctorId.Value.ToString(),
                };
                var doctorResponse = await _grpcClients.DoctorClient.GetDoctorBasicInfoAsync(
                    doctorRequest
                );
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
                var hospitalRequest = new GetHospitalBasicInfoRequest
                {
                    Id = appointment.HospitalId.Value.ToString(),
                };
                var hospitalResponse = await _grpcClients.HospitalClient.GetHospitalBasicInfoAsync(
                    hospitalRequest
                );
                hospitalName = hospitalResponse.Name;
            }
            catch (Exception ex)
            {
                LogWarning(
                    "Failed to get hospital name for notification: {Error}",
                    null,
                    ex.Message
                );
            }
        }

        return (doctorName, hospitalName);
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
        PatientNotificationInfo patientInfo
    )
    {
        // Get payment amount to calculate potential refund
        var paymentAmount = await GetPaymentAmountAsync(appointment.Id);
        var potentialRefundAmount = paymentAmount.HasValue
            ? paymentAmount.Value * details.RefundPercentage / 100
            : (decimal?)null;

        // Get doctor and hospital info for notification context
        var (doctorName, hospitalName) = await GetDoctorAndHospitalNamesForNotificationAsync(
            appointment
        );

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
            PotentialRefundAmount = potentialRefundAmount,
        };

        await _eventBus.PublishAsync(notificationEvent);
        LogInfo(
            "Published staff cancellation NOTIFICATION with options for appointment {AppointmentId} - patient will choose action",
            null,
            appointment.Id
        );
    }

    /// <summary>
    /// CASE 2: Publish immediate refund event for patient cancellation or staff cancellation WITHOUT options
    /// This directly triggers Payment Service to process refund automatically
    /// </summary>
    private async Task PublishImmediateRefundEventAsync(
        AppointmentEntity appointment,
        CancelAppointmentRequest request,
        CancellationDetails details,
        PatientNotificationInfo patientInfo
    )
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
            PatientFullName = patientInfo.FullName,
        };

        await _eventBus.PublishAsync(cancelledEvent);

        LogInfo(
            "Published refund event for appointment {AppointmentId} with {Refund}% refund",
            null,
            appointment.Id,
            details.RefundPercentage
        );
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
        PatientNotificationInfo patientInfo
    )
    {
        var cancelledEvent = new AppointmentCancelledIntegrationEvent
        {
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId, // New doctor ID after update
            HospitalId = appointment.HospitalId,
            AppointmentDate = appointment.AppointmentDate,
            AppointmentType = (int)appointment.AppointmentType,
            CancellationReason =
                $"Chuyển từ {originalDoctor.FullName} (Cọc: {originalPrice:N0} VND) sang {newDoctor.FullName} (Cọc: {newPrice:N0} VND)",
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
            PatientFullName = patientInfo.FullName,
        };

        await _eventBus.PublishAsync(cancelledEvent);
        LogInfo(
            "Published doctor change refund event for appointment {AppointmentId} - Refund: {RefundAmount} VND (from {OldDoctor} to {NewDoctor})",
            null,
            appointment.Id,
            refundAmount,
            originalDoctor.FullName,
            newDoctor.FullName
        );
    }

    /// <summary>
    /// Publish no-refund notification event directly to Notification Service
    /// </summary>
    private async Task PublishNoRefundNotificationAsync(
        AppointmentEntity appointment,
        CancelAppointmentRequest request,
        PatientNotificationInfo patientInfo
    )
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
            PatientFullName = patientInfo.FullName,
        };

        await _eventBus.PublishAsync(noRefundEvent);
        LogInfo(
            "Published no-refund notification event for appointment {AppointmentId} - no refund due to late cancellation",
            null,
            appointment.Id
        );
    }

    /// <summary>
    /// Publish cancellation success notification for appointments without payment
    /// Used when appointment is eligible for refund but no payment record exists
    /// </summary>
    private async Task PublishCancellationSuccessNotificationAsync(
        AppointmentEntity appointment,
        CancelAppointmentRequest request,
        PatientNotificationInfo patientInfo
    )
    {
        // Get doctor and hospital info for notification context using extracted method
        var (doctorName, hospitalName) = await GetDoctorAndHospitalNamesForNotificationAsync(
            appointment
        );

        var successEvent = new AppointmentCancelledSuccessNotificationEvent
        {
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            AppointmentDate = appointment.AppointmentDate,
            CancellationReason = request.CancellationReason,
            CancelledAt = DateTime.UtcNow,
            PatientEmail = patientInfo.Email,
            PatientPhone = patientInfo.Phone,
            PatientFullName = patientInfo.FullName,
            DoctorName = doctorName,
            HospitalName = hospitalName,
        };

        await _eventBus.PublishAsync(successEvent);
        LogInfo(
            "Published cancellation success notification for appointment {AppointmentId} - no payment record found",
            null,
            appointment.Id
        );
    }

    /// <summary>
    /// Get doctor basic information for refund history
    /// </summary>
    private async Task<BookingBasicInfo> GetDoctorBasicInfoAsync(Guid doctorId)
    {
        try
        {
            var doctorRequest = new GetDoctorBasicInfoRequest { Id = doctorId.ToString() };
            var doctorResponse = await _grpcClients.DoctorClient.GetDoctorBasicInfoAsync(
                doctorRequest
            );

            return new BookingBasicInfo
            {
                DoctorId = doctorId,
                FullName =
                    $"{doctorResponse.PositionName} {doctorResponse.FirstName} {doctorResponse.LastName}".Trim(),
                SpecialtyName = doctorResponse.SpecialtyName,
            };
        }
        catch (GrpcCore.RpcException ex)
        {
            LogWarning("Failed to get doctor info for refund: {Error}", null, ex.Message);
            return new BookingBasicInfo
            {
                DoctorId = doctorId,
                FullName = "Bác sĩ",
                SpecialtyName = "",
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
            var patientResponse = await _grpcClients.UserClient.GetUserBasicInfoAsync(
                patientRequest
            );

            var fullName = $"{patientResponse.FirstName} {patientResponse.LastName}".Trim();
            if (string.IsNullOrEmpty(fullName))
            {
                fullName = "Quý khách";
            }

            return new PatientNotificationInfo
            {
                Email = patientResponse.Email,
                Phone = patientResponse.Phone,
                FullName = fullName,
            };
        }
        catch (GrpcCore.RpcException ex)
        {
            LogWarning(
                "Failed to get patient info for no-refund notification: {Error}",
                null,
                ex.Message
            );
            return new PatientNotificationInfo
            {
                Email = null,
                Phone = null,
                FullName = "Quý khách",
            };
        }
    }

    /// <summary>
    /// Get hospital information for notification
    /// </summary>
    private async Task<HospitalNotificationInfo?> GetHospitalInfoForNotificationAsync(
        Guid hospitalId
    )
    {
        try
        {
            var request = new GetHospitalBasicInfoRequest { Id = hospitalId.ToString() };
            var response = await _grpcClients.HospitalClient.GetHospitalBasicInfoAsync(request);

            if (response == null || string.IsNullOrEmpty(response.Id))
            {
                return null;
            }

            return new HospitalNotificationInfo
            {
                HospitalId = Guid.Parse(response.Id),
                Name = response.Name,
                Address = response.Address,
            };
        }
        catch (GrpcCore.RpcException ex)
        {
            LogWarning("Failed to get hospital info for notification: {Error}", null, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Reschedule appointment with same doctor (Option 1)
    /// </summary>
    public async Task<bool> RescheduleSameDoctorAsync(RescheduleSameDoctorRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Rescheduling appointment {AppointmentId} with same doctor",
                    null,
                    request.AppointmentId
                );

                // Get and validate appointment (common validation logic extracted)
                var appointment = await ValidateRescheduleEligibilityAsync(
                    request.AppointmentId,
                    request.RescheduleToken,
                    "be rescheduled"
                );

                // Check doctor availability
                if (appointment.DoctorId.HasValue)
                {
                    var isDoctorAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
                        appointment.DoctorId.Value,
                        request.NewAppointmentDate,
                        request.NewAppointmentTimeId
                    );
                    if (!isDoctorAvailable)
                    {
                        throw new DoctorNotAvailableException(
                            appointment.DoctorId.Value,
                            request.NewAppointmentDate
                        );
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
                appointment.PendingRescheduleAction = null; // Clear pending reschedule action
                appointment.IsRescheduled = true;
                appointment.RescheduleToken = null; // Clear token after use
                appointment.RescheduleTokenExpiry = null;

                await _appointmentRepository.UpdateAppointmentAsync(appointment);

                LogInfo(
                    "Successfully rescheduled appointment {AppointmentId}",
                    null,
                    request.AppointmentId
                );
                return true;
            },
            "RescheduleSameDoctor"
        );
    }

    /// <summary>
    /// Staff assigns new doctor (Option 2 - Step 1: Create soft reservation)
    /// Creates a soft lock on the doctor's schedule until patient confirms or expires
    /// Returns the confirmation URL for patient
    /// </summary>
    public async Task<string> AssignNewDoctorAsync(AssignNewDoctorRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Staff {StaffId} assigning doctor {DoctorId} to appointment {AppointmentId}",
                    null,
                    request.AssignedByStaffId,
                    request.NewDoctorId,
                    request.AppointmentId
                );

                // Get and validate appointment
                var appointment = await _appointmentRepository.GetAppointmentByIdAsync(
                    request.AppointmentId
                );
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

                LogInfo(
                    "Successfully assigned doctor {DoctorId} to appointment {AppointmentId} with soft reservation until {Expiry}",
                    null,
                    request.NewDoctorId,
                    request.AppointmentId,
                    appointment.SoftReservedUntil!
                );

                return confirmUrl;
            },
            "AssignNewDoctor"
        );
    }

    /// <summary>
    /// Assign doctor and create soft reservation for the appointment
    /// </summary>
    private async Task AssignDoctorWithSoftReservationAsync(
        AppointmentEntity appointment,
        AssignNewDoctorRequest request
    )
    {
        LogInfo(
            "Assigning doctor {DoctorId} to appointment {AppointmentId} before cancellation",
            null,
            request.NewDoctorId,
            appointment.Id
        );

        // Determine final date/time (use provided or keep original)
        var finalDate = request.NewAppointmentDate ?? appointment.AppointmentDate;
        var finalTime = request.NewAppointmentTimeId ?? appointment.AppointmentTimeId;

        // Check doctor availability (respects soft reservations)
        var isDoctorAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
            request.NewDoctorId,
            finalDate,
            finalTime
        );
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
    private async Task<AppointmentEntity> EnsureAppointmentIsCancelledAsync(
        AppointmentEntity appointment,
        AssignNewDoctorRequest request
    )
    {
        if (appointment.Status != AppointmentStatus.CANCELLED)
        {
            LogInfo(
                "Appointment {AppointmentId} is not cancelled yet, cancelling now with assigned doctor...",
                null,
                appointment.Id
            );

            // Cancel the appointment with reschedule options enabled
            var cancelRequest = new CancelAppointmentRequest
            {
                AppointmentId = appointment.Id,
                CancellationReason =
                    request.CancellationReason ?? "Staff is assigning a new doctor",
                CancelledByStaffId = request.AssignedByStaffId,
                EnableRescheduleOptions = true,
            };

            await CancelAppointmentAsync(cancelRequest);

            // Reload appointment to get updated status and reschedule token
            var updatedAppointment = await _appointmentRepository.GetAppointmentByIdAsync(
                appointment.Id
            );
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
        if (
            string.IsNullOrEmpty(appointment.RescheduleToken)
            || appointment.RescheduleTokenExpiry == null
            || appointment.RescheduleTokenExpiry < DateTime.UtcNow
        )
        {
            throw new AppointmentException(
                "Reschedule token is missing or expired for cancelled appointment"
            );
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
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Patient requesting refund for appointment {AppointmentId}",
                    null,
                    request.AppointmentId
                );

                // Get and validate appointment (common validation logic extracted)
                var appointment = await ValidateRescheduleEligibilityAsync(
                    request.AppointmentId,
                    request.RescheduleToken,
                    "request refund"
                );

                // Calculate refund percentage using full appointment DateTime (date + time slot)
                var now = DateTime.UtcNow;
                var isStaffCancellation =
                    !string.IsNullOrEmpty(appointment.CancelledBy)
                    && appointment.CancelledBy == "Staff";
                var refundPercentage = RefundPolicyHelper.CalculateRefundPercentage(
                    appointment.AppointmentDate,
                    appointment.AppointmentTimeId,
                    now,
                    isStaffCancellation
                );

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
                    RefundPercentage = refundPercentage,
                };

                // Create request for refund event
                var cancelRequest = new CancelAppointmentRequest
                {
                    AppointmentId = appointment.Id,
                    CancellationReason = appointment.Reason ?? "Patient requested refund",
                    CancelledByStaffId = null,
                    CancelledByPatientId = appointment.PatientId,
                };

                // Reuse the same refund logic - publish to Payment Service
                await PublishImmediateRefundEventAsync(
                    appointment,
                    cancelRequest,
                    cancellationDetails,
                    patientInfo
                );

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

                LogInfo(
                    "Successfully published refund event for appointment {AppointmentId} - patient chose refund option",
                    null,
                    request.AppointmentId
                );
                return true;
            },
            "RequestRefund"
        );
    }

    /// <summary>
    /// Choose new doctor (Option 3)
    /// Handles 3 scenarios: same price, higher price, lower price
    /// </summary>
    public async Task<ChooseNewDoctorResponse> ChooseNewDoctorAsync(ChooseNewDoctorRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Patient choosing new doctor {DoctorId} for appointment {AppointmentId}",
                    null,
                    request.NewDoctorId,
                    request.AppointmentId
                );

                // Validate and get appointment
                var appointment = await ValidateChooseNewDoctorRequestAsync(request);

                // Calculate price difference
                var (originalPrice, newPrice, priceDifference) =
                    await CalculatePriceDifferenceAsync(appointment, request);

                // Create response object
                var response = new ChooseNewDoctorResponse
                {
                    AppointmentId = appointment.Id,
                    OriginalPrice = originalPrice,
                    NewPrice = newPrice,
                    PriceDifference = Math.Abs(priceDifference),
                };

                // Handle different price scenarios
                if (priceDifference == 0 || originalPrice == 0)
                {
                    await HandleSamePriceScenarioAsync(appointment, request, response);
                }
                else if (priceDifference > 0)
                {
                    await HandleHigherPriceScenarioAsync(
                        appointment,
                        request,
                        priceDifference,
                        response
                    );
                }
                else
                {
                    await HandleLowerPriceScenarioAsync(
                        appointment,
                        request,
                        originalPrice,
                        newPrice,
                        priceDifference,
                        response
                    );
                }

                LogInfo(
                    "Successfully processed choose new doctor for appointment {AppointmentId} - Action: {Action}",
                    null,
                    request.AppointmentId,
                    response.Action
                );

                return response;
            },
            "ChooseNewDoctor"
        );
    }

    /// <summary>
    /// Validate choose new doctor request and return appointment
    /// </summary>
    private async Task<AppointmentEntity> ValidateChooseNewDoctorRequestAsync(
        ChooseNewDoctorRequest request
    )
    {
        // Get and validate appointment
        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(
            request.AppointmentId
        );
        if (appointment == null)
        {
            throw new AppointmentNotFoundException(request.AppointmentId);
        }

        // Validate appointment status:
        // - CANCELLED (old flow: staff cancels and offers reschedule)
        // - PENDING/CONFIRMED with PendingRescheduleAction (new flow: patient initiates reschedule without cancelling)
        var isValidStatus =
            appointment.Status == AppointmentStatus.CANCELLED
            || (
                (
                    appointment.Status == AppointmentStatus.PENDING
                    || appointment.Status == AppointmentStatus.CONFIRMED
                ) && !string.IsNullOrEmpty(appointment.PendingRescheduleAction)
            );

        // Validate appointment is cancelled
        if (!isValidStatus)
        {
            throw new AppointmentException(
                "Only cancelled or pending appointments can choose new doctor"
            );
        }

        // Validate reschedule token
        if (
            string.IsNullOrEmpty(appointment.RescheduleToken)
            || appointment.RescheduleToken != request.RescheduleToken
            || appointment.RescheduleTokenExpiry == null
            || appointment.RescheduleTokenExpiry < DateTime.UtcNow
        )
        {
            throw new AppointmentException("Invalid or expired reschedule token");
        }

        // Check new doctor availability (only for patient-chosen doctors)
        if (!request.IsStaffAssigned)
        {
            var isDoctorAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
                request.NewDoctorId,
                request.NewAppointmentDate,
                request.NewAppointmentTimeId
            );
            if (!isDoctorAvailable)
            {
                throw new DoctorNotAvailableException(
                    request.NewDoctorId,
                    request.NewAppointmentDate
                );
            }
        }

        return appointment;
    }

    /// <summary>
    /// Calculate price difference between original and new doctor
    /// </summary>
    private async Task<(
        decimal originalPrice,
        decimal newPrice,
        decimal priceDifference
    )> CalculatePriceDifferenceAsync(AppointmentEntity appointment, ChooseNewDoctorRequest request)
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
        AppointmentEntity appointment,
        ChooseNewDoctorRequest request,
        ChooseNewDoctorResponse response
    )
    {
        // Use appropriate method based on IsStaffAssigned flag
        if (request.IsStaffAssigned)
        {
            await ConfirmNewDoctorAsync(appointment);
            LogInfo(
                "Confirmed staff-assigned doctor {DoctorId} for appointment {AppointmentId}",
                null,
                request.NewDoctorId,
                appointment.Id
            );
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
        AppointmentEntity appointment,
        ChooseNewDoctorRequest request,
        decimal priceDifference,
        ChooseNewDoctorResponse response
    )
    {
        if (!request.IsStaffAssigned)
        {
            // Store pending new doctor info (will be applied after successful payment callback)
            appointment.PendingNewDoctorId = request.NewDoctorId;
            appointment.PendingNewAppointmentDate = request.NewAppointmentDate;
            appointment.PendingNewAppointmentTimeId = request.NewAppointmentTimeId;
            await _appointmentRepository.UpdateAppointmentAsync(appointment);

            LogInfo(
                "Saved pending doctor change for appointment {AppointmentId} (IsStaffAssigned={IsStaffAssigned}) - will apply after payment",
                null,
                appointment.Id,
                request.IsStaffAssigned
            );
        }
        response.Action = "payment_required";
        response.Message =
            $"Additional payment required: {priceDifference:N0} VND. Appointment will be updated after successful payment.";
    }

    /// <summary>
    /// Handle lower price scenario - Publish immediate refund event
    /// </summary>
    private async Task HandleLowerPriceScenarioAsync(
        AppointmentEntity appointment,
        ChooseNewDoctorRequest request,
        decimal originalPrice,
        decimal newPrice,
        decimal priceDifference,
        ChooseNewDoctorResponse response
    )
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
            LogInfo(
                "Confirmed staff-assigned doctor {DoctorId} with refund for appointment {AppointmentId}",
                null,
                request.NewDoctorId,
                appointment.Id
            );
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
            patientInfo
        );

        response.Action = "refund_created";
        response.Message =
            $"Appointment updated. Refund of {refundAmount:N0} VND will be processed automatically";
    }

    #endregion

    #region Status Operations

    public async Task<bool> UpdateAppointmentStatusAsync(UpdateAppointmentStatusRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Updating appointment {AppointmentId} status to {Status}",
                    null,
                    request.Id,
                    request.Status
                );

                var existingAppointment = await _appointmentRepository.GetAppointmentByIdAsync(
                    request.Id
                );
                if (existingAppointment == null)
                {
                    throw new AppointmentNotFoundException(request.Id);
                }

                // Validate status transition
                ValidateStatusTransition(existingAppointment.Status, request.Status);

                var updated = await _appointmentRepository.UpdateAppointmentStatusAsync(
                    request.Id,
                    request.Status,
                    request.Result
                );
                if (!updated)
                {
                    throw new AppointmentException("Failed to update appointment status");
                }

                // Handle cache invalidation and notifications
                await HandlePostStatusUpdateActionsAsync(request, existingAppointment);

                LogInfo(
                    "Successfully updated appointment {AppointmentId} status to {Status}",
                    null,
                    request.Id,
                    request.Status
                );

                return true;
            },
            "UpdateAppointmentStatus"
        );
    }

    /// <summary>
    /// Validate appointment status transition
    /// </summary>
    private static void ValidateStatusTransition(
        AppointmentStatus currentStatus,
        AppointmentStatus newStatus
    )
    {
        var canUpdate = currentStatus switch
        {
            AppointmentStatus.PENDING => newStatus
                is AppointmentStatus.CONFIRMED
                    or AppointmentStatus.CANCELLED,
            AppointmentStatus.CONFIRMED => newStatus
                is AppointmentStatus.COMPLETED
                    or AppointmentStatus.CANCELLED,
            AppointmentStatus.COMPLETED => false, // Cannot change from completed
            AppointmentStatus.CANCELLED => false, // Cannot change from cancelled
            _ => false,
        };

        if (!canUpdate)
        {
            throw new InvalidAppointmentStatusTransitionException(
                currentStatus.ToString(),
                newStatus.ToString()
            );
        }
    }

    /// <summary>
    /// Handle post-status update actions like cache invalidation and notifications
    /// </summary>
    private async Task HandlePostStatusUpdateActionsAsync(
        UpdateAppointmentStatusRequest request,
        AppointmentEntity existingAppointment
    )
    {
        // Invalidate available slots cache only for direct CANCELLED status update
        // (when not going through CancelAppointmentAsync - edge case)
        if (ShouldInvalidateCache(request.Status, existingAppointment))
        {
            await InvalidateAvailableSlotsCacheAsync(
                existingAppointment.DoctorId!.Value,
                existingAppointment.AppointmentDate,
                existingAppointment.ServiceId
            );

            LogInfo(
                "Invalidated cache after direct status update to CANCELLED for appointment {AppointmentId}",
                null,
                request.Id
            );
        }

        // Publish notification event if status is COMPLETED and result exists
        if (
            request.Status == AppointmentStatus.COMPLETED
            && !string.IsNullOrEmpty(existingAppointment.Result)
        )
        {
            await PublishAppointmentResultNotificationAsync(existingAppointment);
        }
    }

    /// <summary>
    /// Determine if cache should be invalidated for the status update
    /// </summary>
    private static bool ShouldInvalidateCache(
        AppointmentStatus newStatus,
        AppointmentEntity appointment
    )
    {
        return newStatus == AppointmentStatus.CANCELLED
            && appointment.DoctorId.HasValue
            && appointment.AppointmentDate.Date >= DateTime.UtcNow.Date;
    }

    /// <summary>
    /// Update appointment result and automatically change status to COMPLETED
    /// Accepts result as text string, converts to .txt file, uploads to S3, and stores CloudFront URL
    /// This is backward compatible - frontend sends text as before, backend handles S3 upload transparently
    /// </summary>
    public async Task<bool> UpdateAppointmentResultAsync(UpdateAppointmentResultRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Updating appointment {AppointmentId} result - converting text to file and uploading to S3",
                    null,
                    request.AppointmentId
                );

                var existingAppointment = await GetAndValidateAppointmentForResultAsync(
                    request.AppointmentId
                );
                var (doctorName, patientName) = await GetParticipantNamesAsync(existingAppointment);
                var resultUrl = await GenerateAndUploadMedicalReportAsync(
                    request,
                    existingAppointment,
                    doctorName,
                    patientName
                );

                // Update result with CloudFront URL and set status to COMPLETED
                var updated = await _appointmentRepository.UpdateAppointmentStatusAsync(
                    request.AppointmentId,
                    AppointmentStatus.COMPLETED,
                    resultUrl
                );

                if (!updated)
                {
                    throw new AppointmentException(
                        "Failed to update appointment result in database"
                    );
                }

                LogInfo(
                    "Successfully updated appointment {AppointmentId} with result URL and status COMPLETED",
                    null,
                    request.AppointmentId
                );

                // Publish notification event for appointment result
                await PublishAppointmentResultNotificationAsync(existingAppointment);

                return true;
            },
            "UpdateAppointmentResult"
        );
    }

    /// <summary>
    /// Get and validate appointment for result update
    /// </summary>
    private async Task<AppointmentEntity> GetAndValidateAppointmentForResultAsync(
        Guid appointmentId
    )
    {
        var existingAppointment = await _appointmentRepository.GetAppointmentByIdAsync(
            appointmentId
        );
        if (existingAppointment == null)
        {
            throw new AppointmentNotFoundException(appointmentId);
        }

        // Validate that appointment can be completed
        if (existingAppointment.Status == AppointmentStatus.CANCELLED)
        {
            throw new InvalidAppointmentStatusTransitionException(
                existingAppointment.Status.ToString(),
                AppointmentStatus.COMPLETED.ToString()
            );
        }

        return existingAppointment;
    }

    /// <summary>
    /// Get doctor and patient names for the medical report
    /// </summary>
    private async Task<(string doctorName, string patientName)> GetParticipantNamesAsync(
        AppointmentEntity appointment
    )
    {
        string doctorName = NoInformationText;
        string patientName = NoInformationText;

        if (appointment.DoctorId.HasValue)
        {
            try
            {
                var doctorInfo = await GetDoctorBasicInfoAsync(appointment.DoctorId.Value);
                doctorName = doctorInfo.FullName ?? NoInformationText;
            }
            catch (Exception ex)
            {
                LogWarning("Failed to get doctor info: {Error}", null, ex.Message);
            }
        }

        try
        {
            var patientInfo = await GetUserBasicInfoAsync(appointment.PatientId);
            if (patientInfo != null)
            {
                patientName = $"{patientInfo.FirstName} {patientInfo.LastName}".Trim();
                if (string.IsNullOrEmpty(patientName))
                {
                    patientName = NoInformationText;
                }
            }
        }
        catch (Exception ex)
        {
            LogWarning("Failed to get patient info: {Error}", null, ex.Message);
        }

        return (doctorName, patientName);
    }

    /// <summary>
    /// Generate professional PDF medical report and upload to S3
    /// </summary>
    private async Task<string> GenerateAndUploadMedicalReportAsync(
        UpdateAppointmentResultRequest request,
        AppointmentEntity appointment,
        string doctorName,
        string patientName
    )
    {
        try
        {
            // Generate professional PDF report from markdown content
            var pdfBytes = MedicalReportPdfGenerator.GeneratePdfReport(
                request.Result,
                appointment.Id.ToString(),
                doctorName,
                patientName,
                appointment.AppointmentDate
            );

            // Create stream from PDF bytes
            using var pdfStream = new MemoryStream(pdfBytes);

            var uploadRequest = new FileUploadRequest
            {
                FileStream = pdfStream,
                FileName = $"result_{request.AppointmentId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
                ContentType = "application/pdf",
                Folder = "appointment-results",
                GenerateUniqueFileName = true,
                Metadata = new Dictionary<string, string>
                {
                    ["AppointmentId"] = request.AppointmentId.ToString(),
                    ["UploadedAt"] = DateTime.UtcNow.ToString("o"),
                    ["UploadType"] = "PdfReport",
                    ["ContentLength"] = pdfBytes.Length.ToString(),
                    ["OriginalFormat"] = "MarkdownToPdf",
                },
            };

            LogInfo(
                "Converting result to PDF medical report for appointment {AppointmentId}, doctor: {Doctor}, patient: {Patient}",
                null,
                request.AppointmentId,
                doctorName,
                patientName
            );

            var uploadResult = await _fileUploadService.UploadFileAsync(uploadRequest);

            if (!uploadResult.Success || string.IsNullOrEmpty(uploadResult.CloudFrontUrl))
            {
                throw new AppointmentException(
                    $"Failed to upload result file to S3: {uploadResult.ErrorMessage ?? "Unknown error"}"
                );
            }

            LogInfo(
                "Successfully uploaded PDF medical report for appointment {AppointmentId} to {Url}",
                null,
                request.AppointmentId,
                uploadResult.CloudFrontUrl
            );

            return uploadResult.CloudFrontUrl;
        }
        catch (Exception ex) when (ex is not AppointmentException)
        {
            LogError(
                ex,
                "Error converting to PDF and uploading for appointment {AppointmentId}",
                null,
                request.AppointmentId
            );
            throw new AppointmentException(
                "Failed to upload PDF medical report to S3",
                innerException: ex
            );
        }
    }

    /// <summary>
    /// Publish appointment result notification event
    /// </summary>
    private async Task PublishAppointmentResultNotificationAsync(AppointmentEntity appointment)
    {
        try
        {
            // Get patient info
            var patientResponse = await GetUserBasicInfoAsync(appointment.PatientId);
            if (patientResponse == null)
            {
                LogWarning(
                    "Failed to get patient info for appointment {AppointmentId}",
                    null,
                    appointment.Id
                );
                return;
            }

            // Get doctor info if available
            string? doctorName = null;
            if (appointment.DoctorId.HasValue)
            {
                var doctorInfo = await GetDoctorBasicInfoAsync(appointment.DoctorId.Value);
                doctorName = doctorInfo.FullName;
            }

            // Get hospital info if available
            string? hospitalName = null;
            if (appointment.HospitalId.HasValue)
            {
                try
                {
                    var hospitalRequest = new GetHospitalBasicInfoRequest
                    {
                        Id = appointment.HospitalId.Value.ToString(),
                    };
                    var hospitalResponse =
                        await _grpcClients.HospitalClient.GetHospitalBasicInfoAsync(
                            hospitalRequest
                        );
                    hospitalName = hospitalResponse.Name;
                }
                catch (Exception ex)
                {
                    LogWarning("Failed to get hospital info: {Error}", null, ex.Message);
                }
            }

            // Get appointment time display
            var timeDisplay = GetAppointmentTimeDisplay(appointment.AppointmentTimeId);

            // Publish event
            var @event = new AppointmentResultUpdatedEvent
            {
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                PatientEmail = patientResponse.Email,
                PatientName = $"{patientResponse.FirstName} {patientResponse.LastName}".Trim(),
                DoctorId = appointment.DoctorId,
                DoctorName = doctorName,
                HospitalName = hospitalName,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = timeDisplay,
                ResultUrl = appointment.Result ?? string.Empty,
                UpdatedAt = DateTime.UtcNow,
            };

            await _eventBus.PublishAsync(@event);

            LogInfo(
                "Published AppointmentResultUpdatedEvent for appointment {AppointmentId}",
                null,
                appointment.Id
            );
        }
        catch (Exception ex)
        {
            LogError(
                ex,
                "Failed to publish appointment result notification event for appointment {AppointmentId}",
                null,
                appointment.Id
            );
        }
    }

    /// <summary>
    /// Get appointment time display from enum
    /// Example: AT_08_00_09_00 -> "08:00 - 09:00"
    /// </summary>
    private static string GetAppointmentTimeDisplay(AppointmentTime appointmentTimeId)
    {
        var timeString = appointmentTimeId.ToString();
        var parts = timeString.Split('_');

        if (parts.Length >= 5 && parts[0] == "AT")
        {
            return $"{parts[1]}:{parts[2]} - {parts[3]}:{parts[4]}";
        }

        return timeString; // Fallback to enum name
    }

    #endregion

    #region Validation Operations

    public async Task<bool> ValidateAppointmentAsync(CreateAppointmentRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                await Task.CompletedTask; // Satisfy async requirement

                LogInfo("Validating appointment for patient {PatientId}", null, request.PatientId);

                // Validate appointment date is not in the past
                if (request.AppointmentDate.Date < DateTime.Today)
                {
                    throw new AppointmentDateInPastException(request.AppointmentDate);
                }

                LogInfo(
                    "Appointment validation successful for patient {PatientId}",
                    null,
                    request.PatientId
                );
                return true;
            },
            "ValidateAppointment"
        );
    }

    #endregion

    #region Email Notification Operations

    public async Task<bool> SendAppointmentBookingSuccessEmailAsync(
        Guid appointmentId,
        Guid patientId,
        string accountId,
        decimal amount = 0
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Sending appointment booking success email for AppointmentId: {AppointmentId}, PatientId: {PatientId}, AccountId: {AccountId}, Amount: {Amount}",
                    null,
                    appointmentId,
                    patientId,
                    accountId,
                    amount
                );

                // 1. Get appointment details using existing method (already includes patient info)
                var appointment = await GetAppointmentByIdForPatientAsync(appointmentId);
                if (appointment == null)
                {
                    LogWarning(
                        "Appointment not found for email notification: {AppointmentId}",
                        null,
                        appointmentId
                    );
                    return false;
                }

                // 2. Get patient information from enriched appointment or fallback to gRPC
                PatientInfo? patientInfo = appointment.PatientInfo;
                if (patientInfo == null || string.IsNullOrEmpty(patientInfo.Email))
                {
                    LogWarning(
                        "Patient info not available in appointment, fetching from User Service: {PatientId}",
                        null,
                        patientId
                    );
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
                        AvatarUrl = userResponse.AvatarUrl,
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
                    AccountId = accountId,
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
                        AppointmentType = appointmentTypeText,
                    },
                    EmailSubject = "Đặt lịch hẹn thành công - BookingCare",
                    CorrelationId = Guid.NewGuid().ToString(),
                };

                await _eventBus.PublishAsync(emailEvent);

                LogInfo(
                    "Successfully published appointment booking success email event for AppointmentId: {AppointmentId} with Amount: {Amount}",
                    null,
                    appointmentId,
                    amount
                );
                return true;
            },
            "SendAppointmentBookingSuccessEmail"
        );
    }

    /// <summary>
    /// Get user basic information using User Service gRPC
    /// </summary>
    private async Task<UserBasicInfoResponse?> GetUserBasicInfoAsync(Guid userId)
    {
        try
        {
            var request = new GetUserBasicInfoRequest { Id = userId.ToString() };

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
        var managementRoles = userRoles
            .Where(role => !string.Equals(role, "Patient", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!managementRoles.Any())
        {
            throw new UnauthorizedAccessException("User does not have management roles");
        }

        // Priority order: Admin > Doctor > Staff
        if (
            managementRoles.Any(role =>
                string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return Role.ADMIN;
        }

        if (
            managementRoles.Any(role =>
                string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return Role.DOCTOR;
        }

        if (
            managementRoles.Any(role =>
                string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return Role.STAFF;
        }

        // If no recognized management role, default to ADMIN for full access
        return Role.ADMIN;
    }

    /// <summary>
    /// Batch fetch and map doctor information to appointments to avoid N+1 problem
    /// Note: ConsultationFee is now taken from entity.Amount (stored at booking time) instead of gRPC
    /// </summary>
    private async Task FetchAndMapDoctorInfoAsync(
        List<AppointmentResponse> responses,
        List<AppointmentEntity> entities,
        string context
    )
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
            // Fetch doctor basic info without prices (prices are stored in entity.Amount)
            var doctorRequest = new GetDoctorsBasicInfoRequest();
            doctorRequest.Ids.AddRange(doctorIds.Select(id => id.ToString()));

            var doctorsResponse = await _grpcClients.DoctorClient.GetDoctorsBasicInfoAsync(
                doctorRequest
            );
            var doctorDict = doctorsResponse.Doctors.ToDictionary(d => Guid.Parse(d.Id), d => d);

            LogInfo("Batch fetched {Count} doctors for {Context}", null, doctorDict.Count, context);

            // Map doctor info to appointments
            for (int i = 0; i < responses.Count; i++)
            {
                var entity = entities[i];
                if (
                    entity.DoctorId.HasValue
                    && doctorDict.TryGetValue(entity.DoctorId.Value, out var doctorInfo)
                )
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
                            : null,
                        // Use Amount from entity (stored at booking time) for accurate historical data
                        ConsultationFee = entity.Amount,
                    };
                }
            }
        }
        catch (GrpcCore.RpcException rpcEx)
        {
            LogWarning(
                "gRPC error batch fetching doctors for {Context}: {Error}",
                null,
                context,
                rpcEx.Status.Detail
            );
        }
    }

    /// <summary>
    /// Batch fetch and map service medical information to appointments to avoid N+1 problem
    /// </summary>
    private async Task FetchAndMapServiceInfoAsync(
        List<AppointmentResponse> responses,
        List<AppointmentEntity> entities,
        string context
    )
    {
        var serviceIds = entities
            .Where(e => e.ServiceId.HasValue)
            .Select(e => e.ServiceId!.Value)
            .Distinct()
            .ToList();

        if (!serviceIds.Any())
        {
            return;
        }

        try
        {
            var serviceRequest = new ServiceMedical.Protos.GetServicesBasicInfoRequest();
            serviceRequest.Ids.AddRange(serviceIds.Select(id => id.ToString()));

            var servicesResponse =
                await _grpcClients.ServiceMedicalClient.GetServicesBasicInfoAsync(serviceRequest);
            var serviceDict = servicesResponse.Services.ToDictionary(s => Guid.Parse(s.Id), s => s);

            LogInfo(
                "Batch fetched {Count} services for {Context}",
                null,
                serviceDict.Count,
                context
            );

            // Map service info to appointments
            for (int i = 0; i < responses.Count; i++)
            {
                var entity = entities[i];
                if (
                    entity.ServiceId.HasValue
                    && serviceDict.TryGetValue(entity.ServiceId.Value, out var serviceInfo)
                )
                {
                    responses[i].ServiceInfo = new ServiceInfo
                    {
                        Id = Guid.Parse(serviceInfo.Id),
                        Name = serviceInfo.Name,
                        // Use Amount from entity (stored at booking time) for accurate historical data
                        Price = entity.Amount,
                        ImageUrl = serviceInfo.ImageUrl,
                    };
                }
            }
        }
        catch (GrpcCore.RpcException rpcEx)
        {
            LogWarning(
                "gRPC error batch fetching services for {Context}: {Error}",
                null,
                context,
                rpcEx.Status.Detail
            );
        }
    }

    /// <summary>
    /// Batch fetch and map specialty information to appointments to avoid N+1 problem
    /// Used for specialty booking (hospital assigns doctor mode) where no doctor is assigned yet
    /// </summary>
    private async Task FetchAndMapSpecialtyInfoAsync(
        List<AppointmentResponse> responses,
        List<AppointmentEntity> entities,
        string context
    )
    {
        // Only fetch specialty info for appointments that have SpecialtyId but no DoctorId
        // (specialty booking mode where hospital assigns doctor)
        var specialtyIds = entities
            .Where(e => e.SpecialtyId.HasValue && !e.DoctorId.HasValue)
            .Select(e => e.SpecialtyId!.Value)
            .Distinct()
            .ToList();

        if (!specialtyIds.Any())
        {
            return;
        }

        try
        {
            var specialtyRequest = new Doctor.Protos.GetSpecialtiesByIdsRequest();
            specialtyRequest.Ids.AddRange(specialtyIds.Select(id => id.ToString()));

            var specialtiesResponse = await _grpcClients.DoctorClient.GetSpecialtiesByIdsAsync(
                specialtyRequest
            );
            var specialtyDict = specialtiesResponse.Specialties.ToDictionary(
                s => Guid.Parse(s.Id),
                s => s
            );

            LogInfo(
                "Batch fetched {Count} specialties for {Context}",
                null,
                specialtyDict.Count,
                context
            );

            // Map specialty info to appointments
            for (int i = 0; i < responses.Count; i++)
            {
                var entity = entities[i];
                if (
                    entity.SpecialtyId.HasValue
                    && !entity.DoctorId.HasValue // Only for specialty booking (no doctor assigned)
                    && specialtyDict.TryGetValue(entity.SpecialtyId.Value, out var specialtyInfo)
                )
                {
                    responses[i].SpecialtyInfo = new SpecialtyInfo
                    {
                        Id = Guid.Parse(specialtyInfo.Id),
                        Name = specialtyInfo.Name,
                        ImageUrl = specialtyInfo.ImageUrl,
                    };
                }
            }
        }
        catch (GrpcCore.RpcException rpcEx)
        {
            LogWarning(
                "gRPC error batch fetching specialties for {Context}: {Error}",
                null,
                context,
                rpcEx.Status.Detail
            );
        }
    }

    /// <summary>
    /// Batch fetch payment information and calculate remaining payment for Staff role
    /// ConsultationFees = Amount (total fee) - Payment.Amount (deposit paid)
    /// Only populated for Staff role to show how much patient still needs to pay
    /// </summary>
    private async Task FetchAndCalculateRemainingPaymentAsync(
        List<AppointmentResponse> responses,
        List<AppointmentEntity> entities
    )
    {
        // Get all appointment IDs
        var appointmentIds = entities.Select(e => e.Id).Distinct().ToList();

        if (!appointmentIds.Any())
        {
            return;
        }

        try
        {
            var paymentRequest = new Payment.Protos.GetPaymentsByAppointmentIdsRequest();
            paymentRequest.AppointmentIds.AddRange(appointmentIds.Select(id => id.ToString()));

            var paymentsResponse =
                await _grpcClients.PaymentClient.GetPaymentsByAppointmentIdsAsync(paymentRequest);

            if (!paymentsResponse.Success || paymentsResponse.Payments == null)
            {
                LogWarning(
                    "Failed to fetch payments for staff view: {Message}",
                    null,
                    paymentsResponse.Message
                );
                return;
            }

            // Create dictionary for quick lookup: appointmentId -> payment amount (deposit)
            var paymentDict = paymentsResponse
                .Payments.Where(p =>
                    !string.IsNullOrEmpty(p.AppointmentId) && Guid.TryParse(p.AppointmentId, out _)
                )
                .ToDictionary(p => Guid.Parse(p.AppointmentId), p => (decimal)p.Amount);

            LogInfo(
                "Batch fetched {Count} payments for {Context}",
                null,
                paymentDict.Count,
                STAFF_VIEW_CONTEXT
            );

            // Calculate ConsultationFees (remaining payment) for each appointment
            for (int i = 0; i < responses.Count; i++)
            {
                var entity = entities[i];

                // Only calculate if appointment has an Amount (total fee)
                if (
                    entity.Amount.HasValue
                    && paymentDict.TryGetValue(entity.Id, out var depositAmount)
                )
                {
                    // ConsultationFees = Total Amount - Deposit Already Paid
                    var remainingPayment = entity.Amount.Value - depositAmount;

                    // Only set if there's a remaining amount (avoid showing negative or zero)
                    if (remainingPayment > 0)
                    {
                        responses[i].ConsultationFees = remainingPayment;
                    }
                    else
                    {
                        responses[i].ConsultationFees = 0; // Fully paid
                    }
                }
            }
        }
        catch (GrpcCore.RpcException rpcEx)
        {
            LogWarning(
                "gRPC error batch fetching payments for {Context}: {Error}",
                null,
                STAFF_VIEW_CONTEXT,
                rpcEx.Status.Detail
            );
        }
        catch (Exception ex)
        {
            LogError(
                ex,
                "Error calculating remaining payment for {Context}",
                null,
                STAFF_VIEW_CONTEXT
            );
        }
    }

    /// <summary>
    /// Batch fetch and map patient information to appointments to avoid N+1 problem
    /// </summary>
    private async Task FetchAndMapPatientInfoAsync(
        List<AppointmentResponse> responses,
        List<AppointmentEntity> entities,
        string context
    )
    {
        var patientIds = entities.Select(e => e.PatientId).Distinct().ToList();

        if (!patientIds.Any())
        {
            return;
        }

        try
        {
            var patientRequest = new GetUsersBasicInfoRequest();
            patientRequest.Ids.AddRange(patientIds.Select(id => id.ToString()));

            var patientsResponse = await _grpcClients.UserClient.GetUsersBasicInfoAsync(
                patientRequest
            );
            var patientDict = patientsResponse.Users.ToDictionary(p => Guid.Parse(p.Id), p => p);

            LogInfo(
                "Batch fetched {Count} patients for {Context}",
                null,
                patientDict.Count,
                context
            );

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
                        AvatarUrl = patientInfo.AvatarUrl,
                    };
                }
            }
        }
        catch (GrpcCore.RpcException rpcEx)
        {
            LogWarning(
                "gRPC error batch fetching patients for {Context}: {Error}",
                null,
                context,
                rpcEx.Status.Detail
            );
        }
    }

    /// <summary>
    /// Batch fetch and map relative information to appointments to avoid N+1 problem
    /// Only fetches for appointments that have RelativeId set
    /// </summary>
    private async Task FetchAndMapRelativeInfoAsync(
        List<AppointmentResponse> responses,
        List<AppointmentEntity> entities,
        string context
    )
    {
        var relativeIds = entities
            .Where(e => e.RelativeId.HasValue)
            .Select(e => e.RelativeId!.Value)
            .Distinct()
            .ToList();

        if (!relativeIds.Any())
        {
            return;
        }

        try
        {
            var relativeRequest = new GetRelativesBasicInfoRequest();
            relativeRequest.Ids.AddRange(relativeIds.Select(id => id.ToString()));

            var relativesResponse = await _grpcClients.UserClient.GetRelativesBasicInfoAsync(
                relativeRequest
            );
            var relativeDict = relativesResponse
                .Relatives.Where(r => r.Found)
                .ToDictionary(r => Guid.Parse(r.Id), r => r);

            LogInfo(
                "Batch fetched {Count} relatives for {Context}",
                null,
                relativeDict.Count,
                context
            );

            // Map relative info to appointments
            for (int i = 0; i < responses.Count; i++)
            {
                var entity = entities[i];
                if (
                    entity.RelativeId.HasValue
                    && relativeDict.TryGetValue(entity.RelativeId.Value, out var relativeInfo)
                )
                {
                    responses[i].RelativeInfo = new RelativeInfo
                    {
                        Id = Guid.Parse(relativeInfo.Id),
                        FirstName = relativeInfo.FirstName,
                        LastName = relativeInfo.LastName,
                        FullName = relativeInfo.FullName,
                        Gender = relativeInfo.Gender,
                        DateOfBirth = DateTime.TryParse(relativeInfo.DateOfBirth, out var dob)
                            ? dob
                            : null,
                        Age = DateTime.TryParse(relativeInfo.DateOfBirth, out var dobAge)
                            ? (int)((DateTime.Today - dobAge).TotalDays / 365.25)
                            : null,
                        Phone = relativeInfo.Phone,
                        Relationship = relativeInfo.Relationship,
                        RelationshipDisplay = relativeInfo.RelationshipDisplay,
                    };
                }
            }
        }
        catch (GrpcCore.RpcException rpcEx)
        {
            LogWarning(
                "gRPC error batch fetching relatives for {Context}: {Error}",
                null,
                context,
                rpcEx.Status.Detail
            );
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
        Guid? serviceId
    )
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
                var cacheKey = CacheKeys.Format(
                    CacheKeys.AvailableSlots,
                    doctorId,
                    dateStr,
                    serviceIdStr
                );
                // Add Schedule Service prefix to match where cache was created
                var fullCacheKey = $"BookingCare:Schedule:{cacheKey}";
                await RemoveCacheDirectlyAsync(fullCacheKey);
                LogInfo(
                    "Invalidated available slots cache for doctor {DoctorId} on {Date} with serviceId {ServiceId}",
                    null,
                    doctorId,
                    dateStr,
                    serviceIdStr
                );
            }

            // Invalidate cache without service filter (serviceId = "null")
            var nullServiceCacheKey = CacheKeys.Format(
                CacheKeys.AvailableSlots,
                doctorId,
                dateStr,
                "null"
            );
            var fullNullCacheKey = $"BookingCare:Schedule:{nullServiceCacheKey}";
            await RemoveCacheDirectlyAsync(fullNullCacheKey);

            // Invalidate all variations using pattern matching
            var patternCacheKey = CacheKeys.Format(
                CacheKeys.AvailableSlots,
                doctorId,
                dateStr,
                "*"
            );
            var fullPatternCacheKey = $"BookingCare:Schedule:{patternCacheKey}";
            await RemoveCacheByPatternDirectlyAsync(fullPatternCacheKey);

            LogInfo(
                "Successfully invalidated available slots cache for doctor {DoctorId} on {Date}",
                null,
                doctorId,
                dateStr
            );
        }
        catch (Exception ex)
        {
            // Log error but don't throw - cache invalidation failure should not fail appointment operations
            LogError(
                ex,
                "Failed to invalidate available slots cache for doctor {DoctorId} on {Date}: {Error}",
                null,
                doctorId,
                DateOnly.FromDateTime(appointmentDate).ToString(DateFormat),
                ex.Message
            );
        }
    }

    /// <summary>
    /// Invalidate service medical available slots cache
    /// IMPORTANT: Available slots cache is stored by Schedule Service with prefix "BookingCare:Schedule:"
    /// </summary>
    private async Task InvalidateServiceMedicalSlotsCacheAsync(
        Guid serviceMedicalId,
        DateTime appointmentDate
    )
    {
        try
        {
            var dateStr = DateOnly.FromDateTime(appointmentDate).ToString(DateFormat);

            // Cache key format: service_medical_available_slots:{serviceMedicalId}:{date}
            var cacheKey = CacheKeys.Format(
                CacheKeys.ServiceMedicalAvailableSlots,
                serviceMedicalId,
                dateStr
            );

            // Add Schedule Service prefix to match where cache was created
            var fullCacheKey = $"BookingCare:Schedule:{cacheKey}";
            await RemoveCacheDirectlyAsync(fullCacheKey);

            LogInfo(
                "Successfully invalidated service medical available slots cache for service {ServiceMedicalId} on {Date}",
                null,
                serviceMedicalId,
                dateStr
            );
        }
        catch (Exception ex)
        {
            // Log error but don't throw - cache invalidation failure should not fail appointment operations
            LogError(
                ex,
                "Failed to invalidate service medical available slots cache for service {ServiceMedicalId} on {Date}: {Error}",
                null,
                serviceMedicalId,
                DateOnly.FromDateTime(appointmentDate).ToString(DateFormat),
                ex.Message
            );
        }
    }

    /// <summary>
    /// Invalidate specialty available slots cache (for "hospital assigns doctor" mode)
    /// IMPORTANT: Available slots cache is stored by Schedule Service with prefix "BookingCare:Schedule:"
    /// </summary>
    private async Task InvalidateSpecialtySlotsCacheAsync(
        Guid hospitalId,
        Guid specialtyId,
        DateTime appointmentDate,
        AppointmentType appointmentType
    )
    {
        try
        {
            var dateStr = DateOnly.FromDateTime(appointmentDate).ToString(DateFormat);

            // Cache key format: specialty_available_slots:{hospitalId}:{specialtyId}:{date}:{appointmentType}
            var cacheKey = CacheKeys.Format(
                CacheKeys.SpecialtyAvailableSlots,
                hospitalId,
                specialtyId,
                dateStr,
                appointmentType.ToString()
            );

            // Add Schedule Service prefix to match where cache was created
            var fullCacheKey = $"BookingCare:Schedule:{cacheKey}";
            await RemoveCacheDirectlyAsync(fullCacheKey);

            // Also invalidate with pattern to cover all appointment types
            var patternCacheKey = CacheKeys.Format(
                CacheKeys.SpecialtyAvailableSlots,
                hospitalId,
                specialtyId,
                dateStr,
                "*"
            );
            var fullPatternCacheKey = $"BookingCare:Schedule:{patternCacheKey}";
            await RemoveCacheByPatternDirectlyAsync(fullPatternCacheKey);

            LogInfo(
                "Successfully invalidated specialty available slots cache for hospital {HospitalId}, specialty {SpecialtyId} on {Date}",
                null,
                hospitalId,
                specialtyId,
                dateStr
            );
        }
        catch (Exception ex)
        {
            // Log error but don't throw - cache invalidation failure should not fail appointment operations
            LogError(
                ex,
                "Failed to invalidate specialty available slots cache for hospital {HospitalId}, specialty {SpecialtyId} on {Date}: {Error}",
                null,
                hospitalId,
                specialtyId,
                DateOnly.FromDateTime(appointmentDate).ToString(DateFormat),
                ex.Message
            );
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

            LogInfo(
                "Directly removed {Count} cache keys matching pattern: {Pattern}",
                null,
                keys.Length,
                fullPattern
            );
        }
        catch (Exception ex)
        {
            LogError(ex, "Error directly removing cache by pattern: {Pattern}", null, fullPattern);
        }
    }

    /// <summary>
    /// Get role-based status counts from query parameters
    /// Determines the appropriate userId and hospitalId based on management role
    /// Passes all filters to ensure counts match the filtered results
    /// </summary>
    private async Task<AppointmentStatusCounts> GetRoleBasedStatusCountsAsync(
        AppointmentQueryRequest query,
        Role managementRole
    )
    {
        switch (managementRole)
        {
            case Role.DOCTOR:
                // Doctor: count by DoctorId from query with filters
                if (query.DoctorId.HasValue)
                {
                    return await GetStatusCountsAsync(
                        new StatusCountsRequest
                        {
                            UserId = query.DoctorId.Value,
                            Role = Role.DOCTOR,
                            FromDate = query.FromDate,
                            ToDate = query.ToDate,
                            AppointmentType = query.AppointmentType,
                            ForRelative = query.ForRelative,
                            SearchTerm = query.SearchTerm,
                        }
                    );
                }
                LogWarning("Doctor role but no DoctorId provided in query for status counts", null);
                return new AppointmentStatusCounts();

            case Role.STAFF:
                // Staff: count by HospitalId from query with filters
                if (query.HospitalId.HasValue)
                {
                    return await GetStatusCountsAsync(
                        new StatusCountsRequest
                        {
                            UserId = null,
                            Role = Role.STAFF,
                            HospitalId = query.HospitalId.Value,
                            FromDate = query.FromDate,
                            ToDate = query.ToDate,
                            AppointmentType = query.AppointmentType,
                            ForRelative = query.ForRelative,
                            SearchTerm = query.SearchTerm,
                        }
                    );
                }
                LogWarning(
                    "Staff role but no HospitalId provided in query for status counts",
                    null
                );
                return new AppointmentStatusCounts();

            case Role.ADMIN:
                // Admin: count all appointments with filters
                return await GetStatusCountsAsync(
                    new StatusCountsRequest
                    {
                        UserId = null,
                        Role = Role.ADMIN,
                        FromDate = query.FromDate,
                        ToDate = query.ToDate,
                        AppointmentType = query.AppointmentType,
                        ForRelative = query.ForRelative,
                        SearchTerm = query.SearchTerm,
                    }
                );

            default:
                LogWarning(
                    "Unknown management role {Role} for status counts",
                    null,
                    managementRole
                );
                return new AppointmentStatusCounts();
        }
    }

    /// <summary>
    /// Request object for status counts to keep method signature focused.
    /// </summary>
    private sealed class StatusCountsRequest
    {
        public Guid? UserId { get; init; }
        public Role Role { get; init; }
        public Guid? HospitalId { get; init; }
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }
        public AppointmentType? AppointmentType { get; init; }
        public bool? ForRelative { get; init; }
        public string? SearchTerm { get; init; }
    }

    /// <summary>
    /// Get counts for all statuses for a specific user or organization
    /// Uses optimized repository method with single DB query
    /// Supports Patient, Doctor, Staff (by Hospital), and Admin (all) roles
    /// Also supports additional filters like date range, appointment type, forRelative, and searchTerm
    /// </summary>
    private async Task<AppointmentStatusCounts> GetStatusCountsAsync(StatusCountsRequest request)
    {
        try
        {
            // Determine parameters based on role
            Guid? patientId = null;
            Guid? doctorId = null;
            Guid? staffHospitalId = null;
            bool countAll = false;

            switch (request.Role)
            {
                case Role.PATIENT:
                    patientId = request.UserId;
                    LogInfo("Getting status counts for Patient {PatientId}", null, patientId!);
                    break;

                case Role.DOCTOR:
                    doctorId = request.UserId;
                    LogInfo("Getting status counts for Doctor {DoctorId}", null, doctorId!);
                    break;

                case Role.STAFF:
                    staffHospitalId = request.HospitalId;
                    LogInfo(
                        "Getting status counts for Staff in Hospital {HospitalId}",
                        null,
                        staffHospitalId!
                    );
                    break;

                case Role.ADMIN:
                    countAll = true;
                    LogInfo("Getting status counts for Admin (all appointments)", null);
                    break;

                default:
                    LogWarning("Unknown role {Role} for status counts", null, request.Role);
                    return new AppointmentStatusCounts();
            }

            // Get counts using optimized repository method (single query with GROUP BY)
            var filter = new AppointmentStatusFilter
            {
                PatientId = patientId,
                DoctorId = doctorId,
                HospitalId = staffHospitalId,
                CountAll = countAll,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                AppointmentType = request.AppointmentType,
                ForRelative = request.ForRelative,
                SearchTerm = request.SearchTerm,
            };

            var statusCountsDict = await _appointmentRepository.GetStatusCountsByUserAsync(filter);

            var counts = new AppointmentStatusCounts
            {
                Pending = statusCountsDict.GetValueOrDefault(AppointmentStatus.PENDING, 0),
                Confirmed = statusCountsDict.GetValueOrDefault(AppointmentStatus.CONFIRMED, 0),
                Cancelled = statusCountsDict.GetValueOrDefault(AppointmentStatus.CANCELLED, 0),
                Completed = statusCountsDict.GetValueOrDefault(AppointmentStatus.COMPLETED, 0),
            };

            counts.Total = counts.Pending + counts.Confirmed + counts.Cancelled + counts.Completed;

            LogInfo(
                "Retrieved status counts for role {Role}: Total={Total}, Pending={Pending}, Confirmed={Confirmed}, Cancelled={Cancelled}, Completed={Completed}",
                null,
                request.Role,
                counts.Total,
                counts.Pending,
                counts.Confirmed,
                counts.Cancelled,
                counts.Completed
            );

            return counts;
        }
        catch (Exception ex)
        {
            LogError(
                ex,
                "Failed to get status counts for role {Role}: {Error}",
                null,
                request.Role,
                ex.Message
            );
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
        bool checkAvailability = true
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Fetching doctors via gRPC for hospital {HospitalId}, specialty {SpecialtyId}, checkAvailability {CheckAvailability}",
                    null,
                    hospitalId,
                    specialtyId,
                    checkAvailability
                );

                // Step 1: Fetch doctors from Doctor Service
                var grpcResponse = await FetchDoctorsFromGrpcAsync(hospitalId, specialtyId);
                if (grpcResponse.Doctors == null || grpcResponse.Doctors.Count == 0)
                {
                    LogInfo(
                        "No doctors found for hospital {HospitalId}, specialty {SpecialtyId}",
                        null,
                        hospitalId,
                        specialtyId
                    );
                    return new AvailableDoctorsResponse();
                }

                // Step 2: Filter doctors based on availability
                var availableDoctors = await FilterDoctorsByAvailabilityAsync(
                    grpcResponse.Doctors,
                    checkAvailability,
                    appointmentDate,
                    appointmentTimeId
                );

                LogInfo(
                    "Found {Count} doctors out of {Total} for hospital {HospitalId}, specialty {SpecialtyId} (checkAvailability: {CheckAvailability})",
                    null,
                    availableDoctors.Count,
                    grpcResponse.Doctors?.Count ?? 0,
                    hospitalId,
                    specialtyId,
                    checkAvailability
                );

                return new AvailableDoctorsResponse
                {
                    Doctors = availableDoctors,
                    TotalCount = availableDoctors.Count,
                };
            },
            "GetAvailableDoctors"
        );
    }

    public async Task<StaffHospitalStatisticsResponse> GetHospitalStaffStatisticsAsync(
        StaffHospitalStatisticsRequest request
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                ValidateRequired(request, nameof(request));
                ValidateGuid(request.HospitalId, nameof(request.HospitalId));

                var fromDate = request.GetFromDate();
                var toDate = request.GetToDate();

                if (fromDate > toDate)
                {
                    throw new AppointmentException(
                        "FromDate must be earlier than or equal to ToDate"
                    );
                }

                LogInfo(
                    "Generating staff statistics for hospital {HospitalId} from {FromDate} to {ToDate} ({Period})",
                    null,
                    request.HospitalId,
                    fromDate,
                    toDate,
                    request.Period
                );

                var appointments = await _appointmentRepository.GetAppointmentsForHospitalAsync(
                    request.HospitalId,
                    fromDate,
                    toDate
                );

                var response = StaffHospitalStatisticsResponse.CreateEmpty(
                    request.HospitalId,
                    fromDate,
                    toDate,
                    request.Period
                );

                if (!appointments.Any())
                {
                    LogInfo(
                        "No appointments found for hospital {HospitalId} in provided range",
                        null,
                        request.HospitalId
                    );
                    return response;
                }

                var patientFirstAppointments =
                    await _appointmentRepository.GetPatientFirstAppointmentsAsync(
                        request.HospitalId
                    );

                response.Overview = BuildHospitalOverview(
                    appointments,
                    patientFirstAppointments,
                    fromDate,
                    toDate
                );
                response.AppointmentTrend = BuildAppointmentTrend(
                    appointments,
                    fromDate,
                    toDate,
                    request.Period
                );
                response.NewPatientTrend = BuildNewPatientTrend(
                    patientFirstAppointments,
                    fromDate,
                    toDate,
                    request.Period
                );

                return response;
            },
            "GetHospitalStaffStatistics"
        );
    }

    private static HospitalAppointmentOverview BuildHospitalOverview(
        List<AppointmentEntity> appointments,
        Dictionary<Guid, DateTime> patientFirstAppointments,
        DateTime fromDate,
        DateTime toDate
    )
    {
        var total = appointments.Count;
        var completed = appointments.Count(a => a.Status == AppointmentStatus.COMPLETED);
        var confirmed = appointments.Count(a => a.Status == AppointmentStatus.CONFIRMED);
        var pending = appointments.Count(a => a.Status == AppointmentStatus.PENDING);
        var cancelled = appointments.Count(a => a.Status == AppointmentStatus.CANCELLED);
        var rescheduled = appointments.Count(a => a.IsRescheduled);

        var newPatients = patientFirstAppointments.Count(kvp =>
            kvp.Value >= fromDate && kvp.Value <= toDate
        );

        return new HospitalAppointmentOverview
        {
            TotalAppointments = total,
            CompletedAppointments = completed,
            ConfirmedAppointments = confirmed,
            PendingAppointments = pending,
            CancelledAppointments = cancelled,
            RescheduledAppointments = rescheduled,
            NewPatients = newPatients,
            NoShowRate = CalculateRate(cancelled, total),
            RescheduleRate = CalculateRate(rescheduled, total),
        };
    }

    private static List<AppointmentTrendPoint> BuildAppointmentTrend(
        List<AppointmentEntity> appointments,
        DateTime fromDate,
        DateTime toDate,
        StatisticsPeriod period
    )
    {
        var results = new List<AppointmentTrendPoint>();
        var cursor = AlignToPeriodStart(fromDate.Date, period);
        var endDate = toDate.Date;

        while (cursor <= endDate)
        {
            var (periodStart, periodEnd, label) = StatisticsPeriodHelper.GetPeriodBounds(
                cursor,
                period
            );

            var periodAppointments = appointments
                .Where(a =>
                    a.AppointmentDate.Date >= periodStart.Date
                    && a.AppointmentDate.Date <= periodEnd.Date
                )
                .ToList();

            results.Add(
                new AppointmentTrendPoint
                {
                    Label = label,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    TotalAppointments = periodAppointments.Count,
                    CompletedAppointments = periodAppointments.Count(a =>
                        a.Status == AppointmentStatus.COMPLETED
                    ),
                    CancelledAppointments = periodAppointments.Count(a =>
                        a.Status == AppointmentStatus.CANCELLED
                    ),
                    RescheduledAppointments = periodAppointments.Count(a => a.IsRescheduled),
                }
            );

            cursor = StatisticsPeriodHelper.GetNextPeriod(periodStart, period);
        }

        return results;
    }

    private static List<NewPatientTrendPoint> BuildNewPatientTrend(
        Dictionary<Guid, DateTime> patientFirstAppointments,
        DateTime fromDate,
        DateTime toDate,
        StatisticsPeriod period
    )
    {
        var filteredFirstAppointments = patientFirstAppointments
            .Where(kvp => kvp.Value >= fromDate && kvp.Value <= toDate)
            .Select(kvp => kvp.Value)
            .ToList();

        var results = new List<NewPatientTrendPoint>();
        var cursor = AlignToPeriodStart(fromDate.Date, period);
        var endDate = toDate.Date;

        while (cursor <= endDate)
        {
            var (periodStart, periodEnd, label) = StatisticsPeriodHelper.GetPeriodBounds(
                cursor,
                period
            );

            var newPatients = filteredFirstAppointments.Count(date =>
                date.Date >= periodStart.Date && date.Date <= periodEnd.Date
            );

            results.Add(
                new NewPatientTrendPoint
                {
                    Label = label,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    NewPatients = newPatients,
                }
            );

            cursor = StatisticsPeriodHelper.GetNextPeriod(periodStart, period);
        }

        return results;
    }

    private static decimal CalculateRate(int numerator, int denominator)
    {
        if (denominator == 0)
        {
            return 0;
        }

        return Math.Round((decimal)numerator / denominator * 100, 2, MidpointRounding.AwayFromZero);
    }

    private static DateTime AlignToPeriodStart(DateTime date, StatisticsPeriod period)
    {
        var dateKind = date.Kind != DateTimeKind.Unspecified ? date.Kind : DateTimeKind.Utc;
        return period switch
        {
            StatisticsPeriod.Daily => date,
            StatisticsPeriod.Weekly => date.AddDays(-(int)date.DayOfWeek).Date,
            StatisticsPeriod.Monthly => new DateTime(date.Year, date.Month, 1, 0, 0, 0, dateKind),
            StatisticsPeriod.Quarterly => new DateTime(
                date.Year,
                ((date.Month - 1) / 3) * 3 + 1,
                1,
                0,
                0,
                0,
                dateKind
            ),
            StatisticsPeriod.Yearly => new DateTime(date.Year, 1, 1, 0, 0, 0, dateKind),
            _ => date,
        };
    }

    /// <summary>
    /// Fetch doctors from gRPC Doctor Service
    /// </summary>
    private async Task<GetAvailableDoctorsResponse> FetchDoctorsFromGrpcAsync(
        Guid hospitalId,
        Guid specialtyId
    )
    {
        var grpcRequest = new GetAvailableDoctorsRequest
        {
            HospitalId = hospitalId.ToString(),
            SpecialtyId = specialtyId.ToString(),
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
        AppointmentTime? appointmentTimeId
    )
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
                    doctorId,
                    appointmentDate.Value,
                    appointmentTimeId.Value
                );

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
            YearsOfExperience = doctor.YearsOfExperience,
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Validate appointment exists, is cancelled, and has valid reschedule token
    /// </summary>
    private async Task<AppointmentEntity> ValidateRescheduleEligibilityAsync(
        Guid appointmentId,
        string rescheduleToken,
        string errorContext
    )
    {
        // Get and validate appointment
        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
        if (appointment == null)
        {
            throw new AppointmentNotFoundException(appointmentId);
        }

        // Validate appointment status:
        // - CANCELLED (old flow: staff cancels and offers reschedule)
        // - PENDING/CONFIRMED with PendingRescheduleAction (new flow: patient initiates reschedule without cancelling)
        var isValidStatus =
            appointment.Status == AppointmentStatus.CANCELLED
            || (
                (
                    appointment.Status == AppointmentStatus.PENDING
                    || appointment.Status == AppointmentStatus.CONFIRMED
                ) && !string.IsNullOrEmpty(appointment.PendingRescheduleAction)
            );

        if (!isValidStatus)
        {
            throw new AppointmentException(
                $"Only cancelled or pending reschedule appointments can {errorContext}"
            );
        }

        // Validate reschedule token
        if (
            string.IsNullOrEmpty(appointment.RescheduleToken)
            || appointment.RescheduleToken != rescheduleToken
            || appointment.RescheduleTokenExpiry == null
            || appointment.RescheduleTokenExpiry < DateTime.UtcNow
        )
        {
            throw new AppointmentException("Invalid or expired reschedule token");
        }

        return appointment;
    }

    #endregion

    #region Assign Doctor To Appointment (NEW flow for "Hospital assigns doctor")

    /// <summary>
    /// Get doctors for assignment flow (hospital staff assigns doctor to pending specialty appointment)
    /// Returns recommended doctors (sorted by experience, rating, booking count) and previous doctors
    /// </summary>
    public async Task<DoctorsForAssignmentResponse> GetDoctorsForAssignmentAsync(
        Models.DTOs.GetDoctorsForAssignmentRequest request
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                ValidateRequired(request, nameof(request));
                ValidateGuid(request.AppointmentId, nameof(request.AppointmentId));

                var appointment = await GetAndValidateAppointmentForAssignmentAsync(
                    request.AppointmentId
                );
                var (hospitalId, specialtyId, appointmentType) = GetAssignmentContext(
                    appointment,
                    request.AppointmentId
                );

                var doctorGrpcResponse = await FetchRecommendedDoctorsFromGrpcAsync(
                    hospitalId,
                    specialtyId,
                    appointmentType
                );

                if (!doctorGrpcResponse.Success)
                {
                    LogWarning(
                        "[GetDoctorsForAssignment] Doctor gRPC failed: {Message}",
                        null,
                        doctorGrpcResponse.Message
                    );
                    return new DoctorsForAssignmentResponse();
                }

                var previousDoctorIds = await GetPreviousDoctorIdsForAppointmentAsync(
                    appointment,
                    hospitalId,
                    specialtyId
                );

                var allDoctorIds = doctorGrpcResponse
                    .Doctors.Select(d => Guid.Parse(d.Id))
                    .Union(previousDoctorIds)
                    .Distinct()
                    .ToList();

                var bookingCountMap = await GetDoctorBookingCountsAsync(allDoctorIds);

                var availabilityMap = await BuildAvailabilityMapAsync(
                    request.CheckAvailabilityAtOriginalTime,
                    allDoctorIds,
                    appointment
                );

                var recommendedDoctors = BuildRecommendedDoctors(
                    doctorGrpcResponse,
                    bookingCountMap,
                    availabilityMap
                );

                var previousDoctors = await BuildPreviousDoctorsAsync(
                    previousDoctorIds,
                    hospitalId,
                    specialtyId,
                    appointmentType,
                    bookingCountMap,
                    availabilityMap
                );

                LogInfo(
                    "[GetDoctorsForAssignment] Found {RecommendedCount} recommended and {PreviousCount} previous doctors",
                    null,
                    recommendedDoctors.Count,
                    previousDoctors.Count
                );

                return new DoctorsForAssignmentResponse
                {
                    RecommendedDoctors = recommendedDoctors,
                    PreviousDoctors = previousDoctors,
                    TotalRecommended = recommendedDoctors.Count,
                    TotalPrevious = previousDoctors.Count,
                };
            },
            "GetDoctorsForAssignment"
        );
    }

    private async Task<AppointmentEntity> GetAndValidateAppointmentForAssignmentAsync(
        Guid appointmentId
    )
    {
        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
        if (appointment == null)
        {
            throw new AppointmentException($"Appointment {appointmentId} not found");
        }

        if (appointment.Status != AppointmentStatus.PENDING)
        {
            throw new AppointmentException("Only PENDING appointments can have doctors assigned");
        }

        if (appointment.DoctorId.HasValue)
        {
            throw new AppointmentException("This appointment already has a doctor assigned");
        }

        if (!appointment.SpecialtyId.HasValue || !appointment.HospitalId.HasValue)
        {
            throw new AppointmentException(
                "Appointment must have specialty and hospital information"
            );
        }

        return appointment;
    }

    private (Guid HospitalId, Guid SpecialtyId, string AppointmentTypeText) GetAssignmentContext(
        AppointmentEntity appointment,
        Guid appointmentIdForLog
    )
    {
        var hospitalId = appointment.HospitalId!.Value;
        var specialtyId = appointment.SpecialtyId!.Value;
        var appointmentTypeText =
            appointment.AppointmentType == AppointmentType.IN_PERSON
                ? "Khám trực tiếp"
                : "Tư vấn trực tuyến";

        LogInfo(
            "[GetDoctorsForAssignment] Fetching doctors for appointment {AppointmentId}, hospital {HospitalId}, specialty {SpecialtyId}, type {AppointmentType}",
            null,
            appointmentIdForLog,
            hospitalId,
            specialtyId,
            appointmentTypeText
        );

        return (hospitalId, specialtyId, appointmentTypeText);
    }

    private async Task<GetDoctorsForAssignmentResponse> FetchRecommendedDoctorsFromGrpcAsync(
        Guid hospitalId,
        Guid specialtyId,
        string appointmentType
    )
    {
        var doctorGrpcRequest = new Doctor.Protos.GetDoctorsForAssignmentRequest
        {
            HospitalId = hospitalId.ToString(),
            SpecialtyId = specialtyId.ToString(),
            AppointmentType = appointmentType,
        };

        return await _grpcClients.DoctorClient.GetDoctorsForAssignmentAsync(doctorGrpcRequest);
    }

    private async Task<List<Guid>> GetPreviousDoctorIdsForAppointmentAsync(
        AppointmentEntity appointment,
        Guid hospitalId,
        Guid specialtyId
    )
    {
        var actualPatientId = appointment.RelativeId ?? appointment.PatientId;
        return await GetPreviousDoctorIdsForPatientAsync(actualPatientId, hospitalId, specialtyId);
    }

    private async Task<Dictionary<Guid, bool>> BuildAvailabilityMapAsync(
        bool checkAvailabilityAtOriginalTime,
        List<Guid> allDoctorIds,
        AppointmentEntity appointment
    )
    {
        if (!checkAvailabilityAtOriginalTime)
        {
            return new Dictionary<Guid, bool>();
        }

        return await CheckDoctorsAvailabilityAsync(
            allDoctorIds,
            DateOnly.FromDateTime(appointment.AppointmentDate),
            appointment.AppointmentTimeId
        );
    }

    private static List<DoctorForAssignment> BuildRecommendedDoctors(
        GetDoctorsForAssignmentResponse doctorGrpcResponse,
        IReadOnlyDictionary<Guid, int> bookingCountMap,
        IReadOnlyDictionary<Guid, bool> availabilityMap
    )
    {
        return MapGrpcDoctorsToAssignment(
                doctorGrpcResponse.Doctors,
                bookingCountMap,
                availabilityMap
            )
            .OrderByDescending(d => d.YearsOfExperience)
            .ThenByDescending(d => d.Rating)
            .ThenByDescending(d => d.BookingCount)
            .ToList();
    }

    private async Task<List<DoctorForAssignment>> BuildPreviousDoctorsAsync(
        IReadOnlyCollection<Guid> previousDoctorIds,
        Guid hospitalId,
        Guid specialtyId,
        string appointmentType,
        IReadOnlyDictionary<Guid, int> bookingCountMap,
        IReadOnlyDictionary<Guid, bool> availabilityMap
    )
    {
        var previousDoctors = new List<DoctorForAssignment>();
        if (!previousDoctorIds.Any())
        {
            return previousDoctors;
        }

        var previousDoctorGrpcRequest = new Doctor.Protos.GetDoctorsForAssignmentRequest
        {
            HospitalId = hospitalId.ToString(),
            SpecialtyId = specialtyId.ToString(),
            AppointmentType = appointmentType,
        };
        previousDoctorGrpcRequest.DoctorIds.AddRange(previousDoctorIds.Select(id => id.ToString()));

        var previousDoctorGrpcResponse =
            await _grpcClients.DoctorClient.GetDoctorsForAssignmentAsync(previousDoctorGrpcRequest);

        if (previousDoctorGrpcResponse.Success)
        {
            previousDoctors = MapGrpcDoctorsToAssignment(
                previousDoctorGrpcResponse.Doctors,
                bookingCountMap,
                availabilityMap
            );
        }

        return previousDoctors;
    }

    /// <summary>
    /// Assign doctor to a pending specialty appointment (NEW flow for "Hospital assigns doctor")
    /// This directly assigns the doctor and confirms the appointment
    /// </summary>
    public async Task<AssignDoctorToAppointmentResponse> AssignDoctorToAppointmentAsync(
        AssignDoctorToAppointmentRequest request
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                ValidateRequired(request, nameof(request));
                ValidateGuid(request.AppointmentId, nameof(request.AppointmentId));
                ValidateGuid(request.DoctorId, nameof(request.DoctorId));
                ValidateGuid(request.AssignedByStaffId, nameof(request.AssignedByStaffId));

                // Step 1: Get and validate appointment
                var appointment = await _appointmentRepository.GetAppointmentByIdAsync(
                    request.AppointmentId
                );
                if (appointment == null)
                {
                    throw new AppointmentException(
                        $"Appointment {request.AppointmentId} not found"
                    );
                }

                // Validate: Must be PENDING and specialty booking (no doctor assigned)
                if (appointment.Status != AppointmentStatus.PENDING)
                {
                    throw new AppointmentException(
                        "Only PENDING appointments can have doctors assigned"
                    );
                }

                if (appointment.DoctorId.HasValue)
                {
                    throw new AppointmentException(
                        "This appointment already has a doctor assigned"
                    );
                }

                // Step 2: Get doctor info via gRPC
                var doctorInfo = await GetDoctorBasicInfoAsync(request.DoctorId);
                if (doctorInfo == null)
                {
                    throw new AppointmentException($"Doctor {request.DoctorId} not found");
                }

                // Step 3: Determine final date/time
                var finalDate = request.NewAppointmentDate ?? appointment.AppointmentDate;
                var finalTimeId = request.NewAppointmentTimeId ?? appointment.AppointmentTimeId;

                // Step 4: Check doctor availability at final date/time
                var isAvailable = await CheckDoctorAvailabilityAsync(
                    request.DoctorId,
                    DateOnly.FromDateTime(finalDate),
                    finalTimeId
                );

                if (!isAvailable)
                {
                    throw new AppointmentException(
                        $"Doctor {doctorInfo.FullName} is not available at the selected date/time"
                    );
                }

                // Step 5: Update appointment
                appointment.DoctorId = request.DoctorId;
                appointment.AppointmentDate = finalDate;
                appointment.AppointmentTimeId = finalTimeId;
                appointment.Status = AppointmentStatus.CONFIRMED;

                await _appointmentRepository.UpdateAppointmentAsync(appointment);

                LogInfo(
                    "[AssignDoctorToAppointment] Assigned doctor {DoctorId} to appointment {AppointmentId}, status changed to CONFIRMED",
                    null,
                    request.DoctorId,
                    request.AppointmentId
                );

                // Step 6: Publish notification event to patient
                await PublishDoctorAssignedNotificationEventAsync(
                    appointment,
                    doctorInfo,
                    request.StaffNote
                );

                // Step 7: Get time string for response
                var (startTime, endTime) = GetTimeStringsFromAppointmentTime(finalTimeId);

                return new AssignDoctorToAppointmentResponse
                {
                    Success = true,
                    AppointmentId = appointment.Id,
                    DoctorId = request.DoctorId,
                    DoctorName = doctorInfo.FullName ?? string.Empty,
                    AppointmentDate = finalDate,
                    AppointmentTime = $"{startTime} - {endTime}",
                    Message = "Doctor assigned successfully. Patient will be notified.",
                };
            },
            "AssignDoctorToAppointment"
        );
    }

    /// <summary>
    /// Get previous doctor IDs who have treated this patient (completed appointments)
    /// </summary>
    private async Task<List<Guid>> GetPreviousDoctorIdsForPatientAsync(
        Guid patientId,
        Guid hospitalId,
        Guid specialtyId
    )
    {
        try
        {
            var completedAppointments =
                await _appointmentRepository.GetCompletedAppointmentsForPatientAsync(
                    patientId,
                    hospitalId,
                    specialtyId
                );

            return completedAppointments
                .Where(a => a.DoctorId.HasValue)
                .Select(a => a.DoctorId!.Value)
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            LogWarning("[GetPreviousDoctorIdsForPatient] Error: {Error}", null, ex.Message);
            return new List<Guid>();
        }
    }

    /// <summary>
    /// Get booking counts for doctors (completed appointments)
    /// </summary>
    private async Task<Dictionary<Guid, int>> GetDoctorBookingCountsAsync(List<Guid> doctorIds)
    {
        try
        {
            return await _appointmentRepository.GetDoctorBookingCountsAsync(doctorIds);
        }
        catch (Exception ex)
        {
            LogWarning("[GetDoctorBookingCounts] Error: {Error}", null, ex.Message);
            return new Dictionary<Guid, int>();
        }
    }

    /// <summary>
    /// Check availability for multiple doctors at a specific date/time
    /// </summary>
    private async Task<Dictionary<Guid, bool>> CheckDoctorsAvailabilityAsync(
        List<Guid> doctorIds,
        DateOnly date,
        AppointmentTime appointmentTimeId
    )
    {
        var result = new Dictionary<Guid, bool>();

        try
        {
            // Call Schedule Service to check working slots
            var scheduleRequest = new Schedule.Protos.CheckDoctorsWorkingSlotRequest
            {
                Date = date.ToString("yyyy-MM-dd"),
                AppointmentTimeId = appointmentTimeId.ToString(),
            };
            scheduleRequest.DoctorIds.AddRange(doctorIds.Select(id => id.ToString()));

            var scheduleResponse = await _grpcClients.ScheduleClient.CheckDoctorsWorkingSlotAsync(
                scheduleRequest
            );

            if (scheduleResponse.Success)
            {
                foreach (var status in scheduleResponse.DoctorStatuses)
                {
                    if (Guid.TryParse(status.DoctorId, out var doctorId))
                    {
                        result[doctorId] = status.IsWorking;
                    }
                }
            }

            // Also check for existing appointments (booked slots)
            var bookedDoctorIds = await _appointmentRepository.GetDoctorsWithBookedSlotAsync(
                doctorIds,
                date,
                appointmentTimeId
            );

            foreach (var doctorId in bookedDoctorIds)
            {
                result[doctorId] = false; // Not available if already booked
            }
        }
        catch (Exception ex)
        {
            LogWarning("[CheckDoctorsAvailability] Error: {Error}", null, ex.Message);
            // Return all as available on error (fail open)
            foreach (var doctorId in doctorIds)
            {
                result.TryAdd(doctorId, true);
            }
        }

        return result;
    }

    /// <summary>
    /// Check availability for a single doctor at a specific date/time
    /// </summary>
    private async Task<bool> CheckDoctorAvailabilityAsync(
        Guid doctorId,
        DateOnly date,
        AppointmentTime appointmentTimeId
    )
    {
        var result = await CheckDoctorsAvailabilityAsync(
            new List<Guid> { doctorId },
            date,
            appointmentTimeId
        );
        return result.GetValueOrDefault(doctorId, true);
    }

    /// <summary>
    /// Publish notification event when doctor is assigned to appointment
    /// </summary>
    private async Task PublishDoctorAssignedNotificationEventAsync(
        AppointmentEntity appointment,
        BookingBasicInfo doctorInfo,
        string? staffNote
    )
    {
        try
        {
            // Get patient info
            var patientInfo = await GetPatientInfoForNotificationAsync(appointment.PatientId);
            if (patientInfo == null)
            {
                LogWarning(
                    "[PublishDoctorAssignedNotification] Patient info not found for {PatientId}",
                    null,
                    appointment.PatientId
                );
                return;
            }

            // Get hospital info
            var hospitalInfo = appointment.HospitalId.HasValue
                ? await GetHospitalInfoForNotificationAsync(appointment.HospitalId.Value)
                : null;

            var (startTime, endTime) = GetTimeStringsFromAppointmentTime(
                appointment.AppointmentTimeId
            );

            var notificationEvent = new DoctorAssignedToAppointmentNotificationEvent
            {
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                PatientAccountId = appointment.PatientAccountId,
                PatientEmail = patientInfo.Email ?? string.Empty,
                PatientPhone = patientInfo.Phone ?? string.Empty,
                PatientFullName = patientInfo.FullName ?? string.Empty,
                AssignedDoctorId = appointment.DoctorId ?? Guid.Empty,
                DoctorFullName = doctorInfo.FullName ?? string.Empty,
                DoctorSpecialty = doctorInfo.SpecialtyName ?? string.Empty,
                HospitalName = hospitalInfo?.Name ?? string.Empty,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = $"{startTime} - {endTime}",
                StaffNote = staffNote ?? string.Empty,
                AssignedAt = DateTime.UtcNow,
                AssignedByStaffId = Guid.Empty, // Will be set from request if needed
                ConfirmationUrl = string.Empty, // Not needed for direct assignment
                ConfirmationExpiry = DateTime.UtcNow.AddHours(48),
            };

            await _eventBus.PublishAsync(notificationEvent);

            LogInfo(
                "[PublishDoctorAssignedNotification] Published notification for appointment {AppointmentId}",
                null,
                appointment.Id
            );
        }
        catch (Exception ex)
        {
            // Don't fail the assignment if notification fails
            LogError(ex, "[PublishDoctorAssignedNotification] Failed to publish notification");
        }
    }

    /// <summary>
    /// Map gRPC doctor responses to DoctorForAssignment DTOs
    /// </summary>
    private static List<DoctorForAssignment> MapGrpcDoctorsToAssignment(
        IEnumerable<DoctorForAssignmentInfo> grpcDoctors,
        IReadOnlyDictionary<Guid, int> bookingCountMap,
        IReadOnlyDictionary<Guid, bool> availabilityMap
    )
    {
        return grpcDoctors
            .Select(d =>
            {
                var doctorId = Guid.Parse(d.Id);
                return new DoctorForAssignment
                {
                    Id = doctorId,
                    AccountId = Guid.Parse(d.AccountId),
                    FullName = d.FullName,
                    AvatarUrl = d.AvatarUrl,
                    PositionName = d.PositionName,
                    SpecialtyName = d.SpecialtyName,
                    YearsOfExperience = d.YearsOfExperience,
                    Rating = d.Rating,
                    ReviewCount = d.ReviewCount,
                    BookingCount = bookingCountMap.GetValueOrDefault(doctorId, 0),
                    ConsultationFee = (decimal)d.ConsultationFee,
                    IsActive = d.IsActive,
                    IsAvailableAtOriginalTime = availabilityMap.GetValueOrDefault(doctorId, true),
                };
            })
            .ToList();
    }

    /// <summary>
    /// Get time strings from AppointmentTime enum
    /// </summary>
    private static (string StartTime, string EndTime) GetTimeStringsFromAppointmentTime(
        AppointmentTime appointmentTime
    )
    {
        var name = appointmentTime.ToString();
        // Format: AT_08_00_08_30 -> "08:00", "08:30"
        if (name.StartsWith("AT_") && name.Length >= 14)
        {
            var parts = name.Substring(3).Split('_');
            if (parts.Length >= 4)
            {
                var startTime = $"{parts[0]}:{parts[1]}";
                var endTime = $"{parts[2]}:{parts[3]}";
                return (startTime, endTime);
            }
        }
        return ("Unknown", "Unknown");
    }

    #region Appointment Revenue Statistics

    /// <summary>
    /// Get appointment revenue statistics for hospital staff dashboard
    /// Shows revenue from COMPLETED appointments only
    /// If FromDate/ToDate not provided: default to last 6 months and monthly statistics
    /// </summary>
    public async Task<AppointmentStatisticsResponse> GetAppointmentStatisticsAsync(
        GetAppointmentStatisticsRequest request
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                // Use computed dates with default values
                var fromDate = request.GetFromDate();
                var toDate = request.GetToDate();

                LogInfo(
                    "Starting to get appointment revenue statistics - Period: {Period}, FromDate: {FromDate}, ToDate: {ToDate}, HospitalId: {HospitalId}",
                    null,
                    request.Period,
                    fromDate,
                    toDate,
                    request.HospitalId
                );

                // Validate required fields
                ValidateRequired(request, nameof(request));
                if (request.HospitalId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "HospitalId is required",
                        nameof(request.HospitalId)
                    );
                }

                // Query to get COMPLETED appointments for the hospital
                var query = new AppointmentQueryRequest
                {
                    HospitalId = request.HospitalId,
                    DoctorId = request.DoctorId,
                    Status = AppointmentStatus.COMPLETED,
                    FromDate = fromDate,
                    ToDate = toDate,
                    PageNumber = 1,
                    PageSize = int.MaxValue, // Get all completed appointments
                };

                // Use STAFF role to get all appointments for the hospital
                var (appointments, _) = await _appointmentRepository.GetAppointmentsAsync(
                    query,
                    Role.STAFF
                );

                // Apply specialty filter if needed (not supported by repository)
                var filteredAppointments = appointments;
                if (request.SpecialtyId.HasValue)
                {
                    filteredAppointments = appointments
                        .Where(a => a.SpecialtyId == request.SpecialtyId.Value)
                        .ToList();
                }

                LogInfo(
                    "Found {Count} completed appointments for statistics",
                    null,
                    filteredAppointments.Count
                );

                // Generate response with hospital revenue overview only
                var response = new AppointmentStatisticsResponse
                {
                    TimeSeries = GenerateAppointmentTimeSeries(
                        filteredAppointments,
                        fromDate,
                        toDate,
                        request.Period
                    ),
                    Summary = GenerateAppointmentSummaryStatistics(
                        filteredAppointments,
                        fromDate,
                        toDate
                    ),
                    Period = request.Period.ToString(),
                    DateRange = $"{fromDate:yyyy-MM-dd} - {toDate:yyyy-MM-dd}",
                };

                LogInfo(
                    "Appointment revenue statistics created successfully with {TimeSeriesCount} periods",
                    null,
                    response.TimeSeries.Count
                );

                return response;
            },
            "GetAppointmentStatistics"
        );
    }

    /// <summary>
    /// Generate time series data for appointment revenue
    /// </summary>
    private List<AppointmentTimeSeriesData> GenerateAppointmentTimeSeries(
        List<AppointmentEntity> appointments,
        DateTime fromDate,
        DateTime toDate,
        StatisticsPeriod period
    )
    {
        var result = new List<AppointmentTimeSeriesData>();
        var current = fromDate.Date;
        var endDate = toDate.Date;

        while (current <= endDate)
        {
            var (periodStart, periodEnd, timeLabel) = StatisticsPeriodHelper.GetPeriodBounds(
                current,
                period
            );

            var periodAppointments = appointments
                .Where(a => a.AppointmentDate >= periodStart && a.AppointmentDate <= periodEnd)
                .ToList();

            var totalRevenue = periodAppointments.Sum(a => a.Amount ?? 0);
            var count = periodAppointments.Count;

            result.Add(
                new AppointmentTimeSeriesData
                {
                    TimeLabel = timeLabel,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    TotalCount = count,
                    TotalRevenue = totalRevenue,
                    AverageRevenue = count > 0 ? totalRevenue / count : 0,
                }
            );

            current = StatisticsPeriodHelper.GetNextPeriod(current, period);
        }

        return result;
    }

    /// <summary>
    /// Generate summary statistics for appointments
    /// </summary>
    private AppointmentSummaryStatistics GenerateAppointmentSummaryStatistics(
        List<AppointmentEntity> appointments,
        DateTime fromDate,
        DateTime toDate
    )
    {
        var totalDays = (toDate - fromDate).TotalDays + 1;
        var totalRevenue = appointments.Sum(a => a.Amount ?? 0);

        return new AppointmentSummaryStatistics
        {
            TotalCompletedAppointments = appointments.Count,
            TotalRevenue = totalRevenue,
            AverageRevenuePerAppointment = appointments.Any()
                ? totalRevenue / appointments.Count
                : 0,
            MaxAppointmentAmount = appointments.Any() ? appointments.Max(a => a.Amount ?? 0) : 0,
            MinAppointmentAmount = appointments.Any() ? appointments.Min(a => a.Amount ?? 0) : 0,
            AverageAppointmentsPerDay = totalDays > 0 ? appointments.Count / (decimal)totalDays : 0,
            GrowthRate = 0, // Future enhancement: compare with previous period
        };
    }

    #endregion

    #endregion
}
