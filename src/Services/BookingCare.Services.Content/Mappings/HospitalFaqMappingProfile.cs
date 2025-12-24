using AutoMapper;
using BookingCare.Services.Content.Models.DTOs;
using BookingCare.Services.Content.Models.Entities;

namespace BookingCare.Services.Content.Mappings;

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


