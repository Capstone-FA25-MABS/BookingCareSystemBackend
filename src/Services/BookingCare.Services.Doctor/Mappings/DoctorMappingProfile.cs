using AutoMapper;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Mappings;

public class DoctorMappingProfile : Profile
{
    public DoctorMappingProfile()
    {
        // Doctor Entity to Response mappings
        CreateMap<DoctorEntity, DoctorResponse>()
            .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
            .ForMember(dest => dest.Prices, opt => opt.MapFrom(src => src.DoctorPrices))
            .ForMember(dest => dest.Languages, opt => opt.MapFrom(src => src.DoctorLanguages.Select(dl => dl.Language)))
            .ForMember(dest => dest.Status, opt => opt.Ignore()) // Status sẽ được set bởi EnrichDoctorsWithStatusAsync
            .ForMember(dest => dest.IsFavorited, opt => opt.Ignore()) // IsFavorited sẽ được set bởi logic khác
            .ForMember(dest => dest.Hospital, opt => opt.Ignore()); // Hospital sẽ được set bởi EnrichDoctorsWithHospitalBasicInfoAsync

        CreateMap<DoctorEntity, DoctorDetailResponse>()
            .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
            .ForMember(dest => dest.Prices, opt => opt.MapFrom(src => src.DoctorPrices))
            .ForMember(dest => dest.Languages, opt => opt.MapFrom(src => src.DoctorLanguages.Select(dl => dl.Language)))
            .ForMember(dest => dest.Status, opt => opt.Ignore()) // Status sẽ được set bởi EnrichDoctorsWithStatusAsync
            .ForMember(dest => dest.IsFavorited, opt => opt.Ignore()) // IsFavorited sẽ được set bởi logic khác
            .ForMember(dest => dest.Hospital, opt => opt.Ignore()); // Hospital sẽ được set bởi EnrichDoctorWithHospitalDetailInfoAsync

        // Doctor Request to Entity mappings
        CreateMap<CreateDoctorRequest, DoctorEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Position, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorPrices, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorLanguages, opt => opt.Ignore());

        CreateMap<UpdateDoctorRequest, DoctorEntity>()
            .ForMember(dest => dest.AccountId, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Position, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorPrices, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorLanguages, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // DoctorPrice Entity to Response mappings
        CreateMap<DoctorPriceEntity, DoctorPriceResponse>()
            .ForMember(dest => dest.ServiceTypeName, opt => opt.MapFrom(src => src.ServiceType.Name));

        // Language Entity to Response mappings
        CreateMap<LanguageEntity, LanguageResponse>();

        // Language Request to Entity mappings
        CreateMap<CreateLanguageRequest, LanguageEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorLanguages, opt => opt.Ignore());

        CreateMap<UpdateLanguageRequest, LanguageEntity>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorLanguages, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // ServiceType Entity to Response mappings
        CreateMap<ServiceTypeEntity, ServiceTypeResponse>();

        // ServiceType Request to Entity mappings
        CreateMap<CreateServiceTypeRequest, ServiceTypeEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorPrices, opt => opt.Ignore());

        CreateMap<UpdateServiceTypeRequest, ServiceTypeEntity>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorPrices, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Collection mappings
        CreateMap<(List<DoctorEntity> Doctors, int TotalCount), DoctorListResponse>()
            .ForMember(dest => dest.Doctors, opt => opt.MapFrom(src => src.Doctors))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());

        CreateMap<(List<LanguageEntity> Languages, int TotalCount), LanguageListResponse>()
            .ForMember(dest => dest.Languages, opt => opt.MapFrom(src => src.Languages))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());

        CreateMap<(List<ServiceTypeEntity> ServiceTypes, int TotalCount), ServiceTypeListResponse>()
            .ForMember(dest => dest.ServiceTypes, opt => opt.MapFrom(src => src.ServiceTypes))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());
    }
}
