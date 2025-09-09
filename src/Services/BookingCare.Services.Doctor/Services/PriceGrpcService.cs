using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Doctor.Services;
using BookingCare.Services.Doctor.Models.DTOs;
using Grpc.Core;
using AutoMapper;

namespace BookingCare.Services.Doctor.Services;

public class PriceGrpcService : Protos.PriceService.PriceServiceBase
{
    private readonly IPriceService _priceService;
    private readonly IMapper _mapper;
    private readonly ILogger<PriceGrpcService> _logger;

    public PriceGrpcService(
        IPriceService priceService,
        IMapper mapper, 
        ILogger<PriceGrpcService> logger)
    {
        _priceService = priceService;
        _mapper = mapper;
        _logger = logger;
    }

    #region Price gRPC Operations

    public override async Task<CreatePriceResponse> CreatePrice(
        Protos.CreatePriceRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC CreatePrice called for amount: {Amount}", request.Amount);

            var createRequest = new Models.DTOs.CreatePriceRequest
            {
                Amount = (decimal)request.Amount
            };

            var result = await _priceService.CreatePriceAsync(createRequest);

            return new CreatePriceResponse
            {
                Success = true,
                Price = MapToPriceInfo(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CreatePrice");
            return new CreatePriceResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<GetPriceResponse> GetPriceById(
        Protos.GetPriceByIdRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetPriceById called for ID: {Id}", request.Id);

            var priceId = Guid.Parse(request.Id);
            var result = await _priceService.GetPriceByIdAsync(priceId);

            if (result == null)
            {
                return new GetPriceResponse
                {
                    Success = false,
                    ErrorMessage = "Price not found"
                };
            }

            return new GetPriceResponse
            {
                Success = true,
                Price = MapToPriceInfo(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetPriceById");
            return new GetPriceResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<GetPricesResponse> GetPrices(
        Protos.GetPricesRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetPrices called");

            var pageSize = request.PageSize <= 0 ? 1000 : Math.Min(request.PageSize, 1000);
            var queryRequest = new Models.DTOs.PriceQueryRequest
            {
                PageNumber = request.PageNumber,
                PageSize = pageSize,
                MinAmount = request.MinAmount > 0 ? (decimal)request.MinAmount : null,
                MaxAmount = request.MaxAmount > 0 ? (decimal)request.MaxAmount : null
            };

            var result = await _priceService.GetPricesAsync(queryRequest);

            var response = new GetPricesResponse
            {
                Success = true,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            foreach (var price in result.Prices)
            {
                response.Prices.Add(MapToPriceInfo(price));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetPrices");
            return new GetPricesResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Mapping Methods

    private static PriceInfo MapToPriceInfo(Models.DTOs.PriceResponse price)
    {
        return new PriceInfo
        {
            Id = price.Id.ToString(),
            Amount = (double)price.Amount
        };
    }

    #endregion
}
