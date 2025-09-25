using AutoMapper;
using BookingCare.Services.Doctor.Exceptions;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Doctor.Services.Implementations;

public class PositionService : BaseService, IPositionService
{
    private readonly IPositionRepository _repository;
    private readonly IMapper _mapper;

    public PositionService(IPositionRepository repository, IMapper mapper, ILogger<PositionService> logger) : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    #region Position CRUD Operations

    public async Task<PositionResponse> CreatePositionAsync(CreatePositionRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Validate unique constraint
            if (await _repository.PositionNameExistsAsync(request.Name))
            {
                throw PositionConflictException.WithName(request.Name);
            }

            // Create position entity
            var position = _mapper.Map<PositionEntity>(request);
            position.Id = Guid.NewGuid();

            var createdPosition = await _repository.CreatePositionAsync(position);
            return _mapper.Map<PositionResponse>(createdPosition);
        }, nameof(CreatePositionAsync));
    }

    public async Task<PositionResponse?> GetPositionByIdAsync(Guid id)
    {
        var position = await _repository.GetPositionByIdAsync(id);
        return position != null ? _mapper.Map<PositionResponse>(position) : null;
    }

    public async Task<PositionResponse?> GetPositionByNameAsync(string name)
    {
        var position = await _repository.GetPositionByNameAsync(name);
        return position != null ? _mapper.Map<PositionResponse>(position) : null;
    }

    public async Task<PositionResponse> UpdatePositionAsync(UpdatePositionRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Check if position exists
            var existingPosition = await _repository.GetPositionByIdAsync(request.Id);
            if (existingPosition == null)
            {
                throw PositionNotFoundException.WithId(request.Id);
            }

            // Validate unique constraint if name is being updated
            if (!string.IsNullOrEmpty(request.Name) && request.Name != existingPosition.Name &&
                await _repository.PositionNameExistsAsync(request.Name, request.Id))
            {
                throw PositionConflictException.WithName(request.Name);
            }

            // Update position entity
            _mapper.Map(request, existingPosition);
            existingPosition.UpdatedAt = DateTime.UtcNow;

            var updatedPosition = await _repository.UpdatePositionAsync(existingPosition);
            return _mapper.Map<PositionResponse>(updatedPosition);
        }, nameof(UpdatePositionAsync));
    }

    public async Task<bool> DeletePositionAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            return await _repository.DeletePositionAsync(id);
        }, nameof(DeletePositionAsync));
    }

    #endregion

    #region Position Query Operations

    public async Task<PositionListResponse> GetPositionsAsync(PositionQueryRequest query)
    {
        var (positions, totalCount) = await _repository.GetPositionsAsync(query);
        var response = _mapper.Map<PositionListResponse>((positions, totalCount));

        // Set pagination info
        response.PageNumber = query.PageNumber;
        response.PageSize = query.PageSize;
        response.TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        return response;
    }

    public async Task<List<PositionResponse>> GetAllPositionsAsync()
    {
        var positions = await _repository.GetAllPositionsAsync();
        return _mapper.Map<List<PositionResponse>>(positions);
    }

    #endregion

    #region Validation Operations

    public async Task<bool> PositionExistsAsync(Guid id)
    {
        return await _repository.PositionExistsAsync(id);
    }

    public async Task<bool> PositionNameExistsAsync(string name, Guid? excludeId = null)
    {
        return await _repository.PositionNameExistsAsync(name, excludeId);
    }

    #endregion
}
