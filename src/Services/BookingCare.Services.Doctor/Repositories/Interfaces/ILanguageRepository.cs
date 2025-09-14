using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Repositories.Interfaces;

public interface ILanguageRepository
{
    // Language CRUD operations
    Task<LanguageEntity?> GetLanguageByIdAsync(Guid id);
    Task<LanguageEntity?> GetLanguageByNameAsync(string name);
    Task<LanguageEntity> CreateLanguageAsync(LanguageEntity language);
    Task<LanguageEntity> UpdateLanguageAsync(LanguageEntity language);
    Task<bool> DeleteLanguageAsync(Guid id);
    Task<bool> LanguageExistsAsync(Guid id);
    Task<bool> LanguageNameExistsAsync(string name, Guid? excludeId = null);

    // Language Query operations
    Task<(List<LanguageEntity> Languages, int TotalCount)> GetLanguagesAsync(LanguageQueryRequest query);
    Task<List<LanguageEntity>> GetAllLanguagesAsync();
    IQueryable<LanguageEntity> GetQueryableLanguages();
}
