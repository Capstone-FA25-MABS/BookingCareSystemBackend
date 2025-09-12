using AutoMapper;
using BookingCare.Services.Doctor.Exceptions;
using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Doctor.Services;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _repository;
    private readonly IPositionRepository _positionRepository;
    private readonly IMapper _mapper;

    public DoctorService(IDoctorRepository repository, IPositionRepository positionRepository, IMapper mapper)
    {
        _repository = repository;
        _positionRepository = positionRepository;
        _mapper = mapper;
    }

    #region Doctor CRUD Operations

    /// <summary>
    /// Tạo doctor mới với giá cơ bản
    /// </summary>
    public async Task<DoctorResponse> CreateDoctorAsync(CreateDoctorRequest request)
    {
        // Validate unique constraints
        if (await _repository.DoctorEmailExistsAsync(request.Email))
        {
            throw DoctorConflictException.WithEmail(request.Email);
        }

        if (await _repository.DoctorAccountExistsAsync(request.AccountId))
        {
            throw DoctorConflictException.WithAccountId(request.AccountId);
        }

        // Validate PositionId exists if provided
        if (request.PositionId.HasValue)
        {
            if (!await _positionRepository.PositionExistsAsync(request.PositionId.Value))
            {
                throw PositionNotFoundException.WithId(request.PositionId.Value);
            }
        }

        // Create doctor entity
        var doctor = _mapper.Map<DoctorEntity>(request);
        doctor.Id = Guid.NewGuid();

        var createdDoctor = await _repository.CreateDoctorAsync(doctor);
        
        // Nếu có truyền Price từ staff, tạo doctor-price
        if (request.Price.HasValue)
        {
            var doctorPrice = new DoctorPriceEntity
            {
                Id = Guid.NewGuid(),
                DoctorId = createdDoctor.Id,
                Amount = request.Price.Value
            };
            await _repository.CreateDoctorPriceAsync(doctorPrice);
        }
        
        var response = _mapper.Map<DoctorResponse>(createdDoctor);
        return response;
    }

    public async Task<DoctorResponse?> GetDoctorByIdAsync(Guid id)
    {
        var doctor = await _repository.GetDoctorByIdAsync(id);
        if (doctor == null) return null;
        var response = _mapper.Map<DoctorResponse>(doctor);
        return response;
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

    /// <summary>
    /// Cập nhật thông tin doctor
    /// </summary>
    public async Task<DoctorResponse> UpdateDoctorAsync(UpdateDoctorRequest request)
    {
        // Check if doctor exists
        var existingDoctor = await _repository.GetDoctorByIdAsync(request.Id);
        if (existingDoctor == null)
        {
            throw DoctorNotFoundException.WithId(request.Id);
        }

        // Validate PositionId exists if provided
        if (request.PositionId.HasValue)
        {
            if (!await _positionRepository.PositionExistsAsync(request.PositionId.Value))
            {
                throw PositionNotFoundException.WithId(request.PositionId.Value);
            }
        }

        // Update doctor entity
        _mapper.Map(request, existingDoctor);
        existingDoctor.UpdatedAt = DateTime.UtcNow;

        // Update price if provided (managed by staff)
        if (request.Price.HasValue)
        {
            await UpdateDoctorPriceAsync(existingDoctor, request.Price.Value, true);
        }

        var updatedDoctor = await _repository.UpdateDoctorAsync(existingDoctor);
        var response = _mapper.Map<DoctorResponse>(updatedDoctor);
        return response;
    }

    public async Task<bool> DeleteDoctorAsync(Guid id)
    {
        return await _repository.DeleteDoctorAsync(id);
    }

    #endregion

    #region Doctor Query Operations

    public async Task<DoctorListResponse> GetDoctorsAsync(DoctorQueryRequest query)
    {
        var (doctors, totalCount) = await _repository.GetDoctorsAsync(query);
        var response = _mapper.Map<DoctorListResponse>((doctors, totalCount));
        
        // Set pagination info
        response.PageNumber = query.PageNumber;
        response.PageSize = query.PageSize;
        response.TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        // Dynamic price removed

        return response;
    }

    public async Task<DoctorListResponse> FilterDoctorsAsync(DoctorAdvancedFilterRequest filter)
    {
        // Convert advanced filter to basic query
        var query = new DoctorQueryRequest
        {
            SpecialtyId = filter.SpecialtyId,
            Gender = filter.Gender,
            MinYearsOfExperience = filter.MinYearsOfExperience,
            MaxYearsOfExperience = filter.MaxYearsOfExperience,
            MinPrice = filter.MinPrice,
            MaxPrice = filter.MaxPrice,
            ClinicId = filter.ClinicId,
            ServiceType = filter.ServiceType,
            Language = filter.Language,
            MinRating = filter.MinRating,
            Address = filter.Address,
            SortBy = filter.SortBy,
            SortOrder = filter.SortOrder,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        return await GetDoctorsAsync(query);
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

    #region DoctorPrice Operations

    public async Task<List<DoctorPriceResponse>> GetDoctorPricesAsync(Guid doctorId)
    {
        var prices = await _repository.GetDoctorPricesAsync(doctorId);
        return _mapper.Map<List<DoctorPriceResponse>>(prices);
    }

    

    public async Task<DoctorPriceResponse> AssignPriceToDoctorAsync(AssignPriceToDoctorRequest request)
    {
        // Validate doctor exists
        if (!await _repository.DoctorExistsAsync(request.DoctorId))
        {
            throw DoctorNotFoundException.WithId(request.DoctorId);
        }

        // Get doctor
        var doctor = await _repository.GetDoctorByIdAsync(request.DoctorId);
        if (doctor == null)
        {
            throw DoctorNotFoundException.WithId(request.DoctorId);
        }

        // Create doctor-price relationship
        var doctorPrice = new DoctorPriceEntity
        {
            Id = Guid.NewGuid(),
            DoctorId = request.DoctorId,
            Amount = request.Amount
        };
        
        var createdDoctorPrice = await _repository.CreateDoctorPriceAsync(doctorPrice);
        return _mapper.Map<DoctorPriceResponse>(createdDoctorPrice);
    }

    public async Task<bool> RemovePriceFromDoctorAsync(Guid doctorId, Guid priceId)
    {
        return await _repository.DeleteDoctorPriceAsync(doctorId, priceId);
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

    public async Task<bool> DoctorPriceExistsAsync(Guid doctorId, Guid priceId)
    {
        return await _repository.DoctorPriceExistsAsync(doctorId, priceId);
    }

    #endregion

    #region Helper Methods

    public IQueryable<DoctorEntity> GetQueryableDoctors()
    {
        return _repository.GetQueryableDoctors();
    }

    // Removed automatic price calculation; prices are managed by staff only

    // Removed CreateDoctorPriceAsync - now using direct repository call

    private async Task UpdateDoctorPriceAsync(DoctorEntity doctor, decimal amount, bool isOverride)
    {
        // Tìm giá hiện tại của doctor
        var existingPrices = await _repository.GetDoctorPricesAsync(doctor.Id);
        var existingPrice = existingPrices.FirstOrDefault();
        
        if (existingPrice != null)
        {
            // Cập nhật giá hiện tại
            existingPrice.Amount = amount;
            existingPrice.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateDoctorPriceAsync(existingPrice);
        }
        else
        {
            // Tạo mới giá
            var doctorPrice = new DoctorPriceEntity
            {
                Id = Guid.NewGuid(),
                DoctorId = doctor.Id,
                Amount = amount
            };
            await _repository.CreateDoctorPriceAsync(doctorPrice);
        }
    }

    #endregion
}
