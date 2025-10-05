using AutoMapper;
using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Entities;
using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Schedule.Mappings;

/// <summary>
/// AutoMapper profile for Schedule service entities and DTOs
/// </summary>
public class ScheduleMappingProfile : Profile
{
    public ScheduleMappingProfile()
    {
        ConfigureDoctorDailyScheduleMappings();
        ConfigureDoctorScheduleExceptionMappings();
        ConfigureClinicExceptionMappings();
        ConfigureServiceScheduleMappings();
        ConfigureAppointmentTimeMappings();
    }

    private void ConfigureDoctorDailyScheduleMappings()
    {
        // Entity to DTO mapping
        CreateMap<DoctorDailyScheduleEntity, DoctorDailyScheduleDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
            .ForMember(dest => dest.ScheduleDate, opt => opt.MapFrom(src => src.ScheduleDate))
            .ForMember(dest => dest.SchedulePatterns, opt => opt.MapFrom(src => src.SchedulePatterns))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        // Request to Entity mapping
        CreateMap<CreateDoctorDailyScheduleRequest, DoctorDailyScheduleEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
            .ForMember(dest => dest.ScheduleDate, opt => opt.MapFrom(src => src.ScheduleDate))
            .ForMember(dest => dest.SchedulePatterns, opt => opt.MapFrom(src => src.SchedulePatterns));
    }

    private void ConfigureDoctorScheduleExceptionMappings()
    {
        // Entity to DTO mapping
        CreateMap<DoctorScheduleExceptionEntity, DoctorScheduleExceptionDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
            .ForMember(dest => dest.ExceptionDate, opt => opt.MapFrom(src => src.ExceptionDate))
            .ForMember(dest => dest.AppointmentTime, opt => opt.MapFrom(src => src.AppointmentTime))
            .ForMember(dest => dest.ExceptionType, opt => opt.MapFrom(src => src.ExceptionType.ToString()))
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.IsAvailable))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // Request to Entity mapping for creating exceptions
        CreateMap<CreateDoctorScheduleExceptionRequest, DoctorScheduleExceptionEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
            .ForMember(dest => dest.ExceptionDate, opt => opt.MapFrom(src => src.ExceptionDate))
            .ForMember(dest => dest.AppointmentTime, opt => opt.Ignore()) // Will be set manually for each appointment time
            .ForMember(dest => dest.ExceptionType, opt => opt.MapFrom(src => src.ExceptionType))
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.IsAvailable))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason));
    }

    private void ConfigureClinicExceptionMappings()
    {
        // Entity to DTO mapping
        CreateMap<ClinicExceptionEntity, ClinicExceptionDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ClinicId, opt => opt.MapFrom(src => src.ClinicId))
            .ForMember(dest => dest.ExceptionDate, opt => opt.MapFrom(src => src.ExceptionDate))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason));

        // Request to Entity mapping
        CreateMap<CreateClinicExceptionRequest, ClinicExceptionEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicId, opt => opt.MapFrom(src => src.ClinicId))
            .ForMember(dest => dest.ExceptionDate, opt => opt.MapFrom(src => src.ExceptionDate))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason));
    }

    private void ConfigureServiceScheduleMappings()
    {
        // Entity to DTO mapping
        CreateMap<ServiceScheduleEntity, ServiceScheduleDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ServiceId, opt => opt.MapFrom(src => src.ServiceId))
            .ForMember(dest => dest.SchedulePatterns, opt => opt.MapFrom(src => src.SchedulePatterns))
            .ForMember(dest => dest.ClinicId, opt => opt.MapFrom(src => src.ClinicId))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        // Request to Entity mapping
        CreateMap<CreateServiceScheduleRequest, ServiceScheduleEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ServiceId, opt => opt.MapFrom(src => src.ServiceId))
            .ForMember(dest => dest.SchedulePatterns, opt => opt.MapFrom(src => src.SchedulePatterns))
            .ForMember(dest => dest.ClinicId, opt => opt.MapFrom(src => src.ClinicId));
    }

    private void ConfigureAppointmentTimeMappings()
    {
        // AppointmentTime enum to DTO mapping using shared helper
        CreateMap<AppointmentTime, AppointmentTimeDto>()
            .ConstructUsing((src, context) => BookingCare.Services.Schedule.Utilities.AppointmentTimeHelper.ConvertEnumToDto(src));
    }
}