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
            Status = hospital.Status,
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
            Status = hospital.Status,
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
            Status = dto.Status.ToString(),
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
            Status = hospital.Status.ToString(),
            CreatedAt = hospital.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            UpdatedAt = hospital.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
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

            // Apply status filter if provided
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<CommonStatus>(request.Status, true, out var status))
            {
                filter.Status = status;
            }

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
}
