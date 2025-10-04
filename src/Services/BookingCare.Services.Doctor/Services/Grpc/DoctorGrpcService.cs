using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using Grpc.Core;

namespace BookingCare.Services.Doctor.Services.Grpc;

public class DoctorGrpcService : Protos.DoctorService.DoctorServiceBase
{
    private readonly IDoctorService _doctorService;
    private readonly ILogger<DoctorGrpcService> _logger;

    public DoctorGrpcService(IDoctorService doctorService, ILogger<DoctorGrpcService> logger)
    {
        _doctorService = doctorService;
        _logger = logger;
    }

    public override async Task<Protos.DoctorResponse> GetDoctor(Protos.GetDoctorRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var id))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid doctor ID format"));
            }

            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Doctor with ID {id} not found"));
            }

            return MapToGrpcDoctorResponse(doctor);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctor for {Id}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.DoctorResponse> GetDoctorByAccountId(Protos.GetDoctorByAccountIdRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.AccountId, out var accountId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid account ID format"));
            }

            var doctor = await _doctorService.GetDoctorByAccountIdAsync(accountId);
            if (doctor == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Doctor with AccountId {accountId} not found"));
            }

            return MapToGrpcDoctorResponse(doctor);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorByAccountId for {AccountId}", request.AccountId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.DoctorResponse> CreateDoctor(Protos.CreateDoctorRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.AccountId, out var accountId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid account ID format"));
            }

            var create = new Models.DTOs.Requests.CreateDoctorRequest
            {
                AccountId = accountId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address,
                Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio,
                YearsOfExperience = request.YearsOfExperience,
                AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl
            };

            if (Guid.TryParse(request.SpecialtyId, out var specialtyId)) create.SpecialtyId = specialtyId;
            if (Guid.TryParse(request.PositionId, out var positionId)) create.PositionId = positionId;
            if (Guid.TryParse(request.HospitalId, out var hospitalId)) create.HospitalId = hospitalId;

            if (!string.IsNullOrWhiteSpace(request.Gender) && Enum.TryParse<Shared.Common.Enums.Gender>(request.Gender, true, out var gender))
            {
                create.Gender = gender;
            }

            var doctor = await _doctorService.CreateDoctorAsync(create);
            return MapToGrpcDoctorResponse(doctor);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in CreateDoctor for email {Email}", request.Email);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.DoctorBatchResponse> GetDoctorsByAccountIds(Protos.GetDoctorsByAccountIdsRequest request, ServerCallContext context)
    {
        try
        {
            var accountIds = new List<Guid>();
            foreach (var idStr in request.AccountIds)
            {
                if (!Guid.TryParse(idStr, out var id))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid account ID format: {idStr}"));
                }
                accountIds.Add(id);
            }

            var doctors = await _doctorService.GetDoctorsByAccountIdsAsync(accountIds);
            var resp = new Protos.DoctorBatchResponse();
            foreach (var d in doctors)
            {
                resp.Doctors.Add(new Protos.DoctorBasicInfo
                {
                    AccountId = d.AccountId.ToString(),
                    Email = d.Email,
                    FullName = d.FullName,
                    AvatarUrl = d.AvatarUrl
                });
            }
            return resp;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorsByAccountIds");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.DeleteDoctorResponse> DeleteDoctor(Protos.DeleteDoctorRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[DoctorGrpcService] gRPC DeleteDoctor called for ID: {DoctorId}", request.Id);

            if (!Guid.TryParse(request.Id, out var doctorId))
            {
                return new Protos.DeleteDoctorResponse
                {
                    Success = false,
                    Message = "Invalid doctor ID format"
                };
            }

            var result = await _doctorService.DeleteDoctorAsync(doctorId);

            if (result)
            {
                _logger.LogInformation("[DoctorGrpcService] Doctor deleted successfully: {DoctorId}", doctorId);
                return new Protos.DeleteDoctorResponse
                {
                    Success = true,
                    Message = "Doctor deleted successfully"
                };
            }
            else
            {
                return new Protos.DeleteDoctorResponse
                {
                    Success = true, // Consider it successful if already deleted
                    Message = "Doctor not found or already deleted"
                };
            }
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error deleting doctor: {DoctorId}", request.Id);
            return new Protos.DeleteDoctorResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static Protos.DoctorResponse MapToGrpcDoctorResponse(DoctorResponse d)
    {
        return new Protos.DoctorResponse
        {
            Id = d.Id.ToString(),
            AccountId = d.AccountId.ToString(),
            Email = d.Email,
            FirstName = d.FirstName,
            LastName = d.LastName,
            FullName = $"{d.FirstName} {d.LastName}".Trim(),
            Gender = d.Gender?.ToString() ?? string.Empty,
            Address = d.Address ?? string.Empty,
            SpecialtyId = d.SpecialtyId?.ToString() ?? string.Empty,
            PositionId = d.PositionId?.ToString() ?? string.Empty,
            HospitalId = d.HospitalId?.ToString() ?? string.Empty,
            Bio = d.Bio ?? string.Empty,
            YearsOfExperience = d.YearsOfExperience,
            AvatarUrl = d.AvatarUrl,
            CreatedAt = d.CreatedAt.ToString("O"),
            UpdatedAt = d.UpdatedAt.ToString("O"),
            Status = d.Status.ToString()
        };
    }
}
