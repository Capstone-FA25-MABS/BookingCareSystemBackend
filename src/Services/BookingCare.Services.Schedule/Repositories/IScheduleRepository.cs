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

    /// <summary>
    /// Get available slots for multiple doctors in a single batch query (optimized for specialty booking)
    /// Returns a dictionary mapping doctor ID to their available appointment times
    /// </summary>
    Task<Dictionary<Guid, List<AppointmentTime>>> GetAvailableSlotsForDoctorsAsync(List<Guid> doctorIds, DateOnly date);

    // ServiceMedicalDailySchedule operations
    Task<ServiceMedicalDailyScheduleEntity?> GetServiceMedicalDailyScheduleAsync(Guid serviceMedicalId, DateOnly date);
    Task<IEnumerable<ServiceMedicalDailyScheduleEntity>> GetServiceMedicalScheduleRangeAsync(Guid serviceMedicalId, DateOnly startDate, DateOnly endDate);
    Task<ServiceMedicalDailyScheduleEntity> CreateOrUpdateServiceMedicalDailyScheduleAsync(ServiceMedicalDailyScheduleEntity schedule);
    Task DeleteServiceMedicalDailyScheduleAsync(Guid serviceMedicalId, DateOnly date);

    // ServiceMedicalScheduleException operations
    Task<IEnumerable<ServiceMedicalScheduleExceptionEntity>> GetServiceMedicalExceptionsAsync(Guid serviceMedicalId, DateOnly date);
    Task<IEnumerable<ServiceMedicalScheduleExceptionEntity>> GetServiceMedicalExceptionsRangeAsync(Guid serviceMedicalId, DateOnly startDate, DateOnly endDate);
    Task<ServiceMedicalScheduleExceptionEntity> CreateServiceMedicalScheduleExceptionAsync(ServiceMedicalScheduleExceptionEntity exception);
    Task DeleteServiceMedicalScheduleExceptionAsync(Guid id);

    // Available slots for service medical operations
    Task<IEnumerable<AppointmentTime>> GetServiceMedicalAvailableSlotsAsync(Guid serviceMedicalId, DateOnly date);
}