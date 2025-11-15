using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

public interface IHospitalRegistrationService
{
    Task<HospitalRegistrationResponseDto> CreateRegistrationAsync(CreateHospitalRegistrationRequestDto request);
    Task<HospitalRegistrationResponseDto> GetRegistrationByIdAsync(Guid id);
    Task<HospitalRegistrationListResponseDto> GetAllRegistrationsAsync(HospitalRegistrationFilterRequestDto filter);
    Task<HospitalRegistrationResponseDto> UpdateRegistrationStatusAsync(Guid id, UpdateRegistrationStatusRequestDto request);
    Task<HospitalRegistrationResponseDto> UpdateRegistrationAsync(Guid id, UpdateRegistrationRequestDto request);
    Task<bool> DeleteRegistrationAsync(Guid id);
    Task<HospitalRegistrationResponseDto> ApproveRegistrationAsync(Guid id, ApproveRegistrationRequestDto request);
    Task<HospitalRegistrationResponseDto> RejectRegistrationAsync(Guid id, RejectRegistrationRequestDto request);
}

