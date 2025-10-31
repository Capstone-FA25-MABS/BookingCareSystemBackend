using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Models.DTOs.Requests;

namespace BookingCare.Services.Hospital.Repositories.Interfaces;

public interface IHospitalRegistrationRepository
{
    Task<HospitalRegistrationEntity> CreateAsync(HospitalRegistrationEntity registration);
    Task<HospitalRegistrationEntity?> GetByIdAsync(Guid id);
    Task<HospitalRegistrationEntity?> GetByEmailAsync(string email);
    Task<HospitalRegistrationEntity?> GetByTaxCodeAsync(string taxCode);
    Task<(List<HospitalRegistrationEntity> Registrations, int TotalCount)> GetAllAsync(
        HospitalRegistrationQueryParameters parameters);
    Task<HospitalRegistrationEntity> UpdateAsync(HospitalRegistrationEntity registration);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);

}

