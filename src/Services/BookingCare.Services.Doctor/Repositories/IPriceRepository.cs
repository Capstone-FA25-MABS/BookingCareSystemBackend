using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Repositories;

public interface IPriceRepository
{
    // Price CRUD operations
    Task<PriceEntity?> GetPriceByIdAsync(Guid id);
    Task<PriceEntity> CreatePriceAsync(PriceEntity price);
    Task<PriceEntity> UpdatePriceAsync(PriceEntity price);
    Task<bool> DeletePriceAsync(Guid id);
    Task<bool> PriceExistsAsync(Guid id);

    // Price Query operations
    Task<(List<PriceEntity> Prices, int TotalCount)> GetPricesAsync(PriceQueryRequest query);
    Task<List<PriceEntity>> GetAllPricesAsync();

    // PriceRule operations
    Task<PriceRuleEntity?> GetActivePriceRuleAsync(
        int? minExperience = null,
        string? position = null);
}
