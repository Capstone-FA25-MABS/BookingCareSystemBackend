using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.Entities;

namespace BookingCare.Services.Doctor.Repositories.Interfaces;

public interface IPositionRepository
{
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
}
