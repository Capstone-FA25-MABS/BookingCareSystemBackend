using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;

namespace BookingCare.Services.Doctor.Services.Interfaces;

public interface ILanguageService
{
    // Language CRUD operations
    Task<LanguageResponse> CreateLanguageAsync(CreateLanguageRequest request);
    Task<LanguageResponse?> GetLanguageByIdAsync(Guid id);
    Task<LanguageResponse?> GetLanguageByNameAsync(string name);
    Task<LanguageResponse> UpdateLanguageAsync(UpdateLanguageRequest request);
    Task<bool> DeleteLanguageAsync(Guid id);
    Task<bool> ToggleLanguageStatusAsync(Guid id);

    // Language Query operations
    Task<LanguageListResponse> GetLanguagesAsync(LanguageQueryRequest query);
    Task<List<LanguageResponse>> GetAllLanguagesAsync();

    // Optimized methods for simple responses
    Task<List<LanguageSimpleResponse>> GetActiveLanguagesSimpleAsync();

    // Validation operations
    Task<bool> LanguageExistsAsync(Guid id);
    Task<bool> LanguageNameExistsAsync(string name, Guid? excludeId = null);
}
