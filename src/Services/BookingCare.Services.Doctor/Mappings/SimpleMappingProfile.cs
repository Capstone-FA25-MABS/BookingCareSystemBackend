using AutoMapper;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Mappings;

public class SimpleMappingProfile : Profile
{
    public SimpleMappingProfile()
    {
        // Specialty Entity to Simple Response mappings
        CreateMap<SpecialtyEntity, SpecialtySimpleResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl));

        // Position Entity to Simple Response mappings
        CreateMap<PositionEntity, PositionSimpleResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.DoctorCount, opt => opt.Ignore()); // Will be set manually

        // Language Entity to Simple Response mappings
        CreateMap<LanguageEntity, LanguageSimpleResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

        // ServiceType Entity to Simple Response mappings
        CreateMap<ServiceTypeEntity, ServiceTypeSimpleResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl));
    }
}
