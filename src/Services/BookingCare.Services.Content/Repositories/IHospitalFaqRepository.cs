using BookingCare.Services.Content.Models.DTOs;
using BookingCare.Services.Content.Models.Entities;

namespace BookingCare.Services.Content.Repositories;

public interface IHospitalFaqRepository
{
    Task<(IReadOnlyList<HospitalFaqEntity> Faqs, int TotalItems)> GetFaqsAsync(
        HospitalFaqFilterRequest filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HospitalFaqEntity>> GetFaqsByHospitalIdAsync(
        Guid hospitalId,
        CancellationToken cancellationToken = default);

    Task<HospitalFaqEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(HospitalFaqEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(HospitalFaqEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(HospitalFaqEntity entity, CancellationToken cancellationToken = default);
}