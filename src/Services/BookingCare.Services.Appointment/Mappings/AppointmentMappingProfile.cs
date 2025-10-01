using AutoMapper;
using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Services.Appointment.Models.Entities;
using BookingCare.Services.Appointment.Enums;

namespace BookingCare.Services.Appointment.Mappings;

/// <summary>
/// AutoMapper profile for Appointment service mappings
/// </summary>
public class AppointmentMappingProfile : Profile
{
    public AppointmentMappingProfile()
    {
        // Appointment mappings
        CreateMap<CreateAppointmentRequest, AppointmentEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => AppointmentStatus.PENDING))
            .ForMember(dest => dest.Result, opt => opt.Ignore());

        CreateMap<AppointmentEntity, AppointmentResponse>()
            .ForMember(dest => dest.PatientInfo, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorInfo, opt => opt.Ignore())
            .ForMember(dest => dest.ServiceInfo, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalInfo, opt => opt.Ignore());
    }
}
