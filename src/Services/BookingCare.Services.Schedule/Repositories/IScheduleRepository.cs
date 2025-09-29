using BookingCare.Services.Schedule.Models.Entities;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Schedule.Repositories;

/// <summary>
/// Interface for schedule repository operations
/// </summary>
public interface IScheduleRepository
{


    // DoctorDailySchedule operations
    Task<DoctorDailyScheduleEntity?> GetDoctorDailyScheduleAsync(long doctorId, DateOnly date);
    Task<IEnumerable<DoctorDailyScheduleEntity>> GetDoctorScheduleRangeAsync(long doctorId, DateOnly startDate, DateOnly endDate);
    Task<DoctorDailyScheduleEntity> CreateOrUpdateDoctorDailyScheduleAsync(DoctorDailyScheduleEntity schedule);
    Task DeleteDoctorDailyScheduleAsync(long doctorId, DateOnly date);

    // DoctorScheduleException operations
    Task<IEnumerable<DoctorScheduleExceptionEntity>> GetDoctorExceptionsAsync(long doctorId, DateOnly date);
    Task<IEnumerable<DoctorScheduleExceptionEntity>> GetDoctorExceptionsRangeAsync(long doctorId, DateOnly startDate, DateOnly endDate);
    Task<DoctorScheduleExceptionEntity> CreateDoctorScheduleExceptionAsync(DoctorScheduleExceptionEntity exception);
    Task DeleteDoctorScheduleExceptionAsync(long id);

    // ClinicException operations
    Task<IEnumerable<ClinicExceptionEntity>> GetClinicExceptionsAsync(long clinicId, DateOnly date);
    Task<ClinicExceptionEntity> CreateClinicExceptionAsync(ClinicExceptionEntity exception);
    Task DeleteClinicExceptionAsync(long id);

    // ServiceSchedule operations
    Task<IEnumerable<ServiceScheduleEntity>> GetServiceSchedulesAsync(long serviceId);
    Task<ServiceScheduleEntity> CreateServiceScheduleAsync(ServiceScheduleEntity serviceSchedule);
    Task DeleteServiceScheduleAsync(long id);

    // Available slots operations
    Task<IEnumerable<AppointmentTime>> GetAvailableSlotsAsync(long doctorId, DateOnly date, long? serviceId = null);
}