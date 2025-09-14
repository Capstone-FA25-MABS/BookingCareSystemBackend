using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using Grpc.Core;
using AutoMapper;

namespace BookingCare.Services.Doctor.Services;

public class PositionGrpcService : Protos.PositionService.PositionServiceBase
{
    private readonly IPositionService _positionService;
    private readonly IMapper _mapper;
    private readonly ILogger<PositionGrpcService> _logger;

    public PositionGrpcService(
        IPositionService positionService,
        IMapper mapper,
        ILogger<PositionGrpcService> logger)
    {
        _positionService = positionService;
        _mapper = mapper;
        _logger = logger;
    }

    #region Position gRPC Operations

    public override async Task<CreatePositionResponse> CreatePosition(
        Protos.CreatePositionRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC CreatePosition called for name: {Name}", request.Name);

            var createRequest = new Models.DTOs.Requests.CreatePositionRequest
            {
                Name = request.Name
            };

            var result = await _positionService.CreatePositionAsync(createRequest);

            return new CreatePositionResponse
            {
                Success = true,
                Position = MapToPositionInfo(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CreatePosition");
            return new CreatePositionResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<GetPositionResponse> GetPositionById(
        Protos.GetPositionByIdRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetPositionById called for ID: {Id}", request.Id);

            var positionId = Guid.Parse(request.Id);
            var result = await _positionService.GetPositionByIdAsync(positionId);

            if (result == null)
            {
                return new GetPositionResponse
                {
                    Success = false,
                    ErrorMessage = "Position not found"
                };
            }

            return new GetPositionResponse
            {
                Success = true,
                Position = MapToPositionInfo(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetPositionById");
            return new GetPositionResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<GetPositionsResponse> GetPositions(
        Protos.GetPositionsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetPositions called");

            var pageSize = request.PageSize <= 0 ? 1000 : Math.Min(request.PageSize, 1000);
            var queryRequest = new PositionQueryRequest
            {
                PageNumber = request.PageNumber,
                PageSize = pageSize,
                SearchTerm = request.SearchTerm
            };

            var result = await _positionService.GetPositionsAsync(queryRequest);

            var response = new GetPositionsResponse
            {
                Success = true,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            foreach (var position in result.Positions)
            {
                response.Positions.Add(MapToPositionInfo(position));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetPositions");
            return new GetPositionsResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Mapping Methods

    private static PositionInfo MapToPositionInfo(PositionResponse position)
    {
        return new PositionInfo
        {
            Id = position.Id.ToString(),
            Name = position.Name,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(position.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(position.UpdatedAt.ToUniversalTime())
        };
    }

    #endregion
}
