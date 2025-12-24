using AutoMapper;
using BookingCare.Services.Favorites.Models.DTOs;

using BookingCare.Services.Favorites.Models.Entities;
namespace BookingCare.Services.Favorites.Mappings;

/// <summary>
/// AutoMapper profile for Favorite mappings
/// </summary>
public class FavoriteMappingProfile : Profile
{
    public FavoriteMappingProfile()

    {
        // Request to Entity mappings
        CreateMap<CreateFavoriteRequest, FavoriteEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<ToggleFavoriteRequest, FavoriteEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        // Entity to Response mappings
        CreateMap<FavoriteEntity, FavoriteResponse>();
    }

}