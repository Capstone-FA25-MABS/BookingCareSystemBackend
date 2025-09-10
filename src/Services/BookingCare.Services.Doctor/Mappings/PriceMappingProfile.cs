using AutoMapper;
using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Mappings;

public class PriceMappingProfile : Profile
{
    public PriceMappingProfile()
    {
        // Price Entity to Response mappings
        CreateMap<PriceEntity, PriceResponse>();

        // Price Request to Entity mappings
        CreateMap<CreatePriceRequest, PriceEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<UpdatePriceRequest, PriceEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        // Collection mappings
        CreateMap<(List<PriceEntity> Prices, int TotalCount), PriceListResponse>()
            .ForMember(dest => dest.Prices, opt => opt.MapFrom(src => src.Prices))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());
    }
}
