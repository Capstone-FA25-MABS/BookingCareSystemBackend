using BookingCare.Services.Content.Models.DTOs;

namespace BookingCare.Services.Content.Services;

public interface IHospitalFaqService
{
    Task<HospitalFaqListResponse> GetFaqsAsync(
        HospitalFaqFilterRequest filter,
        CancellationToken cancellationToken = default);

    Task<List<HospitalFaqResponse>> GetFaqsByHospitalIdAsync(
        Guid hospitalId,
        CancellationToken cancellationToken = default);

    Task<HospitalFaqResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<HospitalFaqResponse> CreateFaqAsync(
        CreateHospitalFaqRequest request,
        Guid createdBy,
        CancellationToken cancellationToken = default);

    Task<HospitalFaqResponse> UpdateFaqAsync(
        Guid id,
        UpdateHospitalFaqRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteFaqAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}


