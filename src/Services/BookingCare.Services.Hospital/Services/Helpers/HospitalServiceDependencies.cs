using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital.Services.Interfaces;

namespace BookingCare.Services.Hospital.Services.Helpers;

/// <summary>
/// Wrapper class for external service dependencies to reduce constructor parameters
/// </summary>
public class HospitalServiceDependencies
{
    public AuthService.AuthServiceClient AuthClient { get; }
    public DoctorService.DoctorServiceClient DoctorClient { get; }
    public ILocationApiService LocationApiService { get; }

    public HospitalServiceDependencies(
        AuthService.AuthServiceClient authClient,
        DoctorService.DoctorServiceClient doctorClient,
        ILocationApiService locationApiService)
    {
        AuthClient = authClient;
        DoctorClient = doctorClient;
        LocationApiService = locationApiService;
    }
}

