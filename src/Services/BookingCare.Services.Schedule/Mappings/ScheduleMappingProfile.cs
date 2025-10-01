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
        // AppointmentTime enum to DTO mapping with custom logic
        CreateMap<AppointmentTime, AppointmentTimeDto>()
            .ConstructUsing((src, context) => ConvertEnumToDto(src));
    }

    /// <summary>
    /// Converts AppointmentTime enum to AppointmentTimeDto
    /// </summary>
    private static AppointmentTimeDto ConvertEnumToDto(AppointmentTime appointmentTime)
    {
        var (startTime, endTime) = GetTimeStringsFromEnum(appointmentTime);
        return new AppointmentTimeDto
        {
            Id = GenerateDeterministicGuid((int)appointmentTime),
            StartTime = startTime,
            EndTime = endTime
        };
    }

    /// <summary>
    /// Generate a deterministic GUID from an integer value for appointment time slots
    /// </summary>
    private static Guid GenerateDeterministicGuid(int value)
    {
        byte[] guidBytes = new byte[16];
        byte[] valueBytes = BitConverter.GetBytes(value);

        Array.Copy(valueBytes, 0, guidBytes, 0, 4);
        for (int i = 4; i < 16; i++)
        {
            guidBytes[i] = (byte)(0xA0 + (i % 16));
        }

        return new Guid(guidBytes);
    }

    /// <summary>
    /// Gets time strings from AppointmentTime enum
    /// </summary>
    private static (string startTime, string endTime) GetTimeStringsFromEnum(AppointmentTime appointmentTime)
    {
        return appointmentTime switch
        {
            // Range time 30 minutes
            AppointmentTime.AT_08_00_08_30 => ("08:00", "08:30"),
            AppointmentTime.AT_08_30_09_00 => ("08:30", "09:00"),
            AppointmentTime.AT_09_00_09_30 => ("09:00", "09:30"),
            AppointmentTime.AT_09_30_10_00 => ("09:30", "10:00"),
            AppointmentTime.AT_10_00_10_30 => ("10:00", "10:30"),
            AppointmentTime.AT_10_30_11_00 => ("10:30", "11:00"),
            AppointmentTime.AT_11_00_11_30 => ("11:00", "11:30"),
            AppointmentTime.AT_11_30_12_00 => ("11:30", "12:00"),
            AppointmentTime.AT_13_00_13_30 => ("13:00", "13:30"),
            AppointmentTime.AT_13_30_14_00 => ("13:30", "14:00"),
            AppointmentTime.AT_14_00_14_30 => ("14:00", "14:30"),
            AppointmentTime.AT_14_30_15_00 => ("14:30", "15:00"),
            AppointmentTime.AT_15_00_15_30 => ("15:00", "15:30"),
            AppointmentTime.AT_15_30_16_00 => ("15:30", "16:00"),
            AppointmentTime.AT_16_00_16_30 => ("16:00", "16:30"),
            AppointmentTime.AT_16_30_17_00 => ("16:30", "17:00"),
            AppointmentTime.AT_17_00_17_30 => ("17:00", "17:30"),
            AppointmentTime.AT_17_30_18_00 => ("17:30", "18:00"),
            AppointmentTime.AT_18_00_18_30 => ("18:00", "18:30"),
            AppointmentTime.AT_18_30_19_00 => ("18:30", "19:00"),
            AppointmentTime.AT_19_00_19_30 => ("19:00", "19:30"),
            AppointmentTime.AT_19_30_20_00 => ("19:30", "20:00"),
            AppointmentTime.AT_20_00_20_30 => ("20:00", "20:30"),
            AppointmentTime.AT_20_30_21_00 => ("20:30", "21:00"),
            AppointmentTime.AT_21_00_21_30 => ("21:00", "21:30"),
            AppointmentTime.AT_21_30_22_00 => ("21:30", "22:00"),
            AppointmentTime.AT_22_00_22_30 => ("22:00", "22:30"),
            AppointmentTime.AT_22_30_23_00 => ("22:30", "23:00"),

            // Range time one hour
            AppointmentTime.AT_08_00_09_00 => ("08:00", "09:00"),
            AppointmentTime.AT_09_00_10_00 => ("09:00", "10:00"),
            AppointmentTime.AT_10_00_11_00 => ("10:00", "11:00"),
            AppointmentTime.AT_11_00_12_00 => ("11:00", "12:00"),
            AppointmentTime.AT_13_00_14_00 => ("13:00", "14:00"),
            AppointmentTime.AT_14_00_15_00 => ("14:00", "15:00"),
            AppointmentTime.AT_15_00_16_00 => ("15:00", "16:00"),
            AppointmentTime.AT_16_00_17_00 => ("16:00", "17:00"),
            AppointmentTime.AT_17_00_18_00 => ("17:00", "18:00"),
            AppointmentTime.AT_18_00_19_00 => ("18:00", "19:00"),
            AppointmentTime.AT_19_00_20_00 => ("19:00", "20:00"),
            AppointmentTime.AT_20_00_21_00 => ("20:00", "21:00"),
            AppointmentTime.AT_21_00_22_00 => ("21:00", "22:00"),
            AppointmentTime.AT_22_00_23_00 => ("22:00", "23:00"),
            _ => ("Unknown", "Unknown")
        };
    }
}