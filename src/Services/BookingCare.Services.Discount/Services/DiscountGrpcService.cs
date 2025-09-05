using BookingCare.Services.Discount.Protos;
using BookingCare.Services.Discount.Services;
using BookingCare.Services.Discount.Models.DTOs;
using Grpc.Core;
using AutoMapper;

namespace BookingCare.Services.Discount.Services;

public class DiscountGrpcService : Protos.DiscountService.DiscountServiceBase
{
    private readonly IDiscountService _discountService;
    private readonly IMapper _mapper;
    private readonly ILogger<DiscountGrpcService> _logger;

    public DiscountGrpcService(
        IDiscountService discountService,
        IMapper mapper,
        ILogger<DiscountGrpcService> logger)
    {
        _discountService = discountService;
        _mapper = mapper;
        _logger = logger;
    }

    public override async Task<ValidateDiscountResponse> ValidateDiscount(
        Protos.ValidateDiscountRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC ValidateDiscount called for code: {Code}", request.Code);

            var validateRequest = new Models.DTOs.ValidateDiscountRequest
            {
                Code = request.Code,
                ClinicId = Guid.Parse(request.ClinicId),
                SpecialtyId = !string.IsNullOrEmpty(request.SpecialtyId) ? Guid.Parse(request.SpecialtyId) : null,
                DoctorId = !string.IsNullOrEmpty(request.DoctorId) ? Guid.Parse(request.DoctorId) : null,
                TotalAmount = (decimal)request.TotalAmount
            };

            var result = await _discountService.ValidateDiscountAsync(validateRequest);

            var response = new ValidateDiscountResponse
            {
                IsValid = result.IsValid,
                Message = result.Message,
                DiscountAmount = (double)result.DiscountAmount,
                FinalAmount = (double)result.FinalAmount
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
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    public override async Task<UseDiscountResponse> UseDiscount(
        Protos.UseDiscountRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC UseDiscount called for code: {Code}", request.Code);

            var useRequest = new Models.DTOs.UseDiscountRequest
            {
                Code = request.Code,
                ClinicId = Guid.Parse(request.ClinicId),
                SpecialtyId = !string.IsNullOrEmpty(request.SpecialtyId) ? Guid.Parse(request.SpecialtyId) : null,
                DoctorId = !string.IsNullOrEmpty(request.DoctorId) ? Guid.Parse(request.DoctorId) : null,
                TotalAmount = (decimal)request.TotalAmount
            };

            var result = await _discountService.UseDiscountAsync(useRequest);

            return new UseDiscountResponse
            {
                Success = result.Success,
                Message = result.Message,
                DiscountAmount = (double)result.DiscountAmount,
                FinalAmount = (double)result.FinalAmount,
                DiscountId = result.DiscountId.ToString(),
                RemainingUses = result.RemainingUses
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC UseDiscount");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    public override async Task<Protos.RevertDiscountUsageResponse> RevertDiscountUsage(
        Protos.RevertDiscountUsageRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC RevertDiscountUsage called for code: {Code}", request.Code);

            var result = await _discountService.RevertDiscountUsageAsync(request.Code, Guid.Parse(request.ClinicId));

            return new Protos.RevertDiscountUsageResponse
            {
                Success = result,
                Message = result ? "Discount usage reverted successfully" : "Failed to revert discount usage"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC RevertDiscountUsage");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    public override async Task<CalculateDiscountAmountResponse> CalculateDiscountAmount(
        CalculateDiscountAmountRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC CalculateDiscountAmount called for code: {Code}", request.Code);

            var discountAmount = await _discountService.CalculateDiscountAmountAsync(
                request.Code,
                (decimal)request.OriginalAmount,
                Guid.Parse(request.ClinicId),
                !string.IsNullOrEmpty(request.SpecialtyId) ? Guid.Parse(request.SpecialtyId) : null,
                !string.IsNullOrEmpty(request.DoctorId) ? Guid.Parse(request.DoctorId) : null);

            var finalAmount = (decimal)request.OriginalAmount - discountAmount;

            return new CalculateDiscountAmountResponse
            {
                DiscountAmount = (double)discountAmount,
                FinalAmount = (double)finalAmount,
                IsValid = discountAmount > 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CalculateDiscountAmount");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    public override async Task<GetApplicableDiscountsResponse> GetApplicableDiscounts(
        GetApplicableDiscountsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetApplicableDiscounts called for clinic: {ClinicId}", request.ClinicId);

            var discounts = await _discountService.GetApplicableDiscountsAsync(
                Guid.Parse(request.ClinicId),
                !string.IsNullOrEmpty(request.SpecialtyId) ? Guid.Parse(request.SpecialtyId) : null,
                !string.IsNullOrEmpty(request.DoctorId) ? Guid.Parse(request.DoctorId) : null);

            var response = new GetApplicableDiscountsResponse();
            response.Discounts.AddRange(discounts.Select(MapToDiscountInfo));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetApplicableDiscounts");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    public override async Task<IsDiscountValidResponse> IsDiscountValid(
        IsDiscountValidRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC IsDiscountValid called for code: {Code}", request.Code);

            var isValid = await _discountService.IsDiscountValidAsync(
                request.Code,
                Guid.Parse(request.ClinicId),
                !string.IsNullOrEmpty(request.SpecialtyId) ? Guid.Parse(request.SpecialtyId) : null,
                !string.IsNullOrEmpty(request.DoctorId) ? Guid.Parse(request.DoctorId) : null);

            return new IsDiscountValidResponse
            {
                IsValid = isValid
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC IsDiscountValid");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    private static DiscountInfo MapToDiscountInfo(DiscountResponse discount)
    {
        return new DiscountInfo
        {
            Id = discount.Id.ToString(),
            Code = discount.Code,
            Name = discount.Name,
            Description = discount.Description ?? "",
            ClinicId = discount.ClinicId.ToString(),
            SpecialtyId = discount.SpecialtyId?.ToString() ?? "",
            DoctorId = discount.DoctorId?.ToString() ?? "",
            ApplicableTo = discount.ApplicableTo.ToString(),
            Amount = (double)discount.Amount,
            DiscountType = discount.DiscountType.ToString(),
            StartDate = discount.StartDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            EndDate = discount.EndDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MaxUses = discount.MaxUses ?? 0,
            UsesCount = discount.UsesCount,
            Status = discount.Status.ToString()
        };
    }
}
