using AutoMapper;
using BookingCare.Services.Appointment.Exceptions;
using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Services.Appointment.Models.Entities;
using BookingCare.Services.Appointment.Repositories;
using BookingCare.Services.Appointment.Enums;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using BookingCare.Services.User.Protos;
using BookingCare.Shared.Common.Helpers;

namespace BookingCare.Services.Appointment.Services;

/// <summary>
/// Service implementation for Appointment service operations
/// </summary>
public class AppointmentService : BaseService, IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly DoctorService.DoctorServiceClient _doctorGrpcClient;
    private readonly HospitalService.HospitalServiceClient _hospitalGrpcClient;
    private readonly UserService.UserServiceClient _userGrpcClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IMapper mapper,
        IEventBus eventBus,
        DoctorService.DoctorServiceClient doctorGrpcClient,
        HospitalService.HospitalServiceClient hospitalGrpcClient,
        UserService.UserServiceClient userGrpcClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AppointmentService> logger) : base(logger)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
        _eventBus = eventBus;
        _doctorGrpcClient = doctorGrpcClient;
        _hospitalGrpcClient = hospitalGrpcClient;
        _userGrpcClient = userGrpcClient;
        _httpContextAccessor = httpContextAccessor;
    }

    #region Appointment Operations

    public async Task<bool> CreateAppointmentAsync(CreateAppointmentRequest request)
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

            LogInfo("Successfully created appointment {AppointmentId}", null, appointmentEntity.Id);
            return true;
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

                var hospitalsResponse = await _hospitalGrpcClient.GetHospitalsBasicInfoAsync(hospitalRequest);
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
            catch (Grpc.Core.RpcException rpcEx)
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

    public async Task<bool> CancelAppointmentAsync(CancelAppointmentRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Cancelling appointment {AppointmentId}", null, request.AppointmentId);

            // Get existing appointment - Single DB query
            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(request.AppointmentId);
            if (appointment == null)
            {
                throw new AppointmentNotFoundException(request.AppointmentId);
            }

            // Validate current status - can only cancel PENDING or CONFIRMED appointments
            if (appointment.Status != AppointmentStatus.PENDING &&
                appointment.Status != AppointmentStatus.CONFIRMED)
            {
                throw new AppointmentException(
                    $"Cannot cancel appointment with status {appointment.Status}. Only PENDING or CONFIRMED appointments can be cancelled.");
            }

            // Validate 24-hour rule - appointment must be at least 24 hours away
            var appointmentDateTime = appointment.AppointmentDate;
            var now = DateTime.UtcNow;
            var hoursUntilAppointment = (appointmentDateTime - now).TotalHours;

            if (hoursUntilAppointment < 24)
            {
                throw new AppointmentException(
                    $"Cannot cancel appointment less than 24 hours before the appointment time. " +
                    $"Appointment is scheduled for {appointmentDateTime:yyyy-MM-dd HH:mm} UTC " +
                    $"({hoursUntilAppointment:F1} hours from now).");
            }

            // Cancel appointment - Pass entity directly to avoid second DB query
            // Repository will handle status update, reason storage, and UpdatedAt timestamp
            var cancelled = await _appointmentRepository.CancelAppointmentAsync(
                appointment,
                request.CancellationReason);

            if (!cancelled)
            {
                throw new AppointmentException("Failed to cancel appointment");
            }

            // Publish integration event for downstream services (Payment & Notification)
            var cancelledEvent = new AppointmentCancelledIntegrationEvent
            {
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                HospitalId = appointment.HospitalId,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentType = (int)appointment.AppointmentType, // Convert enum to int to avoid coupling
                CancellationReason = request.CancellationReason,
                CancelledByStaffId = request.CancelledByStaffId,
                CancelledAt = DateTime.UtcNow
            };

            await _eventBus.PublishAsync(cancelledEvent);

            LogInfo("Successfully cancelled appointment {AppointmentId} and published event",
                null, request.AppointmentId);

            return true;
        }, "CancelAppointment");
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

            var doctorsResponse = await _doctorGrpcClient.GetDoctorsBasicInfoAsync(doctorRequest);
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
        catch (Grpc.Core.RpcException rpcEx)
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

            var patientsResponse = await _userGrpcClient.GetUsersBasicInfoAsync(patientRequest);
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
        catch (Grpc.Core.RpcException rpcEx)
        {
            LogWarning("gRPC error batch fetching patients for {Context}: {Error}", null, context, rpcEx.Status.Detail);
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

    #endregion
}