using AutoMapper;
using BookingCare.Services.HospitalFaq.Models.DTOs;
using BookingCare.Services.HospitalFaq.Models.Entities;

namespace BookingCare.Services.HospitalFaq.Mappings;

public class HospitalFaqMappingProfile : Profile
{
    public HospitalFaqMappingProfile()
    {
        CreateMap<HospitalFaqEntity, HospitalFaqResponse>();
        CreateMap<CreateHospitalFaqRequest, HospitalFaqEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        CreateMap<UpdateHospitalFaqRequest, HospitalFaqEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}

