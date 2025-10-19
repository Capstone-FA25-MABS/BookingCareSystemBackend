using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;

namespace BookingCare.Services.Doctor.Services.Interfaces;

public interface ISpecialtyService
{
    // Specialty CRUD operations
    Task<SpecialtyResponse> CreateSpecialtyAsync(CreateSpecialtyRequest request);
    Task<SpecialtyResponse?> GetSpecialtyByIdAsync(Guid id);
    Task<SpecialtyResponse?> GetSpecialtyByNameAsync(string name);
    Task<SpecialtyResponse> UpdateSpecialtyAsync(UpdateSpecialtyRequest request);
    Task<bool> DeleteSpecialtyAsync(Guid id);
    Task<bool> ToggleSpecialtyStatusAsync(Guid id);

    // Specialty Query operations
    Task<SpecialtyListResponse> GetSpecialtiesAsync(SpecialtyQueryRequest query);
    Task<List<SpecialtyResponse>> GetAllSpecialtiesAsync();
    Task<List<SpecialtyResponse>> GetActiveSpecialtiesAsync();

    // Optimized methods for simple responses
    Task<List<SpecialtySimpleResponse>> GetActiveSpecialtiesSimpleAsync();

    // Validation operations
    Task<bool> SpecialtyExistsAsync(Guid id);
    Task<bool> SpecialtyNameExistsAsync(string name, Guid? excludeId = null);
}
