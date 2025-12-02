using AutoMapper;
using BookingCare.Services.User.Exceptions;
using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Models.Entities;
using BookingCare.Services.User.Repositories;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.User.Services;

/// <summary>
/// Service implementation for PatientRelative operations
/// </summary>
public class PatientRelativeService : BaseService, IPatientRelativeService
{
    private readonly IPatientRelativeRepository _repository;
    private readonly IMapper _mapper;

    // Maximum number of relatives allowed per user
    private const int MAX_RELATIVES_PER_USER = 10;

    public PatientRelativeService(
        IPatientRelativeRepository repository,
        IMapper mapper,
        ILogger<PatientRelativeService> logger) : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<List<PatientRelativeResponse>> GetMyRelativesAsync(Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateGuid(userId, nameof(userId));
            LogDebug("Getting relatives for user: {UserId}", null, userId);

            var relatives = await _repository.GetByUserIdAsync(userId);
            return _mapper.Map<List<PatientRelativeResponse>>(relatives);
        }, "GetMyRelatives");
    }

    /// <inheritdoc />
    public async Task<List<PatientRelativeBasicResponse>> GetMyRelativesBasicAsync(Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateGuid(userId, nameof(userId));
            LogDebug("Getting relatives basic info for user: {UserId}", null, userId);

            var relatives = await _repository.GetByUserIdAsync(userId);
            return _mapper.Map<List<PatientRelativeBasicResponse>>(relatives);
        }, "GetMyRelativesBasic");
    }

    /// <inheritdoc />
    public async Task<PatientRelativeResponse?> GetRelativeByIdAsync(Guid id, Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateGuid(id, nameof(id));
            ValidateGuid(userId, nameof(userId));
            LogDebug("Getting relative {RelativeId} for user {UserId}", null, id, userId);

            var relative = await _repository.GetByIdAndUserIdAsync(id, userId);
            return relative != null ? _mapper.Map<PatientRelativeResponse>(relative) : null;
        }, "GetRelativeById");
    }

    /// <inheritdoc />
    public async Task<PatientRelativeResponse> CreateRelativeAsync(Guid userId, CreatePatientRelativeRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateGuid(userId, nameof(userId));
            ValidateRequired(request, nameof(request));
            LogInfo("Creating relative for user: {UserId}", null, userId);

            // Check limit
            var currentCount = await _repository.CountByUserIdAsync(userId);
            if (currentCount >= MAX_RELATIVES_PER_USER)
            {
                throw new UserException($"Bạn chỉ có thể thêm tối đa {MAX_RELATIVES_PER_USER} người thân");
            }

            // Create entity using AutoMapper
            var entity = _mapper.Map<PatientRelativeEntity>(request);
            entity.UserId = userId;

            var created = await _repository.CreateAsync(entity);

            LogInfo("User {UserId} created relative {RelativeId} ({RelativeName})", null, userId, created.Id, created.FullName);
            return _mapper.Map<PatientRelativeResponse>(created);
        }, "CreateRelative");
    }

    /// <inheritdoc />
    public async Task<PatientRelativeResponse> UpdateRelativeAsync(Guid id, Guid userId, UpdatePatientRelativeRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateGuid(id, nameof(id));
            ValidateGuid(userId, nameof(userId));
            ValidateRequired(request, nameof(request));
            LogInfo("Updating relative {RelativeId} for user {UserId}", null, id, userId);

            var existing = await _repository.GetByIdAndUserIdAsync(id, userId);
            if (existing == null)
            {
                throw new PatientRelativeNotFoundException(id);
            }

            // Update entity using AutoMapper (preserves Id, UserId, CreatedAt)
            _mapper.Map(request, existing);

            var updated = await _repository.UpdateAsync(existing);

            LogInfo("User {UserId} updated relative {RelativeId}", null, userId, id);
            return _mapper.Map<PatientRelativeResponse>(updated);
        }, "UpdateRelative");
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRelativeAsync(Guid id, Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateGuid(id, nameof(id));
            ValidateGuid(userId, nameof(userId));
            LogInfo("Deleting relative {RelativeId} for user {UserId}", null, id, userId);

            var existing = await _repository.GetByIdAndUserIdAsync(id, userId);
            if (existing == null)
            {
                throw new PatientRelativeNotFoundException(id);
            }

            var deleted = await _repository.DeleteAsync(id);

            LogInfo("User {UserId} deleted relative {RelativeId}", null, userId, id);
            return deleted;
        }, "DeleteRelative");
    }

    /// <inheritdoc />
    public async Task<List<PatientRelativeResponse>> GetRelativesByIdsAsync(List<Guid> ids)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            if (ids == null || !ids.Any())
            {
                return new List<PatientRelativeResponse>();
            }

            LogDebug("Getting relatives by IDs: {Count} IDs", null, ids.Count);

            var relatives = await _repository.GetByIdsAsync(ids);
            return _mapper.Map<List<PatientRelativeResponse>>(relatives);
        }, "GetRelativesByIds");
    }
}
