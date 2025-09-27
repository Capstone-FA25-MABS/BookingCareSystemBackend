using AutoMapper;
using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Models.Entities;

namespace BookingCare.Services.User.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
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
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}
