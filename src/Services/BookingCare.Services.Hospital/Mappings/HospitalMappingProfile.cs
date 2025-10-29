using AutoMapper;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Mappings;

public class HospitalMappingProfile : Profile
{
    public HospitalMappingProfile()
    {
        // Hospital mappings
        CreateMap<HospitalEntity, HospitalResponse>()
            .ForMember(dest => dest.Specialties, opt => opt.MapFrom(src => src.HospitalSpecialties))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.HospitalImages))
            .ForMember(dest => dest.CurrentSubscription, opt => opt.MapFrom(src =>
                src.HospitalSubscriptions.FirstOrDefault(s => s.Status == BookingCare.Services.Hospital.Enums.SubscriptionStatus.ACTIVE)));

        // Hospital Simple Response mapping for performance optimization
        CreateMap<HospitalEntity, HospitalSimpleResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl));

        // Hospital List Optimized Response mapping for UI display
        CreateMap<HospitalEntity, HospitalListOptimizedResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl))
            .ForMember(dest => dest.Specialties, opt => opt.Ignore()) // Will be populated by service layer
            .ForMember(dest => dest.TotalSpecialties, opt => opt.Ignore()); // Will be populated by service layer

        CreateMap<HospitalEntity, HospitalDetailResponse>()
            .ForMember(dest => dest.Specialties, opt => opt.MapFrom(src => src.HospitalSpecialties))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.HospitalImages))
            .ForMember(dest => dest.CurrentSubscription, opt => opt.MapFrom(src =>
                src.HospitalSubscriptions.FirstOrDefault(s => s.Status == BookingCare.Services.Hospital.Enums.SubscriptionStatus.ACTIVE)))
            .ForMember(dest => dest.SubscriptionHistory, opt => opt.MapFrom(src => src.HospitalSubscriptions));

        // Hospital Profile mapping (exclude accountId, createdAt, updatedAt)
        CreateMap<HospitalEntity, HospitalProfileResponse>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.HospitalImages))
            .ForMember(dest => dest.Specialties, opt => opt.Ignore());

        CreateMap<HospitalImageEntity, HospitalImageSimpleResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl));

        CreateMap<CreateHospitalRequest, HospitalEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalSpecialties, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalImages, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalSubscriptions, opt => opt.Ignore());

        CreateMap<UpdateHospitalRequest, HospitalEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AccountId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalSpecialties, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalImages, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalSubscriptions, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Hospital Specialty mappings
        CreateMap<HospitalSpecialtyEntity, HospitalSpecialtyResponse>();

        // Hospital Image mappings
        CreateMap<HospitalImageEntity, HospitalImageResponse>();
        CreateMap<CreateHospitalImageRequest, HospitalImageEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Hospital, opt => opt.Ignore());

        CreateMap<UpdateHospitalImageRequest, HospitalImageEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Hospital, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Subscription Plan mappings
        CreateMap<SubscriptionPlanEntity, SubscriptionPlanResponse>();
        CreateMap<SubscriptionPlanEntity, SubscriptionPlanDetailResponse>()
            .ForMember(dest => dest.HospitalSubscriptions, opt => opt.MapFrom(src => src.HospitalSubscriptions))
            .ForMember(dest => dest.ActiveSubscriptionsCount, opt => opt.MapFrom(src =>
                src.HospitalSubscriptions.Count(s => s.Status == BookingCare.Services.Hospital.Enums.SubscriptionStatus.ACTIVE)));

        CreateMap<CreateSubscriptionPlanRequest, SubscriptionPlanEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalSubscriptions, opt => opt.Ignore());

        CreateMap<UpdateSubscriptionPlanRequest, SubscriptionPlanEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalSubscriptions, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Hospital Subscription mappings
        CreateMap<HospitalSubscriptionEntity, HospitalSubscriptionResponse>()
            .ForMember(dest => dest.Hospital, opt => opt.MapFrom(src => src.Hospital))
            .ForMember(dest => dest.SubscriptionPlan, opt => opt.MapFrom(src => src.SubscriptionPlan));

        CreateMap<CreateHospitalSubscriptionRequest, HospitalSubscriptionEntity>()
            .ForMember(dest => dest.HospitalSubscriptionId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Hospital, opt => opt.Ignore())
            .ForMember(dest => dest.SubscriptionPlan, opt => opt.Ignore());

        CreateMap<UpdateHospitalSubscriptionRequest, HospitalSubscriptionEntity>()
            .ForMember(dest => dest.HospitalSubscriptionId, opt => opt.Ignore())
            .ForMember(dest => dest.HospitalId, opt => opt.Ignore())
            .ForMember(dest => dest.SubscriptionId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Hospital, opt => opt.Ignore())
            .ForMember(dest => dest.SubscriptionPlan, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}
