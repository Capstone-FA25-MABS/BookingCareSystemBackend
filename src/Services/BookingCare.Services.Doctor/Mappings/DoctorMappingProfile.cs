using AutoMapper;
using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Mappings;

public class DoctorMappingProfile : Profile
{
    public DoctorMappingProfile()
    {
        // Doctor Entity to Response mappings
        CreateMap<DoctorEntity, DoctorResponse>()
            .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
            .ForMember(dest => dest.Prices, opt => opt.MapFrom(src => src.DoctorPrices.Select(dp => dp.Price)));

        // Doctor Request to Entity mappings
        CreateMap<CreateDoctorRequest, DoctorEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Position, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorPrices, opt => opt.Ignore());

        CreateMap<UpdateDoctorRequest, DoctorEntity>()
            .ForMember(dest => dest.AccountId, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Position, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorPrices, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

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

        // Price Entity to Response mappings
        CreateMap<PriceEntity, PriceResponse>()
            .ForMember(dest => dest.Description, opt => opt.Ignore());

        // Price Request to Entity mappings
        CreateMap<CreatePriceRequest, PriceEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<UpdatePriceRequest, PriceEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        // DoctorPrice Entity to Response mappings
        CreateMap<DoctorPriceEntity, DoctorPriceResponse>()
            .ForMember(dest => dest.Doctor, opt => opt.MapFrom(src => src.Doctor))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price));

        // AssignPriceToDoctor Request to Entity mappings
        CreateMap<AssignPriceToDoctorRequest, DoctorPriceEntity>();

        // Collection mappings
        CreateMap<(List<DoctorEntity> Doctors, int TotalCount), DoctorListResponse>()
            .ForMember(dest => dest.Doctors, opt => opt.MapFrom(src => src.Doctors))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());

        CreateMap<(List<PositionEntity> Positions, int TotalCount), PositionListResponse>()
            .ForMember(dest => dest.Positions, opt => opt.MapFrom(src => src.Positions))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());

        CreateMap<(List<PriceEntity> Prices, int TotalCount), PriceListResponse>()
            .ForMember(dest => dest.Prices, opt => opt.MapFrom(src => src.Prices))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());
    }
}
