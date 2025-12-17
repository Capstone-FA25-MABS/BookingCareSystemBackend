using AutoMapper;
using BookingCare.Services.Content.Exceptions;
using BookingCare.Services.Content.Models.DTOs;
using BookingCare.Services.Content.Models.Entities;
using BookingCare.Services.Content.Repositories;

namespace BookingCare.Services.Content.Services;

public class HospitalFaqService : IHospitalFaqService
{
    private readonly IHospitalFaqRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<HospitalFaqService> _logger;

    public HospitalFaqService(
        IHospitalFaqRepository repository,
        IMapper mapper,
        ILogger<HospitalFaqService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<HospitalFaqListResponse> GetFaqsAsync(
        HospitalFaqFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var (faqs, totalItems) = await _repository.GetFaqsAsync(filter, cancellationToken);
        var faqDtos = _mapper.Map<List<HospitalFaqResponse>>(faqs);

        return new HospitalFaqListResponse
        {
            Items = faqDtos,
            TotalCount = totalItems,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<HospitalFaqResponse>> GetFaqsByHospitalIdAsync(
        Guid hospitalId,
        CancellationToken cancellationToken = default)
    {
        var faqs = await _repository.GetFaqsByHospitalIdAsync(hospitalId, cancellationToken);
        return _mapper.Map<List<HospitalFaqResponse>>(faqs);
    }

    public async Task<HospitalFaqResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var faq = await _repository.GetByIdAsync(id, cancellationToken);
        return faq == null ? null : _mapper.Map<HospitalFaqResponse>(faq);
    }

    public async Task<HospitalFaqResponse> CreateFaqAsync(
        CreateHospitalFaqRequest request,
        Guid createdBy,
        CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<HospitalFaqEntity>(request);
        entity.CreatedBy = createdBy;

        await _repository.AddAsync(entity, cancellationToken);
        _logger.LogInformation("Created FAQ {FaqId} for hospital {HospitalId} by user {CreatedBy}", entity.Id, entity.HospitalId, createdBy);

        return _mapper.Map<HospitalFaqResponse>(entity);
    }

    public async Task<HospitalFaqResponse> UpdateFaqAsync(
        Guid id,
        UpdateHospitalFaqRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            throw new HospitalFaqNotFoundException(id);
        }

        _mapper.Map(request, entity);
        await _repository.UpdateAsync(entity, cancellationToken);
        _logger.LogInformation("Updated FAQ {FaqId}", id);

        return _mapper.Map<HospitalFaqResponse>(entity);
    }

    public async Task DeleteFaqAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            throw new HospitalFaqNotFoundException(id);
        }

        await _repository.DeleteAsync(entity, cancellationToken);
        _logger.LogInformation("Deleted FAQ {FaqId}", id);
    }
}


