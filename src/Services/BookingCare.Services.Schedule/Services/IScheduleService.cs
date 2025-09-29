using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Requests;

namespace BookingCare.Services.Schedule.Services;

/// <summary>
/// Interface for schedule service operations
/// </summary>
public interface IScheduleService
{
    // DoctorDailySchedule operations
    Task<DoctorDailyScheduleDto?> GetDoctorDailyScheduleAsync(long doctorId, DateOnly date);
    Task<IEnumerable<DoctorDailyScheduleDto>> GetDoctorScheduleRangeAsync(GetDoctorScheduleRequest request);
    Task<DoctorDailyScheduleDto> CreateOrUpdateDoctorDailyScheduleAsync(CreateDoctorDailyScheduleRequest request);
    Task DeleteDoctorDailyScheduleAsync(long doctorId, DateOnly date);

    // DoctorScheduleException operations
    Task<IEnumerable<DoctorScheduleExceptionDto>> GetDoctorExceptionsAsync(long doctorId, DateOnly date);
    Task<DoctorScheduleExceptionDto> CreateDoctorScheduleExceptionAsync(CreateDoctorScheduleExceptionRequest request);
    Task DeleteDoctorScheduleExceptionAsync(long id);

    // ClinicException operations
    Task<IEnumerable<ClinicExceptionDto>> GetClinicExceptionsAsync(long clinicId, DateOnly date);
    Task<ClinicExceptionDto> CreateClinicExceptionAsync(CreateClinicExceptionRequest request);
    Task DeleteClinicExceptionAsync(long id);

    // ServiceSchedule operations
    Task<IEnumerable<ServiceScheduleDto>> GetServiceSchedulesAsync(long serviceId);
    Task<ServiceScheduleDto> CreateServiceScheduleAsync(CreateServiceScheduleRequest request);
    Task DeleteServiceScheduleAsync(long id);

    // Available slots operations
    Task<IEnumerable<AppointmentTimeDto>> GetAvailableSlotsAsync(GetAvailableSlotsRequest request);
}