using BookingCare.Services.Favorite;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Review.Grpc;
using BookingCare.Services.Hospital;

namespace BookingCare.Services.Doctor.Services.Interfaces;

/// <summary>
/// Factory interface for creating DoctorService with all required dependencies
/// </summary>
public interface IDoctorServiceFactory
{
    /// <summary>
    /// Creates a DoctorService instance with all required dependencies
    /// </summary>
    IDoctorService CreateDoctorService();
}
