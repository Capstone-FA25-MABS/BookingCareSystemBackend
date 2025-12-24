using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using Grpc.Core;

namespace BookingCare.Services.Hospital.Services;

public class SubscriptionUsageGrpcService : SubscriptionUsageGrpc.SubscriptionUsageGrpcBase
{
    private readonly ISubscriptionUsageService _usageService;
    private readonly ILogger<SubscriptionUsageGrpcService> _logger;

    public SubscriptionUsageGrpcService(
        ISubscriptionUsageService usageService,
        ILogger<SubscriptionUsageGrpcService> logger)
    {
        _usageService = usageService;
        _logger = logger;
    }

    public override async Task<CheckLimitResponse> CheckDoctorLimit(
        CheckLimitRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            var canAdd = await _usageService.CheckDoctorLimitAsync(hospitalId);

            return new CheckLimitResponse
            {
                CanAdd = canAdd,
                HospitalId = request.HospitalId,
                Message = canAdd ? string.Empty : "Đã đạt giới hạn số lượng bác sĩ cho phép trong gói đăng ký"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error checking doctor limit for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<CheckLimitResponse> CheckSpecialtyLimit(
        CheckLimitRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            var canAdd = await _usageService.CheckSpecialtyLimitAsync(hospitalId);

            return new CheckLimitResponse
            {
                CanAdd = canAdd,
                HospitalId = request.HospitalId,
                Message = canAdd ? string.Empty : "Đã đạt giới hạn số lượng chuyên khoa cho phép trong gói đăng ký"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error checking specialty limit for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<CheckLimitResponse> CheckServiceLimit(
        CheckLimitRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            var canAdd = await _usageService.CheckServiceLimitAsync(hospitalId);

            return new CheckLimitResponse
            {
                CanAdd = canAdd,
                HospitalId = request.HospitalId,
                Message = canAdd ? string.Empty : "Đã đạt giới hạn số lượng dịch vụ cho phép trong gói đăng ký"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error checking service limit for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<CheckLimitResponse> CheckAppointmentLimit(
        CheckAppointmentLimitRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            var canAdd = await _usageService.CheckAppointmentLimitAsync(hospitalId, request.AdditionalAppointments);

            return new CheckLimitResponse
            {
                CanAdd = canAdd,
                HospitalId = request.HospitalId,
                Message = canAdd ? string.Empty : "Đã đạt giới hạn số lượng lịch hẹn cho phép trong gói đăng ký"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error checking appointment limit for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<IncrementResponse> IncrementDoctorCount(
        IncrementRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            await _usageService.IncrementDoctorCountAsync(hospitalId);

            return new IncrementResponse
            {
                Success = true,
                HospitalId = request.HospitalId,
                Message = "Doctor count incremented successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error incrementing doctor count for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<IncrementResponse> DecrementDoctorCount(
        IncrementRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            await _usageService.DecrementDoctorCountAsync(hospitalId);

            return new IncrementResponse
            {
                Success = true,
                HospitalId = request.HospitalId,
                Message = "Doctor count decremented successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error decrementing doctor count for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<IncrementResponse> IncrementSpecialtyCount(
        IncrementRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            await _usageService.IncrementSpecialtyCountAsync(hospitalId);

            return new IncrementResponse
            {
                Success = true,
                HospitalId = request.HospitalId,
                Message = "Specialty count incremented successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error incrementing specialty count for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<IncrementResponse> DecrementSpecialtyCount(
        IncrementRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            await _usageService.DecrementSpecialtyCountAsync(hospitalId);

            return new IncrementResponse
            {
                Success = true,
                HospitalId = request.HospitalId,
                Message = "Specialty count decremented successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error decrementing specialty count for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<IncrementResponse> IncrementServiceCount(
        IncrementRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            await _usageService.IncrementServiceCountAsync(hospitalId);

            return new IncrementResponse
            {
                Success = true,
                HospitalId = request.HospitalId,
                Message = "Service count incremented successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error incrementing service count for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<IncrementResponse> DecrementServiceCount(
        IncrementRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            await _usageService.DecrementServiceCountAsync(hospitalId);

            return new IncrementResponse
            {
                Success = true,
                HospitalId = request.HospitalId,
                Message = "Service count decremented successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error decrementing service count for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<IncrementResponse> IncrementAppointmentCount(
        IncrementAppointmentRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            await _usageService.IncrementAppointmentCountAsync(hospitalId, request.Count);

            return new IncrementResponse
            {
                Success = true,
                HospitalId = request.HospitalId,
                Message = "Appointment count incremented successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error incrementing appointment count for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<IncrementResponse> DecrementAppointmentCount(
        IncrementAppointmentRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            await _usageService.DecrementAppointmentCountAsync(hospitalId, request.Count);

            return new IncrementResponse
            {
                Success = true,
                HospitalId = request.HospitalId,
                Message = "Appointment count decremented successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error decrementing appointment count for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<SubscriptionUsageGrpcResponse> GetUsageByHospitalId(
        GetUsageRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid HospitalId format"));
            }

            var usage = await _usageService.GetUsageByHospitalIdAsync(hospitalId);

            return MapToGrpcResponse(usage);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error getting usage for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    private static SubscriptionUsageGrpcResponse MapToGrpcResponse(SubscriptionUsageResponse usage)
    {
        return new SubscriptionUsageGrpcResponse
        {
            HospitalId = usage.HospitalId.ToString(),
            HasActiveSubscription = usage.HasActiveSubscription,
            Message = usage.Message ?? string.Empty,
            SubscriptionPlanId = usage.SubscriptionPlanId?.ToString() ?? string.Empty,
            SubscriptionPlanName = usage.SubscriptionPlanName ?? string.Empty,
            // Use -1 to represent unlimited (null) in gRPC since proto doesn't support nullable int32
            MaxDoctors = usage.MaxDoctors ?? -1,
            MaxSpecialties = usage.MaxSpecialties ?? -1,
            MaxAppointments = usage.MaxAppointments ?? -1,
            MaxServices = usage.MaxServices ?? -1,
            CurrentDoctorCount = usage.CurrentDoctorCount,
            CurrentSpecialtyCount = usage.CurrentSpecialtyCount,
            CurrentAppointmentCount = usage.CurrentAppointmentCount,
            CurrentServiceCount = usage.CurrentServiceCount,
            DoctorUsagePercentage = (double)usage.DoctorUsagePercentage,
            SpecialtyUsagePercentage = (double)usage.SpecialtyUsagePercentage,
            ServiceUsagePercentage = (double)usage.ServiceUsagePercentage,
            IsDoctorLimitExceeded = usage.IsDoctorLimitExceeded,
            IsSpecialtyLimitExceeded = usage.IsSpecialtyLimitExceeded,
            IsServiceLimitExceeded = usage.IsServiceLimitExceeded,
            SubscriptionEndDate = usage.SubscriptionEndDate?.ToString("O") ?? string.Empty,
            DaysUntilExpiry = usage.DaysUntilExpiry
        };
    }
}


