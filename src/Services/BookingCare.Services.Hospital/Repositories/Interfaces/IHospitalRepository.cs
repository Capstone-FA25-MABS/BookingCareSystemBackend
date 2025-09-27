using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Models.DTOs.Requests;

namespace BookingCare.Services.Hospital.Repositories.Interfaces;

public interface IHospitalRepository
{
    Task<HospitalEntity?> GetByIdAsync(Guid id);
    Task<HospitalEntity?> GetByEmailAsync(string email);
    Task<List<HospitalEntity>> GetAllAsync();
    Task<(List<HospitalEntity> hospitals, int totalCount)> GetFilteredAsync(HospitalFilterRequest filter);
    Task<HospitalEntity> CreateAsync(HospitalEntity hospital);
    Task<HospitalEntity> UpdateAsync(HospitalEntity hospital);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
    Task<List<HospitalEntity>> GetBySpecialtyAsync(Guid specialtyId);
    Task<List<HospitalEntity>> GetByAccountIdAsync(Guid accountId);
}
