using AutoMapper;
using BookingCare.Services.Appointment.Exceptions;
using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Services.Appointment.Models.Entities;
using BookingCare.Services.Appointment.Repositories;
using BookingCare.Services.Appointment.Enums;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Doctor.Protos;

namespace BookingCare.Services.Appointment.Services;

/// <summary>
/// Service implementation for Appointment service operations
/// </summary>
public class AppointmentService : BaseService, IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;
    private readonly DoctorService.DoctorServiceClient _doctorGrpcClient;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IMapper mapper,
        DoctorService.DoctorServiceClient doctorGrpcClient,
        ILogger<AppointmentService> logger) : base(logger)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
        _doctorGrpcClient = doctorGrpcClient;
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
                throw new AppointmentConflictException(request.PatientId, request.AppointmentDate, request.AppointmentTimeId);
            }

            // Check doctor availability if doctor is specified
            if (request.DoctorId.HasValue)
            {
                var isDoctorAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
                    request.DoctorId.Value, request.AppointmentDate, request.AppointmentTimeId);
                if (!isDoctorAvailable)
                {
                    throw new DoctorNotAvailableException(request.DoctorId.Value, request.AppointmentDate, request.AppointmentTimeId);
                }
            }

            var appointmentEntity = _mapper.Map<AppointmentEntity>(request);
            await _appointmentRepository.CreateAppointmentAsync(appointmentEntity);

            LogInfo("Successfully created appointment {AppointmentId}", null, appointmentEntity.Id);
            return true;
        }, "CreateAppointment");
    }

    public async Task<AppointmentResponse?> GetAppointmentByIdAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting appointment by ID: {AppointmentId}", null, id);

            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                LogWarning("Appointment not found: {AppointmentId}", null, id);
                return null;
            }

            LogInfo("Appointment found: {AppointmentId}", null, appointment.Id);
            return _mapper.Map<AppointmentResponse>(appointment);
        }, "GetAppointmentById");
    }

    public async Task<AppointmentListResponse> GetAppointmentsByPatientAsync(AppointmentQueryRequest query)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting appointments with query: Page={PageNumber}, Size={PageSize}",
                null, query.PageNumber, query.PageSize);

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

            LogInfo("Retrieved {Count} appointments out of {TotalCount}",
                null, appointments.Count, totalCount);
            return response;
        }, "GetAppointmentsByPatient");
    }

    /// <summary>
    /// Enrich appointments with external data via gRPC calls based on user role
    /// </summary>
    private async Task EnrichAppointmentsWithExternalDataAsync(List<AppointmentResponse> responses, List<AppointmentEntity> entities, Role role)
    {
        for (int i = 0; i < responses.Count; i++)
        {
            var response = responses[i];
            var entity = entities[i];

            try
            {
                switch (role)
                {
                    case Role.PATIENT:
                        // Patient sees doctor, service, and hospital info
                        if (entity.DoctorId.HasValue)
                        {
                            try
                            {
                                var doctorRequest = new GetDoctorRequest
                                {
                                    Id = entity.DoctorId.Value.ToString()
                                };

                                var doctorResponse = await _doctorGrpcClient.GetDoctorAsync(doctorRequest);

                                response.DoctorInfo = new DoctorInfo
                                {
                                    Id = Guid.Parse(doctorResponse.Id),
                                    AccountId = Guid.Parse(doctorResponse.AccountId),
                                    Email = doctorResponse.Email,
                                    FirstName = doctorResponse.FirstName,
                                    LastName = doctorResponse.LastName,
                                    FullName = doctorResponse.FullName,
                                    Gender = doctorResponse.Gender,
                                    Address = doctorResponse.Address,
                                    SpecialtyId = !string.IsNullOrEmpty(doctorResponse.SpecialtyId) ? Guid.Parse(doctorResponse.SpecialtyId) : null,
                                    PositionId = !string.IsNullOrEmpty(doctorResponse.PositionId) ? Guid.Parse(doctorResponse.PositionId) : null,
                                    HospitalId = !string.IsNullOrEmpty(doctorResponse.HospitalId) ? Guid.Parse(doctorResponse.HospitalId) : null,
                                    Bio = doctorResponse.Bio,
                                    YearsOfExperience = doctorResponse.YearsOfExperience,
                                    AvatarUrl = doctorResponse.AvatarUrl,
                                    Status = doctorResponse.Status
                                };

                                LogInfo("Retrieved doctor info for appointment {AppointmentId}, DoctorId {DoctorId}",
                                    null, response.Id, entity.DoctorId.Value);
                            }
                            catch (Grpc.Core.RpcException rpcEx)
                            {
                                LogWarning("gRPC error getting doctor {DoctorId}: {Error}", null, entity.DoctorId.Value, rpcEx.Status.Detail);
                            }
                        }

                        // TODO: Get Service info when available
                        if (entity.ServiceId.HasValue)
                        {
                            LogInfo("TODO: Get service info for appointment {AppointmentId}, ServiceId {ServiceId}",
                                null, response.Id, entity.ServiceId.Value);
                        }

                        // TODO: Get Hospital info when available
                        if (entity.HospitalId.HasValue)
                        {
                            LogInfo("TODO: Get hospital info for appointment {AppointmentId}, HospitalId {HospitalId}",
                                null, response.Id, entity.HospitalId.Value);
                        }
                        break;

                    case Role.DOCTOR:
                        // Doctor sees patient info (and service/hospital if needed)
                        LogInfo("TODO: Get patient info for appointment {AppointmentId}, PatientId {PatientId}",
                            null, response.Id, entity.PatientId);
                        break;

                    case Role.CLINIC:
                        // Hospital sees patient and doctor info
                        LogInfo("TODO: Get patient info for appointment {AppointmentId}, PatientId {PatientId}",
                            null, response.Id, entity.PatientId);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed to enrich appointment {AppointmentId} with external data: {Error}",
                    null, response.Id, ex.Message);
                // Continue processing other appointments even if one fails
            }
        }
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
}