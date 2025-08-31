using BookingCare.Services.Doctor.Models.DTOs;

namespace BookingCare.Services.Doctor.Services;

public interface IDoctorService
{
    // Doctor CRUD operations
    Task<DoctorResponse> CreateDoctorAsync(CreateDoctorRequest request);
    Task<DoctorResponse?> GetDoctorByIdAsync(Guid id);
    Task<DoctorResponse?> GetDoctorByEmailAsync(string email);
    Task<DoctorResponse?> GetDoctorByAccountIdAsync(Guid accountId);
    Task<DoctorResponse> UpdateDoctorAsync(UpdateDoctorRequest request);
    Task<bool> DeleteDoctorAsync(Guid id);

    // Doctor Query operations
    Task<DoctorListResponse> GetDoctorsAsync(DoctorQueryRequest query);
    Task<List<DoctorResponse>> GetDoctorsByClinicAsync(Guid clinicId);
    Task<List<DoctorResponse>> GetDoctorsBySpecialtyAsync(Guid specialtyId);
    Task<List<DoctorResponse>> GetDoctorsByPositionAsync(Guid positionId);
    Task<List<DoctorResponse>> GetActiveDoctorsAsync();

    // Position CRUD operations
    Task<PositionResponse> CreatePositionAsync(CreatePositionRequest request);
    Task<PositionResponse?> GetPositionByIdAsync(Guid id);
    Task<PositionResponse?> GetPositionByNameAsync(string name);
    Task<PositionResponse> UpdatePositionAsync(UpdatePositionRequest request);
    Task<bool> DeletePositionAsync(Guid id);

    // Position Query operations
    Task<PositionListResponse> GetPositionsAsync(PositionQueryRequest query);
    Task<List<PositionResponse>> GetAllPositionsAsync();

    // Price CRUD operations
    Task<PriceResponse> CreatePriceAsync(CreatePriceRequest request);
    Task<PriceResponse?> GetPriceByIdAsync(Guid id);
    Task<PriceResponse> UpdatePriceAsync(UpdatePriceRequest request);
    Task<bool> DeletePriceAsync(Guid id);

    // Price Query operations
    Task<PriceListResponse> GetPricesAsync(PriceQueryRequest query);
    Task<List<PriceResponse>> GetAllPricesAsync();

    // DoctorPrice operations
    Task<DoctorPriceResponse> AssignPriceToDoctorAsync(AssignPriceToDoctorRequest request);
    Task<bool> RemovePriceFromDoctorAsync(Guid doctorId, Guid priceId);
    Task<List<PriceResponse>> GetDoctorPricesAsync(Guid doctorId);
    Task<List<DoctorResponse>> GetDoctorsByPriceAsync(Guid priceId);

    // Validation operations
    Task<bool> DoctorExistsAsync(Guid id);
    Task<bool> DoctorEmailExistsAsync(string email, Guid? excludeId = null);
    Task<bool> DoctorAccountExistsAsync(Guid accountId, Guid? excludeId = null);
    Task<bool> PositionExistsAsync(Guid id);
    Task<bool> PositionNameExistsAsync(string name, Guid? excludeId = null);
    Task<bool> PriceExistsAsync(Guid id);
    Task<bool> DoctorPriceExistsAsync(Guid doctorId, Guid priceId);
}
