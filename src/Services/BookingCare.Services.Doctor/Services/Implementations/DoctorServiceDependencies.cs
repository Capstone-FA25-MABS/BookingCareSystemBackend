using AutoMapper;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using BookingCare.Services.Favorite;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Review.Grpc;
using BookingCare.Services.Hospital;

namespace BookingCare.Services.Doctor.Services.Implementations;

/// <summary>
/// Implementation of DoctorService dependencies container
/// </summary>
public class DoctorServiceDependencies : IDoctorServiceDependencies
{
    public IDoctorRepository Repository { get; }
    public IPositionRepository PositionRepository { get; }
    public ISpecialtyRepository SpecialtyRepository { get; }
    public ILocationApiService LocationApiService { get; }
    public IMapper Mapper { get; }
    public FavoritesService.FavoritesServiceClient FavoritesClient { get; }
    public AuthService.AuthServiceClient AuthClient { get; }
    public HospitalService.HospitalServiceClient HospitalClient { get; }
    public ReviewService.ReviewServiceClient ReviewClient { get; }

    public DoctorServiceDependencies(
        IDoctorRepository repository,
        IPositionRepository positionRepository,
        ISpecialtyRepository specialtyRepository,
        ILocationApiService locationApiService,
        IMapper mapper,
        FavoritesService.FavoritesServiceClient favoritesClient,
        AuthService.AuthServiceClient authClient,
        HospitalService.HospitalServiceClient hospitalClient,
        ReviewService.ReviewServiceClient reviewClient)
    {
        Repository = repository;
        PositionRepository = positionRepository;
        SpecialtyRepository = specialtyRepository;
        LocationApiService = locationApiService;
        Mapper = mapper;
        FavoritesClient = favoritesClient;
        AuthClient = authClient;
        HospitalClient = hospitalClient;
        ReviewClient = reviewClient;
    }
}
