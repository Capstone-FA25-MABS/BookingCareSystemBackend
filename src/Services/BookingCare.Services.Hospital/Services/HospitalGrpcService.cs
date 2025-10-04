using Grpc.Core;
using AutoMapper;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Exceptions;
using CommonStatus = BookingCare.Shared.Common.Enums.Status;
using GrpcStatus = Grpc.Core.Status;

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
                reply.Hospitals.Add(new HospitalReply
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
                });
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
