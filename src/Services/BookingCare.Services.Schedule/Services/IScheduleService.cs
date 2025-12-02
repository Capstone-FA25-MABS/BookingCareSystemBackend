using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Requests;

namespace BookingCare.Services.Schedule.Services;

/// <summary>
/// Interface for schedule service operations
/// </summary>
public interface IScheduleService
{
    // DoctorDailySchedule operations
    Task<DoctorDailyScheduleDto?> GetDoctorDailyScheduleAsync(Guid doctorId, DateOnly date);
    Task<IEnumerable<DoctorDailyScheduleDto>> GetDoctorScheduleRangeAsync(GetDoctorScheduleRequest request);
    Task<DoctorDailyScheduleDto> CreateOrUpdateDoctorDailyScheduleAsync(CreateDoctorDailyScheduleRequest request);
    Task DeleteDoctorDailyScheduleAsync(Guid doctorId, DateOnly date);

    // DoctorScheduleException operations
    Task<IEnumerable<DoctorScheduleExceptionDto>> GetDoctorExceptionsAsync(Guid doctorId, DateOnly date);
    Task<List<DoctorScheduleExceptionDto>> CreateDoctorScheduleExceptionAsync(CreateDoctorScheduleExceptionRequest request);
    Task DeleteDoctorScheduleExceptionAsync(Guid id);

    // ClinicException operations
    Task<IEnumerable<ClinicExceptionDto>> GetClinicExceptionsAsync(Guid clinicId, DateOnly date);
    Task<ClinicExceptionDto> CreateClinicExceptionAsync(CreateClinicExceptionRequest request);
    Task DeleteClinicExceptionAsync(Guid id);

    // ServiceSchedule operations
    Task<IEnumerable<ServiceScheduleDto>> GetServiceSchedulesAsync(Guid serviceId);
    Task<ServiceScheduleDto> CreateServiceScheduleAsync(CreateServiceScheduleRequest request);
    Task DeleteServiceScheduleAsync(Guid id);

    // Available slots operations
    Task<IEnumerable<AppointmentTimeDto>> GetAvailableSlotsAsync(GetAvailableSlotsRequest request, Guid? currentUserId = null);

    // ServiceMedicalDailySchedule operations
    Task<ServiceMedicalDailyScheduleDto?> GetServiceMedicalDailyScheduleAsync(Guid serviceMedicalId, DateOnly date);
    Task<IEnumerable<ServiceMedicalDailyScheduleDto>> GetServiceMedicalScheduleRangeAsync(GetServiceMedicalScheduleRequest request);
    Task<ServiceMedicalDailyScheduleDto> CreateOrUpdateServiceMedicalDailyScheduleAsync(CreateServiceMedicalDailyScheduleRequest request);
    Task DeleteServiceMedicalDailyScheduleAsync(Guid serviceMedicalId, DateOnly date);

    // ServiceMedicalScheduleException operations
    Task<IEnumerable<ServiceMedicalScheduleExceptionDto>> GetServiceMedicalExceptionsAsync(Guid serviceMedicalId, DateOnly date);
    Task<List<ServiceMedicalScheduleExceptionDto>> CreateServiceMedicalScheduleExceptionAsync(CreateServiceMedicalScheduleExceptionRequest request);
    Task DeleteServiceMedicalScheduleExceptionAsync(Guid id);

    // Available slots for service medical operations
    Task<IEnumerable<AppointmentTimeDto>> GetServiceMedicalAvailableSlotsAsync(GetServiceMedicalAvailableSlotsRequest request, Guid? currentUserId = null);

    // Specialty available slots operations (for "hospital assigns doctor" mode)
    // Aggregates availability across all doctors in the specialty
    Task<SpecialtyAvailableSlotsResponseDto> GetSpecialtyAvailableSlotsAsync(GetSpecialtyAvailableSlotsRequest request, Guid? currentUserId = null);
}