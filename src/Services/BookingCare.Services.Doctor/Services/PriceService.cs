using AutoMapper;
using BookingCare.Services.Doctor.Exceptions;
using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories;

namespace BookingCare.Services.Doctor.Services;

public class PriceService : IPriceService
{
    private readonly IPriceRepository _repository;
    private readonly IMapper _mapper;

    public PriceService(IPriceRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    #region Price CRUD Operations

    public async Task<PriceResponse> CreatePriceAsync(CreatePriceRequest request)
    {
        // Create price entity
        var price = _mapper.Map<PriceEntity>(request);
        price.Id = Guid.NewGuid();

        var createdPrice = await _repository.CreatePriceAsync(price);
        return _mapper.Map<PriceResponse>(createdPrice);
    }

    public async Task<PriceResponse?> GetPriceByIdAsync(Guid id)
    {
        var price = await _repository.GetPriceByIdAsync(id);
        return price != null ? _mapper.Map<PriceResponse>(price) : null;
    }

    public async Task<PriceResponse> UpdatePriceAsync(UpdatePriceRequest request)
    {
        // Check if price exists
        var existingPrice = await _repository.GetPriceByIdAsync(request.Id);
        if (existingPrice == null)
        {
            throw PriceNotFoundException.WithId(request.Id);
        }

        // Update price entity
        _mapper.Map(request, existingPrice);

        var updatedPrice = await _repository.UpdatePriceAsync(existingPrice);
        return _mapper.Map<PriceResponse>(updatedPrice);
    }

    public async Task<bool> DeletePriceAsync(Guid id)
    {
        return await _repository.DeletePriceAsync(id);
    }

    #endregion

    #region Price Query Operations

    public async Task<PriceListResponse> GetPricesAsync(PriceQueryRequest query)
    {
        var (prices, totalCount) = await _repository.GetPricesAsync(query);
        var response = _mapper.Map<PriceListResponse>((prices, totalCount));
        
        // Set pagination info
        response.PageNumber = query.PageNumber;
        response.PageSize = query.PageSize;
        response.TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        return response;
    }

    public async Task<List<PriceResponse>> GetAllPricesAsync()
    {
        var prices = await _repository.GetAllPricesAsync();
        return _mapper.Map<List<PriceResponse>>(prices);
    }

    #endregion

    #region Validation Operations

    public async Task<bool> PriceExistsAsync(Guid id)
    {
        return await _repository.PriceExistsAsync(id);
    }

    #endregion
}
