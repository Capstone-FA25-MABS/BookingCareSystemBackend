using BookingCare.Services.HospitalFaq.Models.DTOs;
using BookingCare.Services.HospitalFaq.Models.Entities;

namespace BookingCare.Services.HospitalFaq.Repositories;

public interface IHospitalFaqRepository
{
    Task<(IReadOnlyList<HospitalFaqEntity> Faqs, int TotalItems)> GetFaqsAsync(HospitalFaqFilterRequest filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HospitalFaqEntity>> GetFaqsByHospitalIdAsync(Guid hospitalId, CancellationToken cancellationToken = default);
    Task<HospitalFaqEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(HospitalFaqEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(HospitalFaqEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(HospitalFaqEntity entity, CancellationToken cancellationToken = default);
}

