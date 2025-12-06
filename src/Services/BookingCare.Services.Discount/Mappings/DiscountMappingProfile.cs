using AutoMapper;
using BookingCare.Services.Discount.Models.DTOs;
using BookingCare.Services.Discount.Models.Entities;

namespace BookingCare.Services.Discount.Mappings;

public class DiscountMappingProfile : Profile
{
    public DiscountMappingProfile()
    {
        // Entity to Response mappings
        CreateMap<DiscountEntity, DiscountResponse>();

        // Request to Entity mappings
        CreateMap<CreateDiscountRequest, DiscountEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UsesCount, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateDiscountRequest, DiscountEntity>()
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalId, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicableTo, opt => opt.Ignore())
            .ForMember(dest => dest.DiscountType, opt => opt.Ignore())
            .ForMember(dest => dest.UsesCount, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Collection mappings
        CreateMap<(List<DiscountEntity> Discounts, int TotalCount), DiscountListResponse>()
            .ForMember(dest => dest.Discounts, opt => opt.MapFrom(src => src.Discounts))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());
    }
}
