using BookingCare.Services.Doctor.Models.DTOs;

namespace BookingCare.Services.Doctor.Services;

public interface IPriceService
{
    // Price CRUD operations
    Task<PriceResponse> CreatePriceAsync(CreatePriceRequest request);
    Task<PriceResponse?> GetPriceByIdAsync(Guid id);
    Task<PriceResponse> UpdatePriceAsync(UpdatePriceRequest request);
    Task<bool> DeletePriceAsync(Guid id);

    // Price Query operations
    Task<PriceListResponse> GetPricesAsync(PriceQueryRequest query);
    Task<List<PriceResponse>> GetAllPricesAsync();

    // Validation operations
    Task<bool> PriceExistsAsync(Guid id);
}
