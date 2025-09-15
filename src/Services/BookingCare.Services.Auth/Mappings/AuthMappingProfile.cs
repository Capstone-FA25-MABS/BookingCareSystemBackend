using AutoMapper;
using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Models.Entities;

namespace BookingCare.Services.Auth.Mappings;

/// <summary>
/// AutoMapper profile for Auth service entities and DTOs
/// </summary>
public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        // Entity to Response mappings
        CreateMap<AccountEntity, AccountResponse>();
        CreateMap<RoleEntity, RoleResponse>();
        CreateMap<PermissionEntity, PermissionResponse>();
        CreateMap<AccountRoleEntity, AccountRoleResponse>();
        CreateMap<RolePermissionEntity, RolePermissionResponse>();

        // Request to Entity mappings
        CreateMap<CreateRoleRequest, RoleEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore());

        CreateMap<UpdateRoleRequest, RoleEntity>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<CreatePermissionRequest, PermissionEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore());

        CreateMap<UpdatePermissionRequest, PermissionEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Collection mappings
        CreateMap<(List<RoleEntity> Roles, int TotalCount), RoleListResponse>()
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());

        CreateMap<(List<PermissionEntity> Permissions, int TotalCount), PermissionListResponse>()
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore());

        // Authentication mappings
        CreateMap<RegisterRequest, AccountEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber));
    }
}
