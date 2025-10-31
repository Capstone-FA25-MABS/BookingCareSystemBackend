using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Enums;

namespace BookingCare.Services.Hospital.Repositories.Interfaces;

public interface IHospitalRegistrationRepository
{
    Task<HospitalRegistrationEntity> CreateAsync(HospitalRegistrationEntity registration);
    Task<HospitalRegistrationEntity?> GetByIdAsync(Guid id);
    Task<HospitalRegistrationEntity?> GetByEmailAsync(string email);
    Task<HospitalRegistrationEntity?> GetByTaxCodeAsync(string taxCode);
    Task<(List<HospitalRegistrationEntity> Registrations, int TotalCount)> GetAllAsync(
        string? searchTerm,
        RegistrationStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder);
    Task<HospitalRegistrationEntity> UpdateAsync(HospitalRegistrationEntity registration);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);

}

