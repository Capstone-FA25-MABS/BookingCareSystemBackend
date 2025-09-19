using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Repositories.Interfaces;

public interface IDoctorRepository
{
    // Doctor CRUD operations
    Task<DoctorEntity?> GetDoctorByIdAsync(Guid id);
    Task<DoctorEntity?> GetDoctorByEmailAsync(string email);
    Task<DoctorEntity?> GetDoctorByAccountIdAsync(Guid accountId);
    Task<DoctorEntity> CreateDoctorAsync(DoctorEntity doctor);
    Task<DoctorEntity> UpdateDoctorAsync(DoctorEntity doctor);
    Task<bool> DeleteDoctorAsync(Guid id);
    Task<bool> DoctorExistsAsync(Guid id);
    Task<bool> DoctorEmailExistsAsync(string email, Guid? excludeId = null);
    Task<bool> DoctorAccountExistsAsync(Guid accountId, Guid? excludeId = null);

    // Doctor Query operations
    Task<(List<DoctorEntity> Doctors, int TotalCount)> GetDoctorsAsync(DoctorQueryRequest query);
    Task<List<DoctorEntity>> GetDoctorsByClinicAsync(Guid clinicId);
    Task<List<DoctorEntity>> GetDoctorsBySpecialtyAsync(Guid specialtyId);
    Task<List<DoctorEntity>> GetDoctorsByPositionAsync(Guid positionId);
    Task<List<DoctorEntity>> GetActiveDoctorsAsync();
    IQueryable<DoctorEntity> GetQueryableDoctors();
    Task<List<DoctorEntity>> GetDoctorsByIdsAsync(IEnumerable<Guid> ids);
    Task<List<DoctorEntity>> GetDoctorsByAccountIdsAsync(IEnumerable<Guid> accountIds);

    // DoctorPrice CRUD operations
    Task<DoctorPriceEntity?> GetDoctorPriceAsync(Guid doctorId, Guid priceId);
    Task<DoctorPriceEntity> CreateDoctorPriceAsync(DoctorPriceEntity doctorPrice);
    Task<DoctorPriceEntity> UpdateDoctorPriceAsync(DoctorPriceEntity doctorPrice);
    Task<bool> DeleteDoctorPriceAsync(Guid doctorId, Guid priceId);
    Task<bool> DoctorPriceExistsAsync(Guid doctorId, Guid priceId);

    // DoctorPrice Query operations
    Task<List<DoctorPriceEntity>> GetDoctorPricesAsync(Guid doctorId);
    Task<bool> DeleteAllDoctorPricesAsync(Guid doctorId);

    // Language operations
    Task<List<LanguageEntity>> GetLanguagesAsync();
    Task<LanguageEntity?> GetLanguageByIdAsync(Guid id);
    Task<LanguageEntity> CreateLanguageAsync(LanguageEntity language);
    Task<LanguageEntity> UpdateLanguageAsync(LanguageEntity language);
    Task<bool> DeleteLanguageAsync(Guid id);

    // DoctorLanguage operations
    Task<List<DoctorLanguageEntity>> GetDoctorLanguagesAsync(Guid doctorId);
    Task<DoctorLanguageEntity> CreateDoctorLanguageAsync(DoctorLanguageEntity doctorLanguage);
    Task<bool> DeleteDoctorLanguageAsync(Guid doctorId, Guid languageId);
    Task<bool> DeleteAllDoctorLanguagesAsync(Guid doctorId);

    // ServiceType operations
    Task<List<ServiceTypeEntity>> GetServiceTypesAsync();
    Task<ServiceTypeEntity?> GetServiceTypeByIdAsync(Guid id);
    Task<ServiceTypeEntity> CreateServiceTypeAsync(ServiceTypeEntity serviceType);
    Task<ServiceTypeEntity> UpdateServiceTypeAsync(ServiceTypeEntity serviceType);
    Task<bool> DeleteServiceTypeAsync(Guid id);
}
