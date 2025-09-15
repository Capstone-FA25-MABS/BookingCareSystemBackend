using AutoMapper;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Mappings;

/// <summary>
/// AutoMapper profile cho Payment Service
/// </summary>
public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        // Payment mappings
        CreateMap<CreatePaymentRequest, PaymentEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore());

        // Appointment Payment mappings
        CreateMap<CreateAppointmentPaymentRequest, PaymentEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicId, opt => opt.MapFrom(src => (Guid?)null))
            .ForMember(dest => dest.SubscriptionId, opt => opt.MapFrom(src => (Guid?)null))
            .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => TransactionType.APPOINTMENT))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => PaymentStatus.PENDING))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore());

        // Subscription Payment mappings
        CreateMap<CreateSubscriptionPaymentRequest, PaymentEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AppointmentId, opt => opt.MapFrom(src => (Guid?)null))
            .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => (Guid?)null))
            .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => TransactionType.SUBSCRIPTION))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => PaymentStatus.PENDING))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore());

        CreateMap<PaymentEntity, PaymentResponse>()
            .ForMember(dest => dest.PaymentMethodName, opt => opt.MapFrom(src => src.PaymentMethod.Name));

        // PaymentMethod mappings
        CreateMap<PaymentMethodEntity, PaymentMethodResponse>();
    }
}