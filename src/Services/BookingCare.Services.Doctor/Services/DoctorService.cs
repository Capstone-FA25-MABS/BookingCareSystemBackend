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
    private readonly IPriceRepository _priceRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IMapper _mapper;

    public DoctorService(IDoctorRepository repository, IPriceRepository priceRepository, IPositionRepository positionRepository, IMapper mapper)
    {
        _repository = repository;
        _priceRepository = priceRepository;
        _positionRepository = positionRepository;
        _mapper = mapper;
    }

    #region Doctor CRUD Operations

    /// <summary>
    /// API này sẽ tự động gán giá động cho doctor nếu chưa có giá override (staff nhập).
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
        // Gán giá cho doctor
        decimal dynamicPrice = await CalculateDynamicPriceByRuleAsync(createdDoctor) ?? 0;
        decimal priceToAssign = request.Price ?? dynamicPrice;
        bool isOverride = request.Price != null && request.Price != dynamicPrice;
        await AssignPriceToDoctorInternalAsync(createdDoctor, priceToAssign, isOverride);
        var response = _mapper.Map<DoctorResponse>(createdDoctor);
        response.DynamicPrice = dynamicPrice;
        return response;
    }

    public async Task<DoctorResponse?> GetDoctorByIdAsync(Guid id)
    {
        var doctor = await _repository.GetDoctorByIdAsync(id);
        if (doctor == null) return null;
        var response = _mapper.Map<DoctorResponse>(doctor);
        response.DynamicPrice = await CalculateDynamicPriceAsync(doctor);
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
    /// API này sẽ tự động gán giá động cho doctor nếu chưa có giá override (staff nhập).
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

        var updatedDoctor = await _repository.UpdateDoctorAsync(existingDoctor);
        // Tự động gán giá động nếu chưa có giá override
        await AutoAssignDynamicPriceAsync(updatedDoctor);
        var response = _mapper.Map<DoctorResponse>(updatedDoctor);
        response.DynamicPrice = await CalculateDynamicPriceAsync(updatedDoctor);
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

        // Calculate dynamic prices for each doctor
        foreach (var doctor in response.Doctors)
        {
            var doctorEntity = doctors.First(d => d.Id == doctor.Id);
            doctor.DynamicPrice = await CalculateDynamicPriceAsync(doctorEntity);
        }

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

    public async Task<List<PriceResponse>> GetDoctorPricesAsync(Guid doctorId)
    {
        var prices = await _repository.GetDoctorPricesByDoctorIdAsync(doctorId);
        return _mapper.Map<List<PriceResponse>>(prices);
    }

    public async Task<List<DoctorResponse>> GetDoctorsByPriceAsync(Guid priceId)
    {
        var doctors = await _repository.GetDoctorsByPriceIdAsync(priceId);
        return _mapper.Map<List<DoctorResponse>>(doctors);
    }

    public async Task<DoctorPriceResponse> AssignPriceToDoctorAsync(AssignPriceToDoctorRequest request)
    {
        // Validate doctor exists
        if (!await _repository.DoctorExistsAsync(request.DoctorId))
        {
            throw DoctorNotFoundException.WithId(request.DoctorId);
        }

        // Validate price exists
        if (!await _priceRepository.PriceExistsAsync(request.PriceId))
        {
            throw PriceNotFoundException.WithId(request.PriceId);
        }

        // Check if relationship already exists
        if (await _repository.DoctorPriceExistsAsync(request.DoctorId, request.PriceId))
        {
            throw DoctorPriceConflictException.WithIds(request.DoctorId, request.PriceId);
        }

        // Create doctor-price relationship
        var doctorPrice = _mapper.Map<DoctorPriceEntity>(request);
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

    private async Task<decimal?> CalculateDynamicPriceAsync(DoctorEntity doctor)
    {
        // Nếu có giá override thì trả về giá override
        var overridePrice = doctor.DoctorPrices?.FirstOrDefault(dp => dp.IsOverride);
        if (overridePrice != null)
        {
            return overridePrice.Price?.Amount;
        }
        // Nếu không có override, lấy rule phù hợp và tính giá
        var activeRule = await _priceRepository.GetActivePriceRuleAsync(
            minExperience: doctor.YearsOfExperience,
            position: doctor.Position?.Name
        );
        if (activeRule == null) return null;
        decimal price = activeRule.BasePrice;
        return price;
    }

    private async Task AutoAssignDynamicPriceAsync(DoctorEntity doctor)
    {
        // Nếu đã có giá override thì không làm gì
        if (doctor.DoctorPrices.Any(dp => dp.IsOverride)) return;
        // Tính giá động
        var priceValue = await CalculateDynamicPriceAsync(doctor);
        if (priceValue == null) return;
        // Kiểm tra đã có giá động chưa (IsOverride=false)
        var dynamicPrice = doctor.DoctorPrices.FirstOrDefault(dp => !dp.IsOverride);
        if (dynamicPrice != null)
        {
            // Update giá động
            dynamicPrice.Price.Amount = priceValue.Value;
            await _repository.UpdateDoctorAsync(doctor);
        }
        else
        {
            // Tạo mới PriceEntity và lưu vào database trước
            var price = new PriceEntity { Id = Guid.NewGuid(), Amount = priceValue.Value };
            await _priceRepository.CreatePriceAsync(price);
            
            // Tạo mới DoctorPriceEntity
            var doctorPrice = new DoctorPriceEntity
            {
                DoctorId = doctor.Id,
                PriceId = price.Id,
                IsOverride = false,
                Price = price,
                Doctor = doctor
            };
            doctor.DoctorPrices.Add(doctorPrice);
            await _repository.UpdateDoctorAsync(doctor);
        }
    }

    private async Task<decimal?> CalculateDynamicPriceByRuleAsync(DoctorEntity doctor)
    {
        var activeRule = await _priceRepository.GetActivePriceRuleAsync(
            minExperience: doctor.YearsOfExperience,
            position: doctor.Position?.Name
        );
        if (activeRule == null) return null;
        return activeRule.BasePrice;
    }

    private async Task AssignPriceToDoctorInternalAsync(DoctorEntity doctor, decimal price, bool isOverride)
    {
        // Tạo mới PriceEntity và lưu vào database trước
        var priceEntity = new PriceEntity { Id = Guid.NewGuid(), Amount = price };
        await _priceRepository.CreatePriceAsync(priceEntity);
        // Tạo mới DoctorPriceEntity
        var doctorPrice = new DoctorPriceEntity
        {
            DoctorId = doctor.Id,
            PriceId = priceEntity.Id,
            IsOverride = isOverride,
            Price = priceEntity,
            Doctor = doctor
        };
        doctor.DoctorPrices.Add(doctorPrice);
        await _repository.UpdateDoctorAsync(doctor);
    }

    #endregion
}
