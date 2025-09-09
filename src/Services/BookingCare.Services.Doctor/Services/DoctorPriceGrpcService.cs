using BookingCare.Services.Doctor.Protos;
using Grpc.Core;

namespace BookingCare.Services.Doctor.Services;

public class DoctorPriceGrpcService : Protos.DoctorPriceService.DoctorPriceServiceBase
{
    private readonly IDoctorService _doctorService;
    private readonly ILogger<DoctorPriceGrpcService> _logger;

    public DoctorPriceGrpcService(IDoctorService doctorService, ILogger<DoctorPriceGrpcService> logger)
    {
        _doctorService = doctorService;
        _logger = logger;
    }

    public override async Task<AssignPriceToDoctorResponse> AssignPriceToDoctor(AssignPriceToDoctorRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _doctorService.AssignPriceToDoctorAsync(new Models.DTOs.AssignPriceToDoctorRequest
            {
                DoctorId = Guid.Parse(request.DoctorId),
                PriceId = Guid.Parse(request.PriceId),
                Description = request.Description
            });

            return new AssignPriceToDoctorResponse
            {
                Success = true,
                DoctorPrice = new DoctorPriceInfo
                {
                    DoctorId = result.DoctorId.ToString(),
                    PriceId = result.PriceId.ToString(),
                    Description = result.Description ?? string.Empty
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error AssignPriceToDoctor");
            return new AssignPriceToDoctorResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task<RemovePriceFromDoctorResponse> RemovePriceFromDoctor(RemovePriceFromDoctorRequest request, ServerCallContext context)
    {
        try
        {
            var ok = await _doctorService.RemovePriceFromDoctorAsync(Guid.Parse(request.DoctorId), Guid.Parse(request.PriceId));
            return new RemovePriceFromDoctorResponse { Success = ok, ErrorMessage = ok ? string.Empty : "Failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error RemovePriceFromDoctor");
            return new RemovePriceFromDoctorResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task<GetDoctorPricesResponse> GetDoctorPrices(GetDoctorPricesRequest request, ServerCallContext context)
    {
        try
        {
            var prices = await _doctorService.GetDoctorPricesAsync(Guid.Parse(request.DoctorId));
            var resp = new GetDoctorPricesResponse { Success = true };
            foreach (var p in prices)
            {
                resp.Prices.Add(new PriceInfo { Id = p.Id.ToString(), Amount = (double)p.Amount });
            }
            return resp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error GetDoctorPrices");
            return new GetDoctorPricesResponse { Success = false, ErrorMessage = ex.Message };
        }
    }
}


