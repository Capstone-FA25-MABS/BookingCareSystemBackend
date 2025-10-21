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

            return MapToGrpcDoctorResponseFromById(doctor);
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

    public override async Task<Protos.DoctorBasicInfoResponse> GetDoctorBasicInfo(Protos.GetDoctorBasicInfoRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var id))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid doctor ID format"));
            }

            var doctor = await _doctorService.GetDoctorBasicInfoByIdAsync(id);
            if (doctor == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Doctor with ID {id} not found"));
            }

            return MapToGrpcDoctorBasicInfoResponse(doctor);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorBasicInfo for {Id}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.DoctorsBasicInfoResponse> GetDoctorsBasicInfo(Protos.GetDoctorsBasicInfoRequest request, ServerCallContext context)
    {
        try
        {
            var ids = new List<Guid>();
            foreach (var idStr in request.Ids)
            {
                if (!Guid.TryParse(idStr, out var id))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid doctor ID format: {idStr}"));
                }
                ids.Add(id);
            }

            var doctors = await _doctorService.GetDoctorsBasicInfoByIdsAsync(ids);
            var response = new Protos.DoctorsBasicInfoResponse();

            foreach (var doctor in doctors)
            {
                response.Doctors.Add(MapToGrpcDoctorBasicInfoResponse(doctor));
            }

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorsBasicInfo");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    private static Protos.DoctorBasicInfoResponse MapToGrpcDoctorBasicInfoResponse(Models.Entities.DoctorEntity doctor)
    {
        return new Protos.DoctorBasicInfoResponse
        {
            Id = doctor.Id.ToString(),
            Email = doctor.Email,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            FullName = $"{doctor.FirstName} {doctor.LastName}".Trim(),
            PositionName = doctor.Position?.Name ?? string.Empty,
            SpecialtyName = doctor.Specialty?.Name ?? string.Empty,
            AvatarUrl = doctor.AvatarUrl,
            HospitalId = doctor.HospitalId?.ToString() ?? string.Empty
        };
    }

    public override async Task<Protos.GetAvailableDoctorsResponse> GetAvailableDoctors(
        Protos.GetAvailableDoctorsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[DoctorGrpcService] GetAvailableDoctors called for hospital {HospitalId}, specialty {SpecialtyId}",
                request.HospitalId, request.SpecialtyId);

            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid hospital ID format"));
            }

            if (!Guid.TryParse(request.SpecialtyId, out var specialtyId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid specialty ID format"));
            }

            // Get doctors by hospital and specialty (without availability check)
            var doctors = await _doctorService.GetDoctorsByHospitalAndSpecialtyAsync(hospitalId, specialtyId);

            var response = new Protos.GetAvailableDoctorsResponse
            {
                TotalCount = doctors.Count
            };

            foreach (var doctor in doctors)
            {
                response.Doctors.Add(new Protos.AvailableDoctorInfo
                {
                    Id = doctor.Id.ToString(),
                    FirstName = doctor.FirstName,
                    LastName = doctor.LastName,
                    FullName = $"{doctor.FirstName} {doctor.LastName}".Trim(),
                    AvatarUrl = doctor.AvatarUrl,
                    PositionName = doctor.Position?.Name ?? string.Empty,
                    SpecialtyName = doctor.Specialty?.Name ?? string.Empty,
                    YearsOfExperience = doctor.YearsOfExperience
                });
            }

            _logger.LogInformation("[DoctorGrpcService] Returning {Count} doctors", doctors.Count);
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetAvailableDoctors");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.GetDoctorPriceResponse> GetDoctorPrice(
        Protos.GetDoctorPriceRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[DoctorGrpcService] GetDoctorPrice called for price ID {PriceId}", request.PriceId);

            if (!Guid.TryParse(request.PriceId, out var priceId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid price ID format"));
            }

            // Get doctor price from service
            var price = await _doctorService.GetDoctorPriceByIdAsync(priceId);
            if (price == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Price with ID {priceId} not found"));
            }

            var response = new Protos.GetDoctorPriceResponse
            {
                Id = price.Id.ToString(),
                DoctorId = price.DoctorId.ToString(),
                Amount = (double)price.Amount,
                Currency = "VND"
            };

            _logger.LogInformation("[DoctorGrpcService] Returning price {Amount} VND for price ID {PriceId}",
                price.Amount, priceId);
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorPrice for {PriceId}", request.PriceId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
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

    private static Protos.DoctorResponse MapToGrpcDoctorResponseFromById(DoctorByIdResponse d)
    {
        return new Protos.DoctorResponse
        {
            Id = d.Id.ToString(),
            AccountId = string.Empty, // Not available in DoctorByIdResponse
            Email = d.Email,
            FirstName = d.FirstName,
            LastName = d.LastName,
            FullName = $"{d.FirstName} {d.LastName}".Trim(),
            Gender = d.Gender?.ToString() ?? string.Empty,
            Address = d.Address ?? string.Empty,
            SpecialtyId = d.Specialty?.Id.ToString() ?? string.Empty,
            PositionId = d.Position?.Id.ToString() ?? string.Empty,
            HospitalId = d.Hospital?.Id.ToString() ?? string.Empty,
            Bio = d.Bio ?? string.Empty,
            YearsOfExperience = d.YearsOfExperience,
            AvatarUrl = d.AvatarUrl,
            CreatedAt = string.Empty, // Not available in DoctorByIdResponse
            UpdatedAt = string.Empty, // Not available in DoctorByIdResponse
            Status = string.Empty // Not available in DoctorByIdResponse
        };
    }
}
