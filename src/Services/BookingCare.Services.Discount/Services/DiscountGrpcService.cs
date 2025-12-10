using AutoMapper;
using BookingCare.Services.Discount.Models.DTOs;
using BookingCare.Services.Discount.Services;
using Grpc.Core;
using Protos = BookingCare.Services.Discount.Protos;

namespace BookingCare.Services.Discount.Services;

public class DiscountGrpcService : Protos.DiscountService.DiscountServiceBase
{
    private readonly IDiscountService _discountService;
    private readonly IMapper _mapper;
    private readonly ILogger<DiscountGrpcService> _logger;

    public DiscountGrpcService(
        IDiscountService discountService,
        IMapper mapper,
        ILogger<DiscountGrpcService> logger
    )
    {
        _discountService = discountService;
        _mapper = mapper;
        _logger = logger;
    }

    public override async Task<Protos.ValidateDiscountResponse> ValidateDiscount(
        Protos.ValidateDiscountRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation("gRPC ValidateDiscount called for code: {Code}", request.Code);

            var hospitalIdString = request.HospitalId;
            var validateRequest = new Models.DTOs.ValidateDiscountRequest
            {
                Code = request.Code,
                HospitalId = Guid.Parse(hospitalIdString),
                TotalAmount = (decimal)request.TotalAmount,
            };

            var result = await _discountService.ValidateDiscountAsync(validateRequest);

            var response = new Protos.ValidateDiscountResponse
            {
                IsValid = result.IsValid,
                Message = result.Message,
                DiscountAmount = (double)result.DiscountAmount,
                FinalAmount = (double)result.FinalAmount,
            };

            if (result.Discount != null)
            {
                response.Discount = MapToDiscountInfo(result.Discount);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC ValidateDiscount");
            throw new RpcException(
                new Status(StatusCode.Internal, $"Internal error: {ex.Message}")
            );
        }
    }

    public override async Task<Protos.UseDiscountResponse> UseDiscount(
        Protos.UseDiscountRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation("gRPC UseDiscount called for code: {Code}", request.Code);

            var useRequest = new Models.DTOs.UseDiscountRequest
            {
                Code = request.Code,
                HospitalId = Guid.Parse(request.HospitalId),
                TotalAmount = (decimal)request.TotalAmount,
            };

            var result = await _discountService.UseDiscountAsync(useRequest);

            return new Protos.UseDiscountResponse
            {
                Success = result.Success,
                Message = result.Message,
                DiscountAmount = (double)result.DiscountAmount,
                FinalAmount = (double)result.FinalAmount,
                DiscountId = result.DiscountId.ToString(),
                RemainingUses = result.RemainingUses,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC UseDiscount");
            throw new RpcException(
                new Status(StatusCode.Internal, $"Internal error: {ex.Message}")
            );
        }
    }

    public override async Task<Protos.RevertDiscountUsageResponse> RevertDiscountUsage(
        Protos.RevertDiscountUsageRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation(
                "gRPC RevertDiscountUsage called for code: {Code}",
                request.Code
            );

            var result = await _discountService.RevertDiscountUsageAsync(
                request.Code,
                Guid.Parse(request.HospitalId)
            );

            return new Protos.RevertDiscountUsageResponse
            {
                Success = result,
                Message = result
                    ? "Discount usage reverted successfully"
                    : "Failed to revert discount usage",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC RevertDiscountUsage");
            throw new RpcException(
                new Status(StatusCode.Internal, $"Internal error: {ex.Message}")
            );
        }
    }

    public override async Task<Protos.CalculateDiscountAmountResponse> CalculateDiscountAmount(
        Protos.CalculateDiscountAmountRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation(
                "gRPC CalculateDiscountAmount called for code: {Code}",
                request.Code
            );

            var discountAmount = await _discountService.CalculateDiscountAmountAsync(
                request.Code,
                (decimal)request.OriginalAmount,
                Guid.Parse(request.HospitalId),
                null,
                null
            );

            var finalAmount = (decimal)request.OriginalAmount - discountAmount;

            return new Protos.CalculateDiscountAmountResponse
            {
                DiscountAmount = (double)discountAmount,
                FinalAmount = (double)finalAmount,
                IsValid = discountAmount > 0,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CalculateDiscountAmount");
            throw new RpcException(
                new Status(StatusCode.Internal, $"Internal error: {ex.Message}")
            );
        }
    }

    public override async Task<Protos.GetApplicableDiscountsResponse> GetApplicableDiscounts(
        Protos.GetApplicableDiscountsRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation(
                "gRPC GetApplicableDiscounts called for hospital: {HospitalId}",
                request.HospitalId
            );

            var discounts = await _discountService.GetApplicableDiscountsAsync(
                Guid.Parse(request.HospitalId),
                null,
                null
            );

            var response = new Protos.GetApplicableDiscountsResponse();
            response.Discounts.AddRange(discounts.Select(MapToDiscountInfo));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetApplicableDiscounts");
            throw new RpcException(
                new Status(StatusCode.Internal, $"Internal error: {ex.Message}")
            );
        }
    }

    public override async Task<Protos.IsDiscountValidResponse> IsDiscountValid(
        Protos.IsDiscountValidRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation("gRPC IsDiscountValid called for code: {Code}", request.Code);

            var isValid = await _discountService.IsDiscountValidAsync(
                request.Code,
                Guid.Parse(request.HospitalId),
                null,
                null
            );

            return new Protos.IsDiscountValidResponse { IsValid = isValid };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC IsDiscountValid");
            throw new RpcException(
                new Status(StatusCode.Internal, $"Internal error: {ex.Message}")
            );
        }
    }

    private static Protos.DiscountInfo MapToDiscountInfo(DiscountResponse discount)
    {
        return new Protos.DiscountInfo
        {
            Id = discount.Id.ToString(),
            Code = discount.Code,
            Name = discount.Name,
            Description = discount.Description ?? "",
            HospitalId = discount.HospitalId.ToString(),
            SpecialtyId = "",
            DoctorId = "",
            Amount = (double)discount.Amount,
            DiscountType = discount.DiscountType.ToString(),
            StartDate = discount.StartDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            EndDate = discount.EndDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MaxUses = discount.MaxUses ?? 0,
            UsesCount = discount.UsesCount,
            Status = discount.Status.ToString(),
        };
    }
}
