using AutoMapper;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Mappings;

public class LanguageMappingProfile : Profile
{
    public LanguageMappingProfile()
    {
        // Language Entity to Response mappings
        CreateMap<LanguageEntity, LanguageResponse>();

        // Language Request to Entity mappings
        CreateMap<CreateLanguageRequest, LanguageEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateLanguageRequest, LanguageEntity>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Collection mappings
        CreateMap<(List<LanguageEntity> Languages, int TotalCount), LanguageListResponse>()
            .ForMember(dest => dest.Languages, opt => opt.MapFrom(src => src.Languages))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());
    }
}
