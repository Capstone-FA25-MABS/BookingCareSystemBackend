using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

public interface IHospitalService
{
    Task<HospitalDetailResponse?> GetByIdAsync(Guid id);
    Task<HospitalResponse?> GetByEmailAsync(string email);
    Task<HospitalListResponse> GetAllAsync();
    Task<HospitalListResponse> GetFilteredAsync(HospitalFilterRequest filter);
    Task<HospitalDetailResponse> CreateAsync(CreateHospitalRequest request);
    Task<HospitalResponse> UpdateAsync(Guid id, UpdateHospitalRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<List<HospitalResponse>> GetBySpecialtyAsync(Guid specialtyId);
    Task<List<HospitalResponse>> GetByAccountIdAsync(Guid accountId);
    Task<bool> AddSpecialtyAsync(Guid hospitalId, Guid specialtyId);
    Task<bool> RemoveSpecialtyAsync(Guid hospitalId, Guid specialtyId);
}
