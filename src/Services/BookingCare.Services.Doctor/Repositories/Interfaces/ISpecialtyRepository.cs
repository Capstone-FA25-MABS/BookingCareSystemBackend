using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Repositories.Interfaces;

public interface ISpecialtyRepository
{
    // Specialty CRUD operations
    Task<SpecialtyEntity?> GetSpecialtyByIdAsync(Guid id);
    Task<SpecialtyEntity?> GetSpecialtyByNameAsync(string name);
    Task<SpecialtyEntity> CreateSpecialtyAsync(SpecialtyEntity specialty);
    Task<SpecialtyEntity> UpdateSpecialtyAsync(SpecialtyEntity specialty);
    Task<bool> DeleteSpecialtyAsync(Guid id);
    Task<bool> SpecialtyExistsAsync(Guid id);
    Task<bool> SpecialtyNameExistsAsync(string name, Guid? excludeId = null);

    // Specialty Query operations
    Task<(List<SpecialtyEntity> Specialties, int TotalCount)> GetSpecialtiesAsync(SpecialtyQueryRequest query);
    Task<List<SpecialtyEntity>> GetAllSpecialtiesAsync();
    Task<List<SpecialtyEntity>> GetActiveSpecialtiesAsync();
    Task<List<SpecialtyEntity>> GetSpecialtiesByIdsAsync(List<Guid> ids);

    // Optimized methods for simple responses
    Task<List<SpecialtyEntity>> GetActiveSpecialtiesSimpleAsync();
    Task<int> GetDoctorCountBySpecialtyIdAsync(Guid specialtyId);
}
