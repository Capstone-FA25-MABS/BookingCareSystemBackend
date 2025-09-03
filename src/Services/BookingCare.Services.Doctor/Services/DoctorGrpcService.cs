using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Doctor.Services;
using BookingCare.Services.Doctor.Models.DTOs;
using Grpc.Core;
using AutoMapper;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Services;

public class DoctorGrpcService : Protos.DoctorService.DoctorServiceBase
{
    private readonly IDoctorService _doctorService;
    private readonly IMapper _mapper;
    private readonly ILogger<DoctorGrpcService> _logger;

    public DoctorGrpcService(
        IDoctorService doctorService,
        IMapper mapper, 
        ILogger<DoctorGrpcService> logger)
    {
        _doctorService = doctorService;
        _mapper = mapper;
        _logger = logger;
    }

    #region Doctor gRPC Operations

    public override async Task<CreateDoctorResponse> CreateDoctor(
        Protos.CreateDoctorRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC CreateDoctor called for email: {Email}", request.Email);

            var createRequest = new Models.DTOs.CreateDoctorRequest
            {
                AccountId = Guid.Parse(request.AccountId),
                Email = request.Email,
                Address = request.Address,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = request.Gender != "" ? (Gender)Enum.Parse(typeof(Gender), request.Gender) : null,
                PositionId = !string.IsNullOrEmpty(request.PositionId) ? Guid.Parse(request.PositionId) : null,
                SpecialtyId = !string.IsNullOrEmpty(request.SpecialtyId) ? Guid.Parse(request.SpecialtyId) : null,
                ClinicId = !string.IsNullOrEmpty(request.ClinicId) ? Guid.Parse(request.ClinicId) : null,
                Bio = request.Bio,
                YearsOfExperience = request.YearsOfExperience > 0 ? request.YearsOfExperience : 0,
                AvatarUrl = request.AvatarUrl
            };

            var result = await _doctorService.CreateDoctorAsync(createRequest);

            return new CreateDoctorResponse
            {
                Success = true,
                Doctor = MapToDoctorInfo(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CreateDoctor");
            return new CreateDoctorResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<GetDoctorResponse> GetDoctorById(
        Protos.GetDoctorByIdRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetDoctorById called for ID: {Id}", request.Id);

            var doctorId = Guid.Parse(request.Id);
            var result = await _doctorService.GetDoctorByIdAsync(doctorId);

            if (result == null)
            {
                return new GetDoctorResponse
                {
                    Success = false,
                    ErrorMessage = "Doctor not found"
                };
            }

            return new GetDoctorResponse
            {
                Success = true,
                Doctor = MapToDoctorInfo(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetDoctorById");
            return new GetDoctorResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<GetDoctorResponse> GetDoctorByEmail(
        Protos.GetDoctorByEmailRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetDoctorByEmail called for email: {Email}", request.Email);

            var result = await _doctorService.GetDoctorByEmailAsync(request.Email);

            if (result == null)
            {
                return new GetDoctorResponse
                {
                    Success = false,
                    ErrorMessage = "Doctor not found"
                };
            }

            return new GetDoctorResponse
            {
                Success = true,
                Doctor = MapToDoctorInfo(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetDoctorByEmail");
            return new GetDoctorResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<UpdateDoctorResponse> UpdateDoctor(
        Protos.UpdateDoctorRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC UpdateDoctor called for ID: {Id}", request.Id);

            var updateRequest = new Models.DTOs.UpdateDoctorRequest
            {
                Id = Guid.Parse(request.Id),
                Address = request.Address,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = request.Gender != "" ? (Gender)Enum.Parse(typeof(Gender), request.Gender) : null,
                PositionId = !string.IsNullOrEmpty(request.PositionId) ? Guid.Parse(request.PositionId) : null,
                SpecialtyId = !string.IsNullOrEmpty(request.SpecialtyId) ? Guid.Parse(request.SpecialtyId) : null,
                ClinicId = !string.IsNullOrEmpty(request.ClinicId) ? Guid.Parse(request.ClinicId) : null,
                Bio = request.Bio,
                YearsOfExperience = request.YearsOfExperience > 0 ? request.YearsOfExperience : null,
                AvatarUrl = request.AvatarUrl
            };

            var result = await _doctorService.UpdateDoctorAsync(updateRequest);

            return new UpdateDoctorResponse
            {
                Success = true,
                Doctor = MapToDoctorInfo(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC UpdateDoctor");
            return new UpdateDoctorResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<DeleteDoctorResponse> DeleteDoctor(
        Protos.DeleteDoctorRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC DeleteDoctor called for ID: {Id}", request.Id);

            var doctorId = Guid.Parse(request.Id);
            var result = await _doctorService.DeleteDoctorAsync(doctorId);

            return new DeleteDoctorResponse
            {
                Success = result,
                ErrorMessage = result ? "" : "Failed to delete doctor"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC DeleteDoctor");
            return new DeleteDoctorResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<GetDoctorsResponse> GetDoctors(
        Protos.GetDoctorsRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetDoctors called");

            var queryRequest = new Models.DTOs.DoctorQueryRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                AccountId = !string.IsNullOrEmpty(request.AccountId) ? Guid.Parse(request.AccountId) : null,
                PositionId = !string.IsNullOrEmpty(request.PositionId) ? Guid.Parse(request.PositionId) : null,
                SpecialtyId = !string.IsNullOrEmpty(request.SpecialtyId) ? Guid.Parse(request.SpecialtyId) : null,
                ClinicId = !string.IsNullOrEmpty(request.ClinicId) ? Guid.Parse(request.ClinicId) : null,
                Gender = request.Gender != "" ? (Gender)Enum.Parse(typeof(Gender), request.Gender) : null,
                MinYearsOfExperience = request.MinYearsOfExperience,
                MaxYearsOfExperience = request.MaxYearsOfExperience
            };

            var result = await _doctorService.GetDoctorsAsync(queryRequest);

            var response = new GetDoctorsResponse
            {
                Success = true,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            foreach (var doctor in result.Doctors)
            {
                response.Doctors.Add(MapToDoctorInfo(doctor));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetDoctors");
            return new GetDoctorsResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region DoctorPrice gRPC Operations

    public override async Task<AssignPriceToDoctorResponse> AssignPriceToDoctor(
        Protos.AssignPriceToDoctorRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC AssignPriceToDoctor called for DoctorId: {DoctorId}, PriceId: {PriceId}", 
                request.DoctorId, request.PriceId);

            var assignRequest = new Models.DTOs.AssignPriceToDoctorRequest
            {
                DoctorId = Guid.Parse(request.DoctorId),
                PriceId = Guid.Parse(request.PriceId),
                Description = request.Description
            };

            var result = await _doctorService.AssignPriceToDoctorAsync(assignRequest);

            return new AssignPriceToDoctorResponse
            {
                Success = true,
                DoctorPrice = MapToDoctorPriceInfo(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC AssignPriceToDoctor");
            return new AssignPriceToDoctorResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<RemovePriceFromDoctorResponse> RemovePriceFromDoctor(
        Protos.RemovePriceFromDoctorRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC RemovePriceFromDoctor called for DoctorId: {DoctorId}, PriceId: {PriceId}", 
                request.DoctorId, request.PriceId);

            var doctorId = Guid.Parse(request.DoctorId);
            var priceId = Guid.Parse(request.PriceId);
            var result = await _doctorService.RemovePriceFromDoctorAsync(doctorId, priceId);

            return new RemovePriceFromDoctorResponse
            {
                Success = result,
                ErrorMessage = result ? "" : "Failed to remove price from doctor"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC RemovePriceFromDoctor");
            return new RemovePriceFromDoctorResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<GetDoctorPricesResponse> GetDoctorPrices(
        Protos.GetDoctorPricesRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC GetDoctorPrices called for DoctorId: {DoctorId}", request.DoctorId);

            var doctorId = Guid.Parse(request.DoctorId);
            var result = await _doctorService.GetDoctorPricesAsync(doctorId);

            var response = new GetDoctorPricesResponse
            {
                Success = true
            };

            foreach (var price in result)
            {
                response.Prices.Add(MapToPriceInfo(price));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetDoctorPrices");
            return new GetDoctorPricesResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Validation gRPC Operations

    public override async Task<ValidateDoctorResponse> ValidateDoctor(
        Protos.ValidateDoctorRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC ValidateDoctor called for ID: {Id}", request.Id);

            var doctorId = Guid.Parse(request.Id);
            var result = await _doctorService.DoctorExistsAsync(doctorId);

            return new ValidateDoctorResponse
            {
                Success = true,
                Exists = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC ValidateDoctor");
            return new ValidateDoctorResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Mapping Methods

    private static DoctorInfo MapToDoctorInfo(Models.DTOs.DoctorResponse doctor)
    {
        return new DoctorInfo
        {
            Id = doctor.Id.ToString(),
            AccountId = doctor.AccountId.ToString(),
            Email = doctor.Email,
            Address = doctor.Address ?? "",
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            Gender = doctor.Gender?.ToString() ?? "",
            PositionId = doctor.PositionId?.ToString() ?? "",
            SpecialtyId = doctor.SpecialtyId?.ToString() ?? "",
            ClinicId = doctor.ClinicId?.ToString() ?? "",
            Bio = doctor.Bio ?? "",
            YearsOfExperience = doctor.YearsOfExperience,
            AvatarUrl = doctor.AvatarUrl ?? "",
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(doctor.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(doctor.UpdatedAt.ToUniversalTime())
        };
    }

    private static PriceInfo MapToPriceInfo(Models.DTOs.PriceResponse price)
    {
        return new PriceInfo
        {
            Id = price.Id.ToString(),
            Amount = (double)price.Amount
        };
    }

    private static DoctorPriceInfo MapToDoctorPriceInfo(Models.DTOs.DoctorPriceResponse doctorPrice)
    {
        return new DoctorPriceInfo
        {
            DoctorId = doctorPrice.DoctorId.ToString(),
            PriceId = doctorPrice.PriceId.ToString(),
            Description = doctorPrice.Description ?? "",
            Doctor = MapToDoctorInfo(doctorPrice.Doctor),
            Price = MapToPriceInfo(doctorPrice.Price)
        };
    }

    #endregion
}