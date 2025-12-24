using AutoMapper;
using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Models.Entities;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.User.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // ============ User Mappings ============
        // Entity to Response mappings - explicit mapping with all computed properties ignored
        CreateMap<UserEntity, UserResponse>()
            .ForMember(dest => dest.FullName, opt => opt.Ignore()); // bỏ qua computed property

        // Request to Entity mappings
        CreateMap<CreateUserRequest, UserEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateUserRequest, UserEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AccountId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // ============ PatientRelative Mappings ============
        // Entity to Response
        CreateMap<PatientRelativeEntity, PatientRelativeResponse>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.DateOfBirth)))
            .ForMember(dest => dest.GenderDisplay, opt => opt.MapFrom(src => GetGenderDisplay(src.Gender)))
            .ForMember(dest => dest.RelationshipDisplay, opt => opt.MapFrom(src => GetRelationshipDisplay(src.Relationship)));

        // Entity to Basic Response
        CreateMap<PatientRelativeEntity, PatientRelativeBasicResponse>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.DateOfBirth)))
            .ForMember(dest => dest.RelationshipDisplay, opt => opt.MapFrom(src => GetRelationshipDisplay(src.Relationship)));

        // Request to Entity - use base class mapping to avoid duplication
        CreateMap<PatientRelativeRequestBase, PatientRelativeEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Trim()))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Trim()))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone != null ? src.Phone.Trim() : null))
            .ForMember(dest => dest.HealthInsuranceNumber, opt => opt.MapFrom(src => src.HealthInsuranceNumber != null ? src.HealthInsuranceNumber.Trim() : null))
            .ForMember(dest => dest.IdentityNumber, opt => opt.MapFrom(src => src.IdentityNumber != null ? src.IdentityNumber.Trim() : null))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes != null ? src.Notes.Trim() : null))
            .IncludeAllDerived();

        // Derived mappings inherit from base
        CreateMap<CreatePatientRelativeRequest, PatientRelativeEntity>();
        CreateMap<UpdatePatientRelativeRequest, PatientRelativeEntity>();
    }

    #region Helper Methods

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }

    private static string GetGenderDisplay(Gender gender)
    {
        return gender switch
        {
            Gender.MALE => "Nam",
            Gender.FEMALE => "Nữ",
            Gender.OTHER => "Khác",
            _ => "Không xác định"
        };
    }

    private static string GetRelationshipDisplay(Relationship relationship)
    {
        return relationship switch
        {
            Relationship.PARENT => "Cha/Mẹ",
            Relationship.CHILD => "Con",
            Relationship.SPOUSE => "Vợ/Chồng",
            Relationship.SIBLING => "Anh/Chị/Em",
            Relationship.GRANDPARENT => "Ông/Bà",
            Relationship.GRANDCHILD => "Cháu",
            Relationship.OTHER => "Khác",
            _ => "Không xác định"
        };
    }

    #endregion
}
