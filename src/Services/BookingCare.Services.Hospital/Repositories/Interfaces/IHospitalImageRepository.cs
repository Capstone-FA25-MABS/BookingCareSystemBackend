using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Models.DTOs.Requests;

namespace BookingCare.Services.Hospital.Repositories.Interfaces;

public interface IHospitalImageRepository
{
    Task<HospitalImageEntity?> GetByIdAsync(Guid id);
    Task<List<HospitalImageEntity>> GetAllAsync();
    Task<(List<HospitalImageEntity> images, int totalCount)> GetFilteredAsync(HospitalImageFilterRequest filter);
    Task<HospitalImageEntity> CreateAsync(HospitalImageEntity image);
    Task<HospitalImageEntity> UpdateAsync(HospitalImageEntity image);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<List<HospitalImageEntity>> GetByHospitalIdAsync(Guid hospitalId);
    Task<bool> DeleteByHospitalIdAsync(Guid hospitalId);
}
