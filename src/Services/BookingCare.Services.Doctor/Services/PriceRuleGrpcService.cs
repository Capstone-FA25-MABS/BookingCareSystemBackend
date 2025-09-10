using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Doctor.Repositories;
using Grpc.Core;

namespace BookingCare.Services.Doctor.Services;

public class PriceRuleGrpcService : Protos.PriceRuleService.PriceRuleServiceBase
{
    private readonly IPriceRepository _priceRepository;
    private readonly ILogger<PriceRuleGrpcService> _logger;

    public PriceRuleGrpcService(IPriceRepository priceRepository, ILogger<PriceRuleGrpcService> logger)
    {
        _priceRepository = priceRepository;
        _logger = logger;
    }

    public override async Task<GetActivePriceRuleResponse> GetActivePriceRule(GetActivePriceRuleRequest request, ServerCallContext context)
    {
        try
        {
            var rule = await _priceRepository.GetActivePriceRuleAsync(
                minExperience: request.MinExperience > 0 ? request.MinExperience : null,
                position: string.IsNullOrWhiteSpace(request.Position) ? null : request.Position);

            if (rule == null)
            {
                return new GetActivePriceRuleResponse { Success = true };
            }

            return new GetActivePriceRuleResponse
            {
                Success = true,
                Rule = new PriceRuleInfo
                {
                    Id = rule.Id.ToString(),
                    Name = rule.Name,
                    BasePrice = (double)rule.BasePrice,
                    MinExperience = rule.MinExperience ?? 0,
                    Position = rule.Position ?? string.Empty,
                    Status = rule.Status.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error GetActivePriceRule");
            return new GetActivePriceRuleResponse { Success = false, ErrorMessage = ex.Message };
        }
    }
}


