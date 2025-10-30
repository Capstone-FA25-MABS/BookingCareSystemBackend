using Grpc.Core;
using AutoMapper;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Exceptions;
using CommonStatus = BookingCare.Shared.Common.Enums.Status;
using GrpcStatus = Grpc.Core.Status;
using BookingCare.Services.Hospital.Models.DTOs;

namespace BookingCare.Services.Hospital.Services;

public class HospitalGrpcService : HospitalService.HospitalServiceBase
{
    private readonly IHospitalService _hospitalService;
    private readonly ILogger<HospitalGrpcService> _logger;

    public HospitalGrpcService(
        IHospitalService hospitalService,
        ILogger<HospitalGrpcService> logger)
    {
        _hospitalService = hospitalService;
        _logger = logger;
    }

    private static HospitalReply MapToHospitalReply(BookingCare.Services.Hospital.Models.Entities.HospitalEntity hospital)
    {
        var dto = new HospitalMappingDto
        {
            Id = hospital.Id,
            AccountId = hospital.AccountId,
            Name = hospital.Name,
            Address = hospital.Address,
            Phone = hospital.Phone,
            Email = hospital.Email,
            Description = hospital.Description,
            BackgroundUrl = hospital.BackgroundUrl,
            AvatarUrl = hospital.AvatarUrl,
            CreatedAt = hospital.CreatedAt,
            UpdatedAt = hospital.UpdatedAt
        };

        return MapToHospitalReplyInternal(dto);
    }

    private static HospitalReply MapToHospitalReply(BookingCare.Services.Hospital.Models.DTOs.Responses.HospitalDetailResponse hospital)
    {
        var dto = new HospitalMappingDto
        {
            Id = hospital.Id,
            AccountId = hospital.AccountId,
            Name = hospital.Name,
            Address = hospital.Address,
            Phone = hospital.Phone,
            Email = hospital.Email,
            Description = hospital.Description,
            BackgroundUrl = hospital.BackgroundUrl,
            AvatarUrl = hospital.AvatarUrl,
            CreatedAt = hospital.CreatedAt,
            UpdatedAt = hospital.UpdatedAt
        };

        return MapToHospitalReplyInternal(dto);
    }

    private static HospitalReply MapToHospitalReplyInternal(HospitalMappingDto dto)
    {
        return new HospitalReply
        {
            Id = dto.Id.ToString(),
            AccountId = dto.AccountId.ToString(),
            Name = dto.Name,
            Address = dto.Address,
            Phone = dto.Phone ?? "",
            Email = dto.Email,
            Description = dto.Description,
            BackgroundUrl = dto.BackgroundUrl ?? "",
            AvatarUrl = dto.AvatarUrl ?? "",
            Status = "ACTIVE", // Status is now managed by Auth service
            CreatedAt = dto.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            UpdatedAt = dto.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    private static HospitalReply MapToHospitalReply(BookingCare.Services.Hospital.Models.DTOs.Responses.HospitalResponse hospital)
    {
        return new HospitalReply
        {
            Id = hospital.Id.ToString(),
            AccountId = hospital.AccountId.ToString(),
            Name = hospital.Name,
            Address = hospital.Address,
            Phone = hospital.Phone ?? "",
            Email = hospital.Email,
            Description = hospital.Description,
            BackgroundUrl = hospital.BackgroundUrl ?? "",
            AvatarUrl = hospital.AvatarUrl ?? "",
            Status = "ACTIVE", // Status is now managed by Auth service
            CreatedAt = hospital.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            UpdatedAt = hospital.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    private static HospitalReply MapToHospitalReply(BookingCare.Services.Hospital.Models.DTOs.Responses.HospitalProfileResponse hospital)
    {
        return new HospitalReply
        {
            Id = hospital.Id.ToString(),
            AccountId = "", // HospitalProfileResponse doesn't include AccountId
            Name = hospital.Name,
            Address = hospital.Address,
            Phone = hospital.Phone ?? "",
            Email = hospital.Email,
            Description = hospital.Description,
            BackgroundUrl = hospital.BackgroundUrl ?? "",
            AvatarUrl = hospital.AvatarUrl ?? "",
            Status = "ACTIVE", // Status is now managed by Auth service
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), // Default value since not included
            UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") // Default value since not included
        };
    }

    public override async Task<HospitalReply> GetHospital(GetHospitalRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var hospitalId))
            {
                throw new RpcException(new GrpcStatus(StatusCode.InvalidArgument, "Invalid hospital ID format"));
            }

            var hospital = await _hospitalService.GetByIdAsync(hospitalId);
            if (hospital == null)
            {
                throw new RpcException(new GrpcStatus(StatusCode.NotFound, $"Hospital with ID {request.Id} not found"));
            }

            return MapToHospitalReply(hospital);
        }
        catch (HospitalNotFoundException)
        {
            throw new RpcException(new GrpcStatus(StatusCode.NotFound, $"Hospital with ID {request.Id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hospital with ID {HospitalId}", request.Id);
            throw new RpcException(new GrpcStatus(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<HospitalListReply> GetHospitalsBySpecialty(GetHospitalsBySpecialtyRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.SpecialtyId, out var specialtyId))
            {
                throw new RpcException(new GrpcStatus(StatusCode.InvalidArgument, "Invalid specialty ID format"));
            }

            var hospitals = await _hospitalService.GetBySpecialtyAsync(specialtyId);
            var reply = new HospitalListReply
            {
                TotalCount = hospitals.Count()
            };

            foreach (var hospital in hospitals)
            {
                reply.Hospitals.Add(MapToHospitalReply(hospital));
            }

            return reply;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hospitals by specialty {SpecialtyId}", request.SpecialtyId);
            throw new RpcException(new GrpcStatus(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<HospitalBasicListReply> GetHospitalsList(GetHospitalsListRequest request, ServerCallContext context)
    {
        try
        {
            // Create filter request
            var filter = new Models.DTOs.Requests.HospitalFilterRequest
            {
                Page = request.Page > 0 ? request.Page : 1,
                PageSize = request.PageSize > 0 ? request.PageSize : 50
            };

            // Note: Status filtering is now handled by Auth service, not in database query

            var hospitalsResponse = await _hospitalService.GetFilteredAsync(filter);
            var reply = new HospitalBasicListReply
            {
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = hospitalsResponse.TotalCount
            };

            foreach (var hospital in hospitalsResponse.Hospitals)
            {
                reply.Hospitals.Add(new HospitalBasicReply
                {
                    Id = hospital.Id.ToString(),
                    Name = hospital.Name,
                    Address = hospital.Address
                });
            }

            return reply;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hospitals list");
            throw new RpcException(new GrpcStatus(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<HospitalReply> CreateHospital(CreateHospitalGrpcRequest grpcRequest, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(grpcRequest.AccountId, out var accountId))
            {
                throw new RpcException(new GrpcStatus(StatusCode.InvalidArgument, "Invalid account ID format"));
            }

            var createRequest = new BookingCare.Services.Hospital.Models.DTOs.Requests.CreateHospitalRequest
            {
                AccountId = accountId,
                Name = grpcRequest.Name,
                Address = grpcRequest.Address,
                Phone = grpcRequest.Phone,
                Email = grpcRequest.Email,
                Description = grpcRequest.Description,
                BackgroundUrl = grpcRequest.BackgroundUrl,
                AvatarUrl = grpcRequest.AvatarUrl
            };

            var hospital = await _hospitalService.CreateAsync(createRequest);

            return MapToHospitalReply(hospital);
        }
        catch (HospitalAlreadyExistsException ex)
        {
            throw new RpcException(new GrpcStatus(StatusCode.AlreadyExists, ex.Message));
        }
        catch (InvalidHospitalDataException ex)
        {
            throw new RpcException(new GrpcStatus(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hospital");
            throw new RpcException(new GrpcStatus(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<DeleteHospitalReply> DeleteHospital(DeleteHospitalRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var hospitalId))
            {
                throw new RpcException(new GrpcStatus(StatusCode.InvalidArgument, "Invalid hospital ID format"));
            }

            var result = await _hospitalService.DeleteAsync(hospitalId);

            return new DeleteHospitalReply
            {
                Success = result,
                Message = result ? "Hospital deleted successfully" : "Failed to delete hospital"
            };
        }
        catch (HospitalNotFoundException ex)
        {
            throw new RpcException(new GrpcStatus(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hospital with ID {HospitalId}", request.Id);
            throw new RpcException(new GrpcStatus(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<HospitalBasicInfoResponse> GetHospitalBasicInfo(GetHospitalBasicInfoRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var id))
            {
                throw new RpcException(new GrpcStatus(StatusCode.InvalidArgument, "Invalid hospital ID format"));
            }

            var hospital = await _hospitalService.GetHospitalBasicInfoByIdAsync(id);
            if (hospital == null)
            {
                throw new RpcException(new GrpcStatus(StatusCode.NotFound, $"Hospital with ID {id} not found"));
            }

            return MapToHospitalBasicInfoResponse(hospital);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetHospitalBasicInfo for {Id}", request.Id);
            throw new RpcException(new GrpcStatus(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<HospitalsBasicInfoResponse> GetHospitalsBasicInfo(GetHospitalsBasicInfoRequest request, ServerCallContext context)
    {
        try
        {
            var ids = new List<Guid>();
            foreach (var idStr in request.Ids)
            {
                if (!Guid.TryParse(idStr, out var id))
                {
                    throw new RpcException(new GrpcStatus(StatusCode.InvalidArgument, $"Invalid hospital ID format: {idStr}"));
                }
                ids.Add(id);
            }

            var hospitals = await _hospitalService.GetHospitalsBasicInfoByIdsAsync(ids);
            var response = new HospitalsBasicInfoResponse();

            foreach (var hospital in hospitals)
            {
                response.Hospitals.Add(MapToHospitalBasicInfoResponse(hospital));
            }

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetHospitalsBasicInfo");
            throw new RpcException(new GrpcStatus(StatusCode.Internal, "Internal server error"));
        }
    }

    private static HospitalBasicInfoResponse MapToHospitalBasicInfoResponse(BookingCare.Services.Hospital.Models.Entities.HospitalEntity hospital)
    {
        return new HospitalBasicInfoResponse
        {
            Id = hospital.Id.ToString(),
            Name = hospital.Name,
            Address = hospital.Address,
            Phone = hospital.Phone ?? string.Empty,
            Email = hospital.Email,
            AvatarUrl = hospital.AvatarUrl ?? string.Empty
        };
    }
}
