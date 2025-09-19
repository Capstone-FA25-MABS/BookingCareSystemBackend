using AutoMapper;
using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Models.Entities;

namespace BookingCare.Services.Review.Mappings;

/// <summary>
/// AutoMapper profile for Review mappings
/// </summary>
public class ReviewMappingProfile : Profile
{
    public ReviewMappingProfile()
    {
        // Entity to Response mappings
        CreateMap<ReviewEntity, ReviewResponse>();
        CreateMap<ReplyEntity, ReplyResponse>();

        // Request to Entity mappings
        CreateMap<CreateReviewRequest, ReviewEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Replies, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateReviewRequest, ReviewEntity>()
            .ForMember(dest => dest.PatientId, opt => opt.Ignore())
            .ForMember(dest => dest.TargetType, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorId, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicServiceId, opt => opt.Ignore())
            .ForMember(dest => dest.Replies, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<AddReplyRequest, ReplyEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}