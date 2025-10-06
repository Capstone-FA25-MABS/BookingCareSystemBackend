using BookingCare.Services.Schedule.Models.Entities;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Schedule.Repositories;

/// <summary>
/// Interface for schedule repository operations
/// </summary>
public interface IScheduleRepository
{


    // DoctorDailySchedule operations
    Task<DoctorDailyScheduleEntity?> GetDoctorDailyScheduleAsync(Guid doctorId, DateOnly date);
    Task<IEnumerable<DoctorDailyScheduleEntity>> GetDoctorScheduleRangeAsync(Guid doctorId, DateOnly startDate, DateOnly endDate);
    Task<DoctorDailyScheduleEntity> CreateOrUpdateDoctorDailyScheduleAsync(DoctorDailyScheduleEntity schedule);
    Task DeleteDoctorDailyScheduleAsync(Guid doctorId, DateOnly date);

    // DoctorScheduleException operations
    Task<IEnumerable<DoctorScheduleExceptionEntity>> GetDoctorExceptionsAsync(Guid doctorId, DateOnly date);
    Task<IEnumerable<DoctorScheduleExceptionEntity>> GetDoctorExceptionsRangeAsync(Guid doctorId, DateOnly startDate, DateOnly endDate);
    Task<DoctorScheduleExceptionEntity> CreateDoctorScheduleExceptionAsync(DoctorScheduleExceptionEntity exception);
    Task DeleteDoctorScheduleExceptionAsync(Guid id);

    // ClinicException operations
    Task<IEnumerable<ClinicExceptionEntity>> GetClinicExceptionsAsync(Guid clinicId, DateOnly date);
    Task<ClinicExceptionEntity> CreateClinicExceptionAsync(ClinicExceptionEntity exception);
    Task DeleteClinicExceptionAsync(Guid id);

    // ServiceSchedule operations
    Task<IEnumerable<ServiceScheduleEntity>> GetServiceSchedulesAsync(Guid serviceId);
    Task<ServiceScheduleEntity> CreateServiceScheduleAsync(ServiceScheduleEntity serviceSchedule);
    Task DeleteServiceScheduleAsync(Guid id);

    // Available slots operations
    Task<IEnumerable<AppointmentTime>> GetAvailableSlotsAsync(Guid doctorId, DateOnly date, Guid? serviceId = null);
}