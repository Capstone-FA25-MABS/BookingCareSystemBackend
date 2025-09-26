using AutoMapper;
using BookingCare.Services.ServiceMedical.Models.DTOs.Requests;
using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;
using BookingCare.Services.ServiceMedical.Models.Entities;

namespace BookingCare.Services.ServiceMedical.Mappings
{
    public class ServiceMedicalMappingProfile : Profile
    {
        public ServiceMedicalMappingProfile()
        {
            // ServiceCategory mappings
            CreateMap<ServiceCategoryEntity, ServiceCategoryResponse>()
                .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children))
                .ForMember(dest => dest.Parent, opt => opt.MapFrom(src => src.Parent));

            CreateMap<CreateServiceCategoryRequest, ServiceCategoryEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "INACTIVE"))
                .ForMember(dest => dest.Parent, opt => opt.Ignore())
                .ForMember(dest => dest.Children, opt => opt.Ignore())
                .ForMember(dest => dest.Services, opt => opt.Ignore());

            CreateMap<UpdateServiceCategoryRequest, ServiceCategoryEntity>()
                .ForMember(dest => dest.Parent, opt => opt.Ignore())
                .ForMember(dest => dest.Children, opt => opt.Ignore())
                .ForMember(dest => dest.Services, opt => opt.Ignore());

            // Service mappings
            CreateMap<ServiceEntity, ServiceResponse>()
                .ForMember(dest => dest.ServiceCategory, opt => opt.MapFrom(src => src.ServiceCategory));

            CreateMap<CreateServiceRequest, ServiceEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "INACTIVE"))
                .ForMember(dest => dest.ServiceCategory, opt => opt.Ignore())
                .ForMember(dest => dest.Schedules, opt => opt.Ignore());

            CreateMap<UpdateServiceRequest, ServiceEntity>()
                .ForMember(dest => dest.ServiceCategory, opt => opt.Ignore())
                .ForMember(dest => dest.Schedules, opt => opt.Ignore());

        }
    }
}
