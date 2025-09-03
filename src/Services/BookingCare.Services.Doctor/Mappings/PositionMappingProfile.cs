using AutoMapper;
using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Mappings;

public class PositionMappingProfile : Profile
{
    public PositionMappingProfile()
    {
        // Position Entity to Response mappings
        CreateMap<PositionEntity, PositionResponse>();

        // Position Request to Entity mappings
        CreateMap<CreatePositionRequest, PositionEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdatePositionRequest, PositionEntity>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Collection mappings
        CreateMap<(List<PositionEntity> Positions, int TotalCount), PositionListResponse>()
            .ForMember(dest => dest.Positions, opt => opt.MapFrom(src => src.Positions))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());
    }
}
