using AutoMapper;
using BookingCare.Services.Doctor.Exceptions;
using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories;

namespace BookingCare.Services.Doctor.Services;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _repository;
    private readonly IMapper _mapper;

    public DoctorService(IDoctorRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    #region Doctor CRUD Operations

    public async Task<DoctorResponse> CreateDoctorAsync(CreateDoctorRequest request)
    {
        // Validate unique constraints
        if (await _repository.DoctorEmailExistsAsync(request.Email))
        {
            throw new DoctorConflictException(request.Email);
        }

        if (await _repository.DoctorAccountExistsAsync(request.AccountId))
        {
            throw new DoctorConflictException(request.AccountId);
        }

        // Validate position exists if provided
        if (request.PositionId.HasValue && !await _repository.PositionExistsAsync(request.PositionId.Value))
        {
            throw new PositionNotFoundException(request.PositionId.Value);
        }

        // Create doctor entity
        var doctor = _mapper.Map<DoctorEntity>(request);
        doctor.Id = Guid.NewGuid();

        var createdDoctor = await _repository.CreateDoctorAsync(doctor);
        return _mapper.Map<DoctorResponse>(createdDoctor);
    }

    public async Task<DoctorResponse?> GetDoctorByIdAsync(Guid id)
    {
        var doctor = await _repository.GetDoctorByIdAsync(id);
        return doctor != null ? _mapper.Map<DoctorResponse>(doctor) : null;
    }

    public async Task<DoctorResponse?> GetDoctorByEmailAsync(string email)
    {
        var doctor = await _repository.GetDoctorByEmailAsync(email);
        return doctor != null ? _mapper.Map<DoctorResponse>(doctor) : null;
    }

    public async Task<DoctorResponse?> GetDoctorByAccountIdAsync(Guid accountId)
    {
        var doctor = await _repository.GetDoctorByAccountIdAsync(accountId);
        return doctor != null ? _mapper.Map<DoctorResponse>(doctor) : null;
    }

    public async Task<DoctorResponse> UpdateDoctorAsync(UpdateDoctorRequest request)
    {
        // Check if doctor exists
        var existingDoctor = await _repository.GetDoctorByIdAsync(request.Id);
        if (existingDoctor == null)
        {
            throw new DoctorNotFoundException(request.Id);
        }

        // Validate unique constraints if email is being updated
        if (!string.IsNullOrEmpty(request.Email) && request.Email != existingDoctor.Email)
        {
            if (await _repository.DoctorEmailExistsAsync(request.Email, request.Id))
            {
                throw new DoctorConflictException(request.Email);
            }
        }

        // Validate position exists if provided
        if (request.PositionId.HasValue && !await _repository.PositionExistsAsync(request.PositionId.Value))
        {
            throw new PositionNotFoundException(request.PositionId.Value);
        }

        // Update doctor entity
        _mapper.Map(request, existingDoctor);
        var updatedDoctor = await _repository.UpdateDoctorAsync(existingDoctor);
        return _mapper.Map<DoctorResponse>(updatedDoctor);
    }

    public async Task<bool> DeleteDoctorAsync(Guid id)
    {
        if (!await _repository.DoctorExistsAsync(id))
        {
            throw new DoctorNotFoundException(id);
        }

        return await _repository.DeleteDoctorAsync(id);
    }

    #endregion

    #region Doctor Query Operations

    public async Task<DoctorListResponse> GetDoctorsAsync(DoctorQueryRequest query)
    {
        var (doctors, totalCount) = await _repository.GetDoctorsAsync(query);
        
        var response = _mapper.Map<DoctorListResponse>((doctors, totalCount));
        response.PageNumber = query.PageNumber;
        response.PageSize = query.PageSize;
        response.TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
        
        return response;
    }

    public async Task<List<DoctorResponse>> GetDoctorsByClinicAsync(Guid clinicId)
    {
        var doctors = await _repository.GetDoctorsByClinicAsync(clinicId);
        return _mapper.Map<List<DoctorResponse>>(doctors);
    }

    public async Task<List<DoctorResponse>> GetDoctorsBySpecialtyAsync(Guid specialtyId)
    {
        var doctors = await _repository.GetDoctorsBySpecialtyAsync(specialtyId);
        return _mapper.Map<List<DoctorResponse>>(doctors);
    }

    public async Task<List<DoctorResponse>> GetDoctorsByPositionAsync(Guid positionId)
    {
        var doctors = await _repository.GetDoctorsByPositionAsync(positionId);
        return _mapper.Map<List<DoctorResponse>>(doctors);
    }

    public async Task<List<DoctorResponse>> GetActiveDoctorsAsync()
    {
        var doctors = await _repository.GetActiveDoctorsAsync();
        return _mapper.Map<List<DoctorResponse>>(doctors);
    }

    #endregion

    #region Position CRUD Operations

    public async Task<PositionResponse> CreatePositionAsync(CreatePositionRequest request)
    {
        // Validate unique constraint
        if (await _repository.PositionNameExistsAsync(request.Name))
        {
            throw new PositionConflictException(request.Name);
        }

        // Create position entity
        var position = _mapper.Map<PositionEntity>(request);
        position.Id = Guid.NewGuid();

        var createdPosition = await _repository.CreatePositionAsync(position);
        return _mapper.Map<PositionResponse>(createdPosition);
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
        // Check if position exists
        var existingPosition = await _repository.GetPositionByIdAsync(request.Id);
        if (existingPosition == null)
        {
            throw new PositionNotFoundException(request.Id);
        }

        // Validate unique constraint if name is being updated
        if (!string.IsNullOrEmpty(request.Name) && request.Name != existingPosition.Name)
        {
            if (await _repository.PositionNameExistsAsync(request.Name, request.Id))
            {
                throw new PositionConflictException(request.Name);
            }
        }

        // Update position entity
        _mapper.Map(request, existingPosition);
        var updatedPosition = await _repository.UpdatePositionAsync(existingPosition);
        return _mapper.Map<PositionResponse>(updatedPosition);
    }

    public async Task<bool> DeletePositionAsync(Guid id)
    {
        if (!await _repository.PositionExistsAsync(id))
        {
            throw new PositionNotFoundException(id);
        }

        return await _repository.DeletePositionAsync(id);
    }

    #endregion

    #region Position Query Operations

    public async Task<PositionListResponse> GetPositionsAsync(PositionQueryRequest query)
    {
        var (positions, totalCount) = await _repository.GetPositionsAsync(query);
        
        var response = _mapper.Map<PositionListResponse>((positions, totalCount));
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
            throw new PriceNotFoundException(request.Id);
        }

        // Update price entity
        _mapper.Map(request, existingPrice);
        var updatedPrice = await _repository.UpdatePriceAsync(existingPrice);
        return _mapper.Map<PriceResponse>(updatedPrice);
    }

    public async Task<bool> DeletePriceAsync(Guid id)
    {
        if (!await _repository.PriceExistsAsync(id))
        {
            throw new PriceNotFoundException(id);
        }

        return await _repository.DeletePriceAsync(id);
    }

    #endregion

    #region Price Query Operations

    public async Task<PriceListResponse> GetPricesAsync(PriceQueryRequest query)
    {
        var (prices, totalCount) = await _repository.GetPricesAsync(query);
        
        var response = _mapper.Map<PriceListResponse>((prices, totalCount));
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

    #region DoctorPrice Operations

    public async Task<DoctorPriceResponse> AssignPriceToDoctorAsync(AssignPriceToDoctorRequest request)
    {
        // Validate doctor exists
        if (!await _repository.DoctorExistsAsync(request.DoctorId))
        {
            throw new DoctorNotFoundException(request.DoctorId);
        }

        // Validate price exists
        if (!await _repository.PriceExistsAsync(request.PriceId))
        {
            throw new PriceNotFoundException(request.PriceId);
        }

        // Check if relationship already exists
        if (await _repository.DoctorPriceExistsAsync(request.DoctorId, request.PriceId))
        {
            throw new DoctorPriceConflictException(request.DoctorId, request.PriceId);
        }

        // Create doctor price relationship
        var doctorPrice = _mapper.Map<DoctorPriceEntity>(request);
        var createdDoctorPrice = await _repository.CreateDoctorPriceAsync(doctorPrice);
        
        // Get the created relationship with navigation properties
        var result = await _repository.GetDoctorPriceAsync(request.DoctorId, request.PriceId);
        return _mapper.Map<DoctorPriceResponse>(result);
    }

    public async Task<bool> RemovePriceFromDoctorAsync(Guid doctorId, Guid priceId)
    {
        // Validate doctor exists
        if (!await _repository.DoctorExistsAsync(doctorId))
        {
            throw new DoctorNotFoundException(doctorId);
        }

        // Validate price exists
        if (!await _repository.PriceExistsAsync(priceId))
        {
            throw new PriceNotFoundException(priceId);
        }

        return await _repository.DeleteDoctorPriceAsync(doctorId, priceId);
    }

    public async Task<List<PriceResponse>> GetDoctorPricesAsync(Guid doctorId)
    {
        // Validate doctor exists
        if (!await _repository.DoctorExistsAsync(doctorId))
        {
            throw new DoctorNotFoundException(doctorId);
        }

        var prices = await _repository.GetDoctorPricesByDoctorIdAsync(doctorId);
        return _mapper.Map<List<PriceResponse>>(prices);
    }

    public async Task<List<DoctorResponse>> GetDoctorsByPriceAsync(Guid priceId)
    {
        // Validate price exists
        if (!await _repository.PriceExistsAsync(priceId))
        {
            throw new PriceNotFoundException(priceId);
        }

        var doctors = await _repository.GetDoctorsByPriceIdAsync(priceId);
        return _mapper.Map<List<DoctorResponse>>(doctors);
    }

    #endregion

    #region Validation Operations

    public async Task<bool> DoctorExistsAsync(Guid id)
    {
        return await _repository.DoctorExistsAsync(id);
    }

    public async Task<bool> DoctorEmailExistsAsync(string email, Guid? excludeId = null)
    {
        return await _repository.DoctorEmailExistsAsync(email, excludeId);
    }

    public async Task<bool> DoctorAccountExistsAsync(Guid accountId, Guid? excludeId = null)
    {
        return await _repository.DoctorAccountExistsAsync(accountId, excludeId);
    }

    public async Task<bool> PositionExistsAsync(Guid id)
    {
        return await _repository.PositionExistsAsync(id);
    }

    public async Task<bool> PositionNameExistsAsync(string name, Guid? excludeId = null)
    {
        return await _repository.PositionNameExistsAsync(name, excludeId);
    }

    public async Task<bool> PriceExistsAsync(Guid id)
    {
        return await _repository.PriceExistsAsync(id);
    }

    public async Task<bool> DoctorPriceExistsAsync(Guid doctorId, Guid priceId)
    {
        return await _repository.DoctorPriceExistsAsync(doctorId, priceId);
    }

    #endregion
}
