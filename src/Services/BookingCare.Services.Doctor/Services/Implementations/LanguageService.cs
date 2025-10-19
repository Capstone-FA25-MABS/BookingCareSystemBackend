using AutoMapper;
using BookingCare.Services.Doctor.Exceptions;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Doctor.Services.Implementations;

public class LanguageService : BaseService, ILanguageService
{
    private readonly ILanguageRepository _repository;
    private readonly IMapper _mapper;

    public LanguageService(ILanguageRepository repository, IMapper mapper, ILogger<LanguageService> logger) : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    #region Language CRUD Operations

    public async Task<LanguageResponse> CreateLanguageAsync(CreateLanguageRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Validate unique name
            if (await _repository.LanguageNameExistsAsync(request.Name))
            {
                throw new ArgumentException($"Language with name '{request.Name}' already exists");
            }

            // Create language entity
            var language = _mapper.Map<LanguageEntity>(request);
            language.Id = Guid.NewGuid();

            var createdLanguage = await _repository.CreateLanguageAsync(language);
            var response = _mapper.Map<LanguageResponse>(createdLanguage);
            return response;
        }, nameof(CreateLanguageAsync));
    }

    public async Task<LanguageResponse?> GetLanguageByIdAsync(Guid id)
    {
        var language = await _repository.GetLanguageByIdAsync(id);
        return language != null ? _mapper.Map<LanguageResponse>(language) : null;
    }

    public async Task<LanguageResponse?> GetLanguageByNameAsync(string name)
    {
        var language = await _repository.GetLanguageByNameAsync(name);
        return language != null ? _mapper.Map<LanguageResponse>(language) : null;
    }

    public async Task<LanguageResponse> UpdateLanguageAsync(UpdateLanguageRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Check if language exists
            var existingLanguage = await _repository.GetLanguageByIdAsync(request.Id);
            if (existingLanguage == null)
            {
                throw new ArgumentException($"Language with ID {request.Id} not found");
            }

            // Validate unique name (exclude current language)
            if (await _repository.LanguageNameExistsAsync(request.Name, request.Id))
            {
                throw new ArgumentException($"Language with name '{request.Name}' already exists");
            }

            // Update language entity
            _mapper.Map(request, existingLanguage);
            existingLanguage.UpdatedAt = DateTime.UtcNow;

            var updatedLanguage = await _repository.UpdateLanguageAsync(existingLanguage);
            var response = _mapper.Map<LanguageResponse>(updatedLanguage);
            return response;
        }, nameof(UpdateLanguageAsync));
    }

    public async Task<bool> DeleteLanguageAsync(Guid id)
    {
        // Check if language exists
        if (!await _repository.LanguageExistsAsync(id))
        {
            return false;
        }

        // Check if language is being used by any doctor
        // This would require a method to check doctor_languages table
        // For now, we'll allow deletion and let database constraints handle it

        return await _repository.DeleteLanguageAsync(id);
    }

    public async Task<bool> ToggleLanguageStatusAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var language = await _repository.GetLanguageByIdAsync(id);
            if (language == null)
            {
                throw new ArgumentException($"Language with ID {id} not found");
            }

            // Toggle status: ACTIVE -> INACTIVE, INACTIVE -> ACTIVE
            language.Status = language.Status == Status.ACTIVE ? Status.INACTIVE : Status.ACTIVE;
            language.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateLanguageAsync(language);
            return true;
        }, nameof(ToggleLanguageStatusAsync));
    }

    #endregion

    #region Language Query Operations

    public async Task<LanguageListResponse> GetLanguagesAsync(LanguageQueryRequest query)
    {
        var (languages, totalCount) = await _repository.GetLanguagesAsync(query);
        var response = _mapper.Map<LanguageListResponse>((languages, totalCount));

        // Set pagination info
        response.PageNumber = query.PageNumber;
        response.PageSize = query.PageSize;
        response.TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        return response;
    }

    public async Task<List<LanguageResponse>> GetAllLanguagesAsync()
    {
        var languages = await _repository.GetAllLanguagesAsync();
        return _mapper.Map<List<LanguageResponse>>(languages);
    }

    public async Task<List<LanguageSimpleResponse>> GetActiveLanguagesSimpleAsync()
    {
        var languages = await _repository.GetActiveLanguagesSimpleAsync();
        return _mapper.Map<List<LanguageSimpleResponse>>(languages);
    }

    #endregion

    #region Validation Operations

    public async Task<bool> LanguageExistsAsync(Guid id)
    {
        return await _repository.LanguageExistsAsync(id);
    }

    public async Task<bool> LanguageNameExistsAsync(string name, Guid? excludeId = null)
    {
        return await _repository.LanguageNameExistsAsync(name, excludeId);
    }

    #endregion
}
