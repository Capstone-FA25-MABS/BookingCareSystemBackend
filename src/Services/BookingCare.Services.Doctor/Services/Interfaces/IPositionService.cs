using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;

namespace BookingCare.Services.Doctor.Services.Interfaces;

public interface IPositionService
{
    // Position CRUD operations
    Task<PositionResponse> CreatePositionAsync(CreatePositionRequest request);
    Task<PositionResponse?> GetPositionByIdAsync(Guid id);
    Task<PositionResponse?> GetPositionByNameAsync(string name);
    Task<PositionResponse> UpdatePositionAsync(UpdatePositionRequest request);
    Task<bool> DeletePositionAsync(Guid id);
    Task<bool> TogglePositionStatusAsync(Guid id);

    // Position Query operations
    Task<PositionListResponse> GetPositionsAsync(PositionQueryRequest query);
    Task<List<PositionResponse>> GetAllPositionsAsync();

    // Optimized methods for simple responses
    Task<List<PositionSimpleResponse>> GetActivePositionsSimpleAsync();

    // Validation operations
    Task<bool> PositionExistsAsync(Guid id);
    Task<bool> PositionNameExistsAsync(string name, Guid? excludeId = null);
}
