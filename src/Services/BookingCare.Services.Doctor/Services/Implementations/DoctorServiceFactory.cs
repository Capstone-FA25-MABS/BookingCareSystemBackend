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
/// Factory implementation for creating DoctorService with all required dependencies
/// </summary>
public class DoctorServiceFactory : IDoctorServiceFactory
{
    private readonly IDoctorRepository _repository;
    private readonly IPositionRepository _positionRepository;
    private readonly ISpecialtyRepository _specialtyRepository;
    private readonly ILocationApiService _locationApiService;
    private readonly IMapper _mapper;
    private readonly FavoritesService.FavoritesServiceClient _favoritesClient;
    private readonly AuthService.AuthServiceClient _authClient;
    private readonly HospitalService.HospitalServiceClient _hospitalClient;
    private readonly ReviewService.ReviewServiceClient _reviewClient;
    private readonly ILogger<DoctorService> _logger;

    public DoctorServiceFactory(
        IDoctorRepository repository,
        IPositionRepository positionRepository,
        ISpecialtyRepository specialtyRepository,
        ILocationApiService locationApiService,
        IMapper mapper,
        FavoritesService.FavoritesServiceClient favoritesClient,
        AuthService.AuthServiceClient authClient,
        HospitalService.HospitalServiceClient hospitalClient,
        ReviewService.ReviewServiceClient reviewClient,
        ILogger<DoctorService> logger)
    {
        _repository = repository;
        _positionRepository = positionRepository;
        _specialtyRepository = specialtyRepository;
        _locationApiService = locationApiService;
        _mapper = mapper;
        _favoritesClient = favoritesClient;
        _authClient = authClient;
        _hospitalClient = hospitalClient;
        _reviewClient = reviewClient;
        _logger = logger;
    }

    public IDoctorService CreateDoctorService()
    {
        var dependencies = new DoctorServiceDependencies(
            _repository,
            _positionRepository,
            _specialtyRepository,
            _locationApiService,
            _mapper,
            _favoritesClient,
            _authClient,
            _hospitalClient,
            _reviewClient);

        return new DoctorService(dependencies, _logger);
    }
}
