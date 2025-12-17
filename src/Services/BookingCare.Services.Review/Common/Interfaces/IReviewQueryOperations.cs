using BookingCare.Services.Review.Models.DTOs;

namespace BookingCare.Services.Review.Common.Interfaces;

/// <summary>
/// Common interface for review query operations shared between service and repository layers
/// </summary>
public interface IReviewQueryOperations
{
    /// <summary>
    /// Gets reviews with filtering and pagination
    /// </summary>
    /// <param name="request">The filter and pagination parameters</param>
    /// <returns>Paginated reviews</returns>
    Task<PagedReviewsResponse> GetReviewsAsync(GetReviewsRequest request);

    /// <summary>
    /// Gets reviews for a specific doctor
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="minRating">Minimum rating filter</param>
    /// <param name="maxRating">Maximum rating filter</param>
    /// <returns>Paginated reviews for the doctor</returns>
    Task<PagedReviewsResponse> GetReviewsByDoctorAsync(
        Guid doctorId,
        int page = 1,
        int pageSize = 10,
        int? minRating = null,
        int? maxRating = null
    );

    /// <summary>
    /// Gets reviews for a specific service
    /// </summary>
    /// <param name="serviceId">The service ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews for the service</returns>
    Task<PagedReviewsResponse> GetReviewsByServiceAsync(
        Guid serviceId,
        int page = 1,
        int pageSize = 10
    );

    /// <summary>
    /// Gets reviews by a specific patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews by the patient</returns>
    Task<PagedReviewsResponse> GetReviewsByPatientAsync(
        Guid patientId,
        int page = 1,
        int pageSize = 10
    );

    /// <summary>
    /// Gets reviews for a specific hospital
    /// </summary>
    /// <param name="hospitalId">The hospital ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="minRating">Minimum rating filter</param>
    /// <param name="maxRating">Maximum rating filter</param>
    /// <returns>Paginated reviews for the hospital</returns>
    Task<PagedReviewsResponse> GetReviewsByHospitalAsync(
        Guid hospitalId,
        int page = 1,
        int pageSize = 10,
        int? minRating = null,
        int? maxRating = null
    );

    /// <summary>
    /// Gets the average rating for a doctor
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>Average rating</returns>
    Task<double> GetAverageRatingByDoctorAsync(Guid doctorId);

    /// <summary>
    /// Gets the average rating for a service
    /// </summary>
    /// <param name="serviceId">The service ID</param>
    /// <returns>Average rating</returns>
    Task<double> GetAverageRatingByServiceAsync(Guid serviceId);

    /// <summary>
    /// Gets the total count of reviews for a doctor
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>Review count</returns>
    Task<long> GetReviewCountByDoctorAsync(Guid doctorId);

    /// <summary>
    /// Gets the total count of reviews for a service
    /// </summary>
    /// <param name="serviceId">The service ID</param>
    /// <returns>Review count</returns>
    Task<long> GetReviewCountByServiceAsync(Guid serviceId);

    /// <summary>
    /// Gets optimized statistics for a doctor
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>Review statistics without rating distribution</returns>
    Task<ReviewStatisticsResponse> GetDoctorStatisticsAsync(Guid doctorId);

    /// <summary>
    /// Gets optimized statistics for a service
    /// </summary>
    /// <param name="serviceId">The service ID</param>
    /// <returns>Review statistics without rating distribution</returns>
    Task<ReviewStatisticsResponse> GetServiceStatisticsAsync(Guid serviceId);

    /// <summary>
    /// Gets detailed statistics with rating distribution for a doctor
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>Detailed review statistics including rating distribution</returns>
    Task<ReviewDetailedStatisticsResponse> GetDoctorDetailedStatisticsAsync(Guid doctorId);

    /// <summary>
    /// Gets detailed statistics with rating distribution for a service
    /// </summary>
    /// <param name="serviceId">The service ID</param>
    /// <returns>Detailed review statistics including rating distribution</returns>
    Task<ReviewDetailedStatisticsResponse> GetServiceDetailedStatisticsAsync(Guid serviceId);

    /// <summary>
    /// Gets the average rating for a hospital
    /// </summary>
    /// <param name="hospitalId">The hospital ID</param>
    /// <returns>Average rating</returns>
    Task<double> GetAverageRatingByHospitalAsync(Guid hospitalId);

    /// <summary>
    /// Gets the total count of reviews for a hospital
    /// </summary>
    /// <param name="hospitalId">The hospital ID</param>
    /// <returns>Review count</returns>
    Task<long> GetReviewCountByHospitalAsync(Guid hospitalId);

    /// <summary>
    /// Gets optimized statistics for a hospital
    /// </summary>
    /// <param name="hospitalId">The hospital ID</param>
    /// <returns>Review statistics without rating distribution</returns>
    Task<ReviewStatisticsResponse> GetHospitalStatisticsAsync(Guid hospitalId);

    /// <summary>
    /// Gets detailed statistics with rating distribution for a hospital
    /// </summary>
    /// <param name="hospitalId">The hospital ID</param>
    /// <returns>Detailed review statistics including rating distribution</returns>
    Task<ReviewDetailedStatisticsResponse> GetHospitalDetailedStatisticsAsync(Guid hospitalId);
}
