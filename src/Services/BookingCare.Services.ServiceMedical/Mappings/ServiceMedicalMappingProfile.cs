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

            // Admin response mapping (flat list without navigation properties)
            CreateMap<ServiceCategoryEntity, ServiceCategoryAdminResponse>();
            CreateMap<ServiceCategoryResponse, ServiceCategoryAdminResponse>();

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

            CreateMap<ServiceResponse, ServiceWithHospitalResponse>()
                .ForMember(dest => dest.Hospital, opt => opt.Ignore());

            // Direct mapping from ServiceEntity to ServiceWithHospitalResponse
            CreateMap<ServiceEntity, ServiceWithHospitalResponse>()
                .ForMember(dest => dest.ServiceCategory, opt => opt.Ignore())
                .ForMember(dest => dest.Hospital, opt => opt.Ignore());

            CreateMap<ServiceResponse, ServiceOptimizedResponse>()
                .ForMember(dest => dest.ParentCategoryName, opt => opt.MapFrom(src => src.ServiceCategory != null && src.ServiceCategory.Parent != null ? src.ServiceCategory.Parent.Name : null))
                .ForMember(dest => dest.Hospital, opt => opt.Ignore());

            CreateMap<CreateServiceRequest, ServiceEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "INACTIVE"))
                .ForMember(dest => dest.ServiceCategory, opt => opt.Ignore());

            CreateMap<UpdateServiceRequest, ServiceEntity>()
                .ForMember(dest => dest.ServiceCategory, opt => opt.Ignore());

            // Optimized mappings for better performance
            CreateMap<ServiceEntity, ServiceOptimizedResponse>()
                .ForMember(dest => dest.ParentCategoryName, opt => opt.MapFrom(src => src.ServiceCategory != null && src.ServiceCategory.Parent != null ? src.ServiceCategory.Parent.Name : null))
                .ForMember(dest => dest.Hospital, opt => opt.Ignore());

            CreateMap<HospitalInfoResponse, HospitalBasicInfo>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl));

        }
    }
}
