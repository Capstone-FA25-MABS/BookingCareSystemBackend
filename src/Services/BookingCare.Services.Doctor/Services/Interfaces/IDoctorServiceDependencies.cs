using AutoMapper;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Shared.Common.Services;
using BookingCare.Services.Favorite;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Review.Grpc;
using BookingCare.Services.Hospital;

namespace BookingCare.Services.Doctor.Services.Interfaces;

/// <summary>
/// Container for DoctorService dependencies to reduce constructor parameters
/// </summary>
public interface IDoctorServiceDependencies
{
    IDoctorRepository Repository { get; }
    IPositionRepository PositionRepository { get; }
    ISpecialtyRepository SpecialtyRepository { get; }
    ILocationApiService LocationApiService { get; }
    IMapper Mapper { get; }
    FavoritesService.FavoritesServiceClient FavoritesClient { get; }
    AuthService.AuthServiceClient AuthClient { get; }
    HospitalService.HospitalServiceClient HospitalClient { get; }
    ReviewService.ReviewServiceClient ReviewClient { get; }
}
