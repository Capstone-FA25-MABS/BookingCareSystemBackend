using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Repositories;

public interface IDoctorRepository
{
    // Doctor CRUD operations
    Task<DoctorEntity?> GetDoctorByIdAsync(Guid id);
    Task<DoctorEntity?> GetDoctorByEmailAsync(string email);
    Task<DoctorEntity?> GetDoctorByAccountIdAsync(Guid accountId);
    Task<DoctorEntity> CreateDoctorAsync(DoctorEntity doctor);
    Task<DoctorEntity> UpdateDoctorAsync(DoctorEntity doctor);
    Task<bool> DeleteDoctorAsync(Guid id);
    Task<bool> DoctorExistsAsync(Guid id);
    Task<bool> DoctorEmailExistsAsync(string email, Guid? excludeId = null);
    Task<bool> DoctorAccountExistsAsync(Guid accountId, Guid? excludeId = null);

    // Doctor Query operations
    Task<(List<DoctorEntity> Doctors, int TotalCount)> GetDoctorsAsync(DoctorQueryRequest query);
    Task<List<DoctorEntity>> GetDoctorsByClinicAsync(Guid clinicId);
    Task<List<DoctorEntity>> GetDoctorsBySpecialtyAsync(Guid specialtyId);
    Task<List<DoctorEntity>> GetDoctorsByPositionAsync(Guid positionId);
    Task<List<DoctorEntity>> GetActiveDoctorsAsync();

    // Position CRUD operations
    Task<PositionEntity?> GetPositionByIdAsync(Guid id);
    Task<PositionEntity?> GetPositionByNameAsync(string name);
    Task<PositionEntity> CreatePositionAsync(PositionEntity position);
    Task<PositionEntity> UpdatePositionAsync(PositionEntity position);
    Task<bool> DeletePositionAsync(Guid id);
    Task<bool> PositionExistsAsync(Guid id);
    Task<bool> PositionNameExistsAsync(string name, Guid? excludeId = null);

    // Position Query operations
    Task<(List<PositionEntity> Positions, int TotalCount)> GetPositionsAsync(PositionQueryRequest query);
    Task<List<PositionEntity>> GetAllPositionsAsync();

    // Price CRUD operations
    Task<PriceEntity?> GetPriceByIdAsync(Guid id);
    Task<PriceEntity> CreatePriceAsync(PriceEntity price);
    Task<PriceEntity> UpdatePriceAsync(PriceEntity price);
    Task<bool> DeletePriceAsync(Guid id);
    Task<bool> PriceExistsAsync(Guid id);

    // Price Query operations
    Task<(List<PriceEntity> Prices, int TotalCount)> GetPricesAsync(PriceQueryRequest query);
    Task<List<PriceEntity>> GetAllPricesAsync();

    // DoctorPrice CRUD operations
    Task<DoctorPriceEntity?> GetDoctorPriceAsync(Guid doctorId, Guid priceId);
    Task<DoctorPriceEntity> CreateDoctorPriceAsync(DoctorPriceEntity doctorPrice);
    Task<bool> DeleteDoctorPriceAsync(Guid doctorId, Guid priceId);
    Task<bool> DoctorPriceExistsAsync(Guid doctorId, Guid priceId);

    // DoctorPrice Query operations
    Task<List<DoctorPriceEntity>> GetDoctorPricesAsync(Guid doctorId);
    Task<List<PriceEntity>> GetDoctorPricesByDoctorIdAsync(Guid doctorId);
    Task<List<DoctorEntity>> GetDoctorsByPriceIdAsync(Guid priceId);
}
