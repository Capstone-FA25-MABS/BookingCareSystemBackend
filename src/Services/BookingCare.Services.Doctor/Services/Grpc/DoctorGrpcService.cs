using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Review.Grpc;
using Grpc.Core;

namespace BookingCare.Services.Doctor.Services.Grpc;

public class DoctorGrpcService : Protos.DoctorService.DoctorServiceBase
{
    private const string INVALID_SPECIALTY_ID_FORMAT = "Invalid specialty ID format";
    private readonly IDoctorService _doctorService;
    private readonly ISpecialtyService _specialtyService;
    private readonly ReviewService.ReviewServiceClient _reviewClient;
    private readonly BookingCare.Services.Hospital.HospitalService.HospitalServiceClient _hospitalClient;
    private readonly ILogger<DoctorGrpcService> _logger;

    public DoctorGrpcService(
        IDoctorService doctorService,
        ISpecialtyService specialtyService,
        ReviewService.ReviewServiceClient reviewClient,
        BookingCare.Services.Hospital.HospitalService.HospitalServiceClient hospitalClient,
        ILogger<DoctorGrpcService> logger)
    {
        _doctorService = doctorService;
        _specialtyService = specialtyService;
        _reviewClient = reviewClient;
        _hospitalClient = hospitalClient;
        _logger = logger;
    }

    public override async Task<Protos.DoctorResponse> GetDoctor(Protos.GetDoctorRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var id))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid doctor ID format"));
            }

            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Doctor with ID {id} not found"));
            }

            return MapToGrpcDoctorResponseFromById(doctor);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctor for {Id}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.DoctorResponse> GetDoctorByAccountId(Protos.GetDoctorByAccountIdRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.AccountId, out var accountId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid account ID format"));
            }

            var doctor = await _doctorService.GetDoctorByAccountIdAsync(accountId);
            if (doctor == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Doctor with AccountId {accountId} not found"));
            }

            return MapToGrpcDoctorResponse(doctor);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorByAccountId for {AccountId}", request.AccountId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Map basic doctor fields from gRPC request
    /// </summary>
    private Models.DTOs.Requests.CreateDoctorRequest MapBasicDoctorFields(Protos.CreateDoctorRequest request, Guid accountId)
    {
        return new Models.DTOs.Requests.CreateDoctorRequest
        {
            AccountId = accountId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address,
            Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio,
            YearsOfExperience = request.YearsOfExperience,
            AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl
        };
    }

    /// <summary>
    /// Map optional IDs from gRPC request
    /// </summary>
    private void MapOptionalIds(Protos.CreateDoctorRequest request, Models.DTOs.Requests.CreateDoctorRequest create)
    {
        if (Guid.TryParse(request.SpecialtyId, out var specialtyId))
        {
            create.SpecialtyId = specialtyId;
        }

        if (Guid.TryParse(request.PositionId, out var positionId))
        {
            create.PositionId = positionId;
        }

        if (Guid.TryParse(request.HospitalId, out var hospitalId))
        {
            create.HospitalId = hospitalId;
        }
    }

    /// <summary>
    /// Map gender from gRPC request
    /// </summary>
    private void MapGender(Protos.CreateDoctorRequest request, Models.DTOs.Requests.CreateDoctorRequest create)
    {
        if (!string.IsNullOrWhiteSpace(request.Gender) &&
            Enum.TryParse<Shared.Common.Enums.Gender>(request.Gender, true, out var gender))
        {
            create.Gender = gender;
        }
    }

    /// <summary>
    /// Map language IDs from gRPC request
    /// </summary>
    private void MapLanguageIds(Protos.CreateDoctorRequest request, Models.DTOs.Requests.CreateDoctorRequest create)
    {
        if (request.LanguageIds == null || request.LanguageIds.Count == 0)
        {
            return;
        }

        create.LanguageIds = request.LanguageIds
            .Where(id => Guid.TryParse(id, out _))
            .Select(Guid.Parse)
            .ToList();
    }

    /// <summary>
    /// Map prices from gRPC request
    /// </summary>
    private void MapPrices(Protos.CreateDoctorRequest request, Models.DTOs.Requests.CreateDoctorRequest create)
    {
        if (request.Prices == null || request.Prices.Count == 0)
        {
            return;
        }

        create.Prices = request.Prices
            .Where(p => Guid.TryParse(p.ServiceTypeId, out _))
            .Select(p => new Models.DTOs.Requests.DoctorPriceRequest
            {
                ServiceTypeId = Guid.Parse(p.ServiceTypeId),
                Amount = (decimal)p.Amount
            })
            .ToList();
    }

    /// <summary>
    /// Build create doctor request from gRPC request
    /// </summary>
    private Models.DTOs.Requests.CreateDoctorRequest BuildCreateDoctorRequest(Protos.CreateDoctorRequest request, Guid accountId)
    {
        var create = MapBasicDoctorFields(request, accountId);
        MapOptionalIds(request, create);
        MapGender(request, create);
        MapLanguageIds(request, create);
        MapPrices(request, create);
        return create;
    }

    public override async Task<Protos.DoctorResponse> CreateDoctor(Protos.CreateDoctorRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.AccountId, out var accountId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid account ID format"));
            }

            var create = BuildCreateDoctorRequest(request, accountId);
            var doctor = await _doctorService.CreateDoctorAsync(create);
            return MapToGrpcDoctorResponse(doctor);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "[DoctorGrpcService] Invalid operation in CreateDoctor for email {Email}: {Message}", request.Email, ex.Message);
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in CreateDoctor for email {Email}", request.Email);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.DoctorBatchResponse> GetDoctorsByAccountIds(Protos.GetDoctorsByAccountIdsRequest request, ServerCallContext context)
    {
        try
        {
            var accountIds = new List<Guid>();
            foreach (var idStr in request.AccountIds)
            {
                if (!Guid.TryParse(idStr, out var id))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid account ID format: {idStr}"));
                }
                accountIds.Add(id);
            }

            var doctors = await _doctorService.GetDoctorsByAccountIdsAsync(accountIds);
            var resp = new Protos.DoctorBatchResponse();
            foreach (var d in doctors)
            {
                resp.Doctors.Add(new Protos.DoctorBasicInfo
                {
                    AccountId = d.AccountId.ToString(),
                    Email = d.Email,
                    FullName = d.FullName,
                    AvatarUrl = d.AvatarUrl,
                    Address = d.Address,
                });
            }
            return resp;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorsByAccountIds");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.DeleteDoctorResponse> DeleteDoctor(Protos.DeleteDoctorRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[DoctorGrpcService] gRPC DeleteDoctor called for ID: {DoctorId}", request.Id);

            if (!Guid.TryParse(request.Id, out var doctorId))
            {
                return new Protos.DeleteDoctorResponse
                {
                    Success = false,
                    Message = "Invalid doctor ID format"
                };
            }

            var result = await _doctorService.DeleteDoctorAsync(doctorId);

            if (result)
            {
                _logger.LogInformation("[DoctorGrpcService] Doctor deleted successfully: {DoctorId}", doctorId);
                return new Protos.DeleteDoctorResponse
                {
                    Success = true,
                    Message = "Doctor deleted successfully"
                };
            }
            else
            {
                return new Protos.DeleteDoctorResponse
                {
                    Success = true, // Consider it successful if already deleted
                    Message = "Doctor not found or already deleted"
                };
            }
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error deleting doctor: {DoctorId}", request.Id);
            return new Protos.DeleteDoctorResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public override async Task<Protos.DoctorBasicInfoResponse> GetDoctorBasicInfo(Protos.GetDoctorBasicInfoRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var id))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid doctor ID format"));
            }

            var doctor = await _doctorService.GetDoctorBasicInfoByIdAsync(id);
            if (doctor == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Doctor with ID {id} not found"));
            }

            return MapToGrpcDoctorBasicInfoResponse(doctor);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorBasicInfo for {Id}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.DoctorsBasicInfoResponse> GetDoctorsBasicInfo(Protos.GetDoctorsBasicInfoRequest request, ServerCallContext context)
    {
        try
        {
            var ids = new List<Guid>();
            foreach (var idStr in request.Ids)
            {
                if (!Guid.TryParse(idStr, out var id))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid doctor ID format: {idStr}"));
                }
                ids.Add(id);
            }

            var doctors = await _doctorService.GetDoctorsBasicInfoByIdsAsync(ids);
            var response = new Protos.DoctorsBasicInfoResponse();

            // If service_type_name is provided, get prices for all doctors in batch
            Dictionary<Guid, decimal>? priceDict = null;
            if (!string.IsNullOrWhiteSpace(request.ServiceTypeName))
            {
                priceDict = await _doctorService.GetDoctorsPricesByServiceTypeAsync(ids, request.ServiceTypeName);
            }

            foreach (var doctor in doctors)
            {
                var grpcDoctor = MapToGrpcDoctorBasicInfoResponse(doctor);

                // Add consultation fee if available
                if (priceDict != null && priceDict.TryGetValue(doctor.Id, out var price))
                {
                    grpcDoctor.ConsultationFee = (double)price;
                }

                response.Doctors.Add(grpcDoctor);
            }

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorsBasicInfo");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.SpecialtySimpleResponse> GetSpecialtyById(Protos.GetSpecialtyByIdRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var id))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, INVALID_SPECIALTY_ID_FORMAT));
            }

            var specialty = await _specialtyService.GetSpecialtyByIdAsync(id);
            if (specialty == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Specialty with ID {id} not found"));
            }

            return MapToGrpcSpecialtySimpleResponse(specialty);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetSpecialtyById for {Id}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.SpecialtiesBatchResponse> GetSpecialtiesByIds(Protos.GetSpecialtiesByIdsRequest request, ServerCallContext context)
    {
        try
        {
            var ids = new List<Guid>();
            var hasValidId = false;
            foreach (var idStr in request.Ids)
            {
                if (Guid.TryParse(idStr, out var id))
                {
                    ids.Add(id);
                    hasValidId = true;
                }
                else
                {
                    _logger.LogWarning("[DoctorGrpcService] Invalid specialty ID format: {Id}", idStr);
                }
            }

            if (!hasValidId)
            {
                _logger.LogWarning("[DoctorGrpcService] No valid specialty IDs provided");
                return new Protos.SpecialtiesBatchResponse();
            }

            var specialties = await _specialtyService.GetSpecialtiesByIdsAsync(ids);
            var response = new Protos.SpecialtiesBatchResponse();

            foreach (var specialty in specialties)
            {
                response.Specialties.Add(MapToGrpcSpecialtySimpleResponse(specialty));
            }

            _logger.LogInformation("[DoctorGrpcService] Retrieved {Count} specialties out of {Requested} requested",
                specialties.Count, request.Ids.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetSpecialtiesByIds");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    private static Protos.DoctorBasicInfoResponse MapToGrpcDoctorBasicInfoResponse(Models.Entities.DoctorEntity doctor)
    {
        return new Protos.DoctorBasicInfoResponse
        {
            Id = doctor.Id.ToString(),
            Email = doctor.Email,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            FullName = $"{doctor.FirstName} {doctor.LastName}".Trim(),
            PositionName = doctor.Position?.Name ?? string.Empty,
            SpecialtyName = doctor.Specialty?.Name ?? string.Empty,
            AvatarUrl = doctor.AvatarUrl,
            HospitalId = doctor.HospitalId?.ToString() ?? string.Empty
        };
    }

    public override async Task<Protos.GetAvailableDoctorsResponse> GetAvailableDoctors(
        Protos.GetAvailableDoctorsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[DoctorGrpcService] GetAvailableDoctors called for hospital {HospitalId}, specialty {SpecialtyId}",
                request.HospitalId, request.SpecialtyId);

            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid hospital ID format"));
            }

            if (!Guid.TryParse(request.SpecialtyId, out var specialtyId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, INVALID_SPECIALTY_ID_FORMAT));
            }

            // Get doctors by hospital and specialty (without availability check)
            var doctors = await _doctorService.GetDoctorsByHospitalAndSpecialtyAsync(hospitalId, specialtyId);

            var response = new Protos.GetAvailableDoctorsResponse
            {
                TotalCount = doctors.Count
            };

            foreach (var doctor in doctors)
            {
                response.Doctors.Add(new Protos.AvailableDoctorInfo
                {
                    Id = doctor.Id.ToString(),
                    FirstName = doctor.FirstName,
                    LastName = doctor.LastName,
                    FullName = $"{doctor.FirstName} {doctor.LastName}".Trim(),
                    AvatarUrl = doctor.AvatarUrl,
                    PositionName = doctor.Position?.Name ?? string.Empty,
                    SpecialtyName = doctor.Specialty?.Name ?? string.Empty,
                    YearsOfExperience = doctor.YearsOfExperience
                });
            }

            _logger.LogInformation("[DoctorGrpcService] Returning {Count} doctors", doctors.Count);
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetAvailableDoctors");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Get doctor IDs only by hospital and specialty (optimized for schedule aggregation)
    /// Returns only doctor IDs without additional information for better performance
    /// Optionally filters by appointment type (IN_PERSON or TELEHEALTH)
    /// </summary>
    public override async Task<Protos.GetDoctorIdsByHospitalAndSpecialtyResponse> GetDoctorIdsByHospitalAndSpecialty(
        Protos.GetDoctorIdsByHospitalAndSpecialtyRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[DoctorGrpcService] GetDoctorIdsByHospitalAndSpecialty called for hospital {HospitalId}, specialty {SpecialtyId}, appointmentType {AppointmentType}",
                request.HospitalId, request.SpecialtyId, request.AppointmentType ?? "ALL");

            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid hospital ID format"));
            }

            if (!Guid.TryParse(request.SpecialtyId, out var specialtyId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, INVALID_SPECIALTY_ID_FORMAT));
            }

            // Get active doctor IDs only by hospital and specialty, optionally filtered by appointment type
            var doctorIds = await _doctorService.GetActiveDoctorIdsByHospitalAndSpecialtyAsync(
                hospitalId,
                specialtyId,
                request.AppointmentType);

            var response = new Protos.GetDoctorIdsByHospitalAndSpecialtyResponse
            {
                TotalCount = doctorIds.Count
            };

            response.DoctorIds.AddRange(doctorIds.Select(id => id.ToString()));

            _logger.LogInformation("[DoctorGrpcService] Returning {Count} doctor IDs", doctorIds.Count);
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorIdsByHospitalAndSpecialty");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.GetDoctorPriceResponse> GetDoctorPrice(
        Protos.GetDoctorPriceRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[DoctorGrpcService] GetDoctorPrice called for price ID {PriceId}", request.PriceId);

            if (!Guid.TryParse(request.PriceId, out var priceId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid price ID format"));
            }

            // Get doctor price from service
            var price = await _doctorService.GetDoctorPriceByIdAsync(priceId);
            if (price == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Price with ID {priceId} not found"));
            }

            var response = new Protos.GetDoctorPriceResponse
            {
                Id = price.Id.ToString(),
                DoctorId = price.DoctorId.ToString(),
                Amount = (double)price.Amount,
                Currency = "VND"
            };

            _logger.LogInformation("[DoctorGrpcService] Returning price {Amount} VND for price ID {PriceId}",
                price.Amount, priceId);
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorPrice for {PriceId}", request.PriceId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }


    private static Protos.DoctorResponse MapToGrpcDoctorResponse(DoctorResponse d)
    {
        return new Protos.DoctorResponse
        {
            Id = d.Id.ToString(),
            AccountId = d.AccountId.ToString(),
            Email = d.Email,
            FirstName = d.FirstName,
            LastName = d.LastName,
            FullName = $"{d.FirstName} {d.LastName}".Trim(),
            Gender = d.Gender?.ToString() ?? string.Empty,
            Address = d.Address ?? string.Empty,
            SpecialtyId = d.SpecialtyId?.ToString() ?? string.Empty,
            PositionId = d.PositionId?.ToString() ?? string.Empty,
            HospitalId = d.HospitalId?.ToString() ?? string.Empty,
            Bio = d.Bio ?? string.Empty,
            YearsOfExperience = d.YearsOfExperience,
            AvatarUrl = d.AvatarUrl,
            CreatedAt = d.CreatedAt.ToString("O"),
            UpdatedAt = d.UpdatedAt.ToString("O"),
            Status = d.Status.ToString()
        };
    }

    private static Protos.DoctorResponse MapToGrpcDoctorResponseFromById(DoctorByIdResponse d)
    {
        return new Protos.DoctorResponse
        {
            Id = d.Id.ToString(),
            AccountId = string.Empty, // Not available in DoctorByIdResponse
            Email = d.Email,
            FirstName = d.FirstName,
            LastName = d.LastName,
            FullName = $"{d.FirstName} {d.LastName}".Trim(),
            Gender = d.Gender?.ToString() ?? string.Empty,
            Address = d.Address ?? string.Empty,
            SpecialtyId = d.Specialty?.Id.ToString() ?? string.Empty,
            PositionId = d.Position?.Id.ToString() ?? string.Empty,
            HospitalId = d.Hospital?.Id.ToString() ?? string.Empty,
            Bio = d.Bio ?? string.Empty,
            YearsOfExperience = d.YearsOfExperience,
            AvatarUrl = d.AvatarUrl,
            CreatedAt = string.Empty, // Not available in DoctorByIdResponse
            UpdatedAt = string.Empty, // Not available in DoctorByIdResponse
            Status = string.Empty // Not available in DoctorByIdResponse
        };
    }

    private static Protos.SpecialtySimpleResponse MapToGrpcSpecialtySimpleResponse(SpecialtyResponse specialty)
    {
        return new Protos.SpecialtySimpleResponse
        {
            Id = specialty.Id.ToString(),
            Name = specialty.Name,
            ImageUrl = specialty.ImageUrl ?? string.Empty
        };
    }

    public override async Task<Protos.GetDoctorCountsBySpecialtyAndHospitalResponse> GetDoctorCountsBySpecialtyAndHospital(
        Protos.GetDoctorCountsBySpecialtyAndHospitalRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid hospital ID format"));
            }

            var specialtyIds = request.SpecialtyIds
                .Where(id => Guid.TryParse(id, out _))
                .Select(Guid.Parse)
                .ToList();

            if (!specialtyIds.Any())
            {
                return new Protos.GetDoctorCountsBySpecialtyAndHospitalResponse();
            }

            var counts = await _doctorService.GetDoctorCountsBySpecialtyAndHospitalAsync(hospitalId, specialtyIds);

            var response = new Protos.GetDoctorCountsBySpecialtyAndHospitalResponse();
            foreach (var count in counts)
            {
                response.SpecialtyCounts[count.Key.ToString()] = count.Value;
            }

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorCountsBySpecialtyAndHospital for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.GetServiceTypesByHospitalResponse> GetServiceTypesByHospital(
        Protos.GetServiceTypesByHospitalRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[DoctorGrpcService] GetServiceTypesByHospital called for hospital {HospitalId}", request.HospitalId);

            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid hospital ID format"));
            }

            var serviceTypes = await _doctorService.GetServiceTypesByHospitalAsync(hospitalId);

            var response = new Protos.GetServiceTypesByHospitalResponse();
            foreach (var serviceType in serviceTypes)
            {
                response.ServiceTypes.Add(new Protos.ServiceTypeWithDoctorCountResponse
                {
                    Id = serviceType.ServiceTypeId.ToString(),
                    Name = serviceType.ServiceTypeName,
                    ImageUrl = serviceType.ServiceTypeImageUrl ?? string.Empty,
                    DoctorCount = serviceType.DoctorCount
                });
            }

            _logger.LogInformation("[DoctorGrpcService] Returning {Count} service types for hospital {HospitalId}", serviceTypes.Count, hospitalId);
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetServiceTypesByHospital for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Get doctors with full details by hospital ID (OPTIMIZED - single gRPC call)
    /// Combines GetDoctorAccountIdsByHospitalId + GetDoctorsByAccountIds into one call
    /// </summary>
    public override async Task<Protos.DoctorBatchResponse> GetDoctorsByHospitalId(
        Protos.GetDoctorsByHospitalIdRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[DoctorGrpcService] GetDoctorsByHospitalId called for hospital {HospitalId}", request.HospitalId);

            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid hospital ID format"));
            }

            // Get doctor account IDs for this hospital (optimized query)
            var accountIds = await _doctorService.GetDoctorAccountIdsByHospitalIdAsync(hospitalId);

            if (!accountIds.Any())
            {
                _logger.LogInformation("[DoctorGrpcService] No doctors found for hospital {HospitalId}", hospitalId);
                return new Protos.DoctorBatchResponse();
            }

            // Get doctor details by account IDs (reuse existing optimized method)
            var doctors = await _doctorService.GetDoctorsByAccountIdsAsync(accountIds);

            var response = new Protos.DoctorBatchResponse();
            foreach (var doctor in doctors)
            {
                response.Doctors.Add(new Protos.DoctorBasicInfo
                {
                    AccountId = doctor.AccountId.ToString(),
                    Email = doctor.Email,
                    FullName = doctor.FullName,
                    AvatarUrl = doctor.AvatarUrl,
                    Address = doctor.Address
                });
            }

            _logger.LogInformation("[DoctorGrpcService] Returning {Count} doctors for hospital {HospitalId}",
                doctors.Count, hospitalId);

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorsByHospitalId for hospital {HospitalId}", request.HospitalId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Parse and validate specialty IDs from request
    /// </summary>
    private List<Guid> ParseSpecialtyIds(IEnumerable<string> specialtyIds)
    {
        return specialtyIds
            .Where(id => Guid.TryParse(id, out _))
            .Select(Guid.Parse)
            .ToList();
    }

    /// <summary>
    /// Get doctor ratings from review service
    /// </summary>
    private async Task<Dictionary<Guid, double>> GetDoctorRatingsAsync(List<Guid> doctorIds)
    {
        var ratingMap = new Dictionary<Guid, double>();

        try
        {
            var reviewRequest = new BookingCare.Services.Review.Grpc.BatchDoctorsStatisticsRequest();
            reviewRequest.DoctorIds.AddRange(doctorIds.Select(id => id.ToString()));

            var reviewResponse = await _reviewClient.GetBatchDoctorsStatisticsAsync(reviewRequest);
            foreach (var kvp in reviewResponse.DoctorStatistics)
            {
                if (Guid.TryParse(kvp.Key, out var doctorId))
                {
                    ratingMap[doctorId] = kvp.Value.AverageRating;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DoctorGrpcService] Failed to get review statistics, continuing without ratings");
        }

        return ratingMap;
    }

    /// <summary>
    /// Get hospital information for doctors
    /// </summary>
    private async Task<Dictionary<Guid, (string Name, string Address)>> GetHospitalInfoAsync(List<Guid> hospitalIds)
    {
        var hospitalMap = new Dictionary<Guid, (string Name, string Address)>();

        if (!hospitalIds.Any())
        {
            return hospitalMap;
        }

        try
        {
            var hospitalRequest = new BookingCare.Services.Hospital.GetHospitalsBasicInfoRequest();
            hospitalRequest.Ids.AddRange(hospitalIds.Select(id => id.ToString()));

            var hospitalResponse = await _hospitalClient.GetHospitalsBasicInfoAsync(hospitalRequest);
            foreach (var hospital in hospitalResponse.Hospitals)
            {
                if (Guid.TryParse(hospital.Id, out var hospitalId))
                {
                    hospitalMap[hospitalId] = (hospital.Name, hospital.Address);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DoctorGrpcService] Failed to get hospital info, continuing without hospital names");
        }

        return hospitalMap;
    }

    /// <summary>
    /// Get doctor prices
    /// </summary>
    private async Task<Dictionary<Guid, (decimal Amount, string ServiceTypeName)>> GetDoctorPricesAsync(
        List<Guid> doctorIds,
        List<DoctorEntity> doctors)
    {
        var priceMap = new Dictionary<Guid, (decimal Amount, string ServiceTypeName)>();

        try
        {
            // Get prices for "IN_PERSON" service type
            var prices = await _doctorService.GetDoctorsPricesByServiceTypeAsync(doctorIds, "IN_PERSON");
            foreach (var kvp in prices)
            {
                priceMap[kvp.Key] = (kvp.Value, "IN_PERSON");
            }

            // Get prices for doctors without IN_PERSON price
            await GetAlternativePricesAsync(doctorIds, doctors, priceMap);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DoctorGrpcService] Failed to get doctor prices, continuing without prices");
        }

        return priceMap;
    }

    /// <summary>
    /// Get alternative prices for doctors without IN_PERSON price
    /// </summary>
    private async Task GetAlternativePricesAsync(
        List<Guid> doctorIds,
        List<DoctorEntity> doctors,
        Dictionary<Guid, (decimal Amount, string ServiceTypeName)> priceMap)
    {
        var doctorsWithoutPrice = doctorIds.Where(id => !priceMap.ContainsKey(id)).ToList();
        if (!doctorsWithoutPrice.Any())
        {
            return;
        }

        foreach (var doctor in doctors.Where(d => doctorsWithoutPrice.Contains(d.Id)))
        {
            var doctorPrices = await _doctorService.GetDoctorPricesAsync(doctor.Id);
            var firstPrice = doctorPrices.FirstOrDefault();
            if (firstPrice != null)
            {
                var serviceTypeName = !string.IsNullOrWhiteSpace(firstPrice.ServiceTypeName)
                    ? firstPrice.ServiceTypeName
                    : "Khám chuyên khoa";
                priceMap[doctor.Id] = (firstPrice.Amount, serviceTypeName);
            }
        }
    }

    /// <summary>
    /// Get specialty names for doctors
    /// </summary>
    private async Task<Dictionary<Guid, string>> GetSpecialtyNamesAsync(List<DoctorEntity> doctors)
    {
        var specialtyMap = new Dictionary<Guid, string>();
        var specialtyIds = doctors
            .Where(d => d.SpecialtyId.HasValue)
            .Select(d => d.SpecialtyId!.Value)
            .Distinct()
            .ToList();

        if (!specialtyIds.Any())
        {
            return specialtyMap;
        }

        try
        {
            var specialties = await _specialtyService.GetSpecialtiesByIdsAsync(specialtyIds);
            foreach (var specialty in specialties)
            {
                specialtyMap[specialty.Id] = specialty.Name;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DoctorGrpcService] Failed to get specialty names");
        }

        return specialtyMap;
    }

    /// <summary>
    /// Build doctor recommendation info
    /// </summary>
    private Protos.DoctorRecommendationInfo BuildDoctorRecommendationInfo(
        DoctorEntity doctor,
        Dictionary<Guid, double> ratingMap,
        Dictionary<Guid, (string Name, string Address)> hospitalMap,
        Dictionary<Guid, (decimal Amount, string ServiceTypeName)> priceMap,
        Dictionary<Guid, string> specialtyMap)
    {
        var specialtyName = doctor.SpecialtyId.HasValue && specialtyMap.TryGetValue(doctor.SpecialtyId.Value, out var name)
            ? name
            : string.Empty;

        var doctorInfo = new Protos.DoctorRecommendationInfo
        {
            Id = doctor.Id.ToString(),
            FullName = $"{doctor.FirstName} {doctor.LastName}".Trim(),
            SpecialtyName = specialtyName,
            YearsOfExperience = doctor.YearsOfExperience,
            AvatarUrl = doctor.AvatarUrl ?? string.Empty
        };

        // Add rating
        if (ratingMap.TryGetValue(doctor.Id, out var rating))
        {
            doctorInfo.Rating = rating;
        }

        // Add hospital info
        if (doctor.HospitalId.HasValue && hospitalMap.TryGetValue(doctor.HospitalId.Value, out var hospitalInfo))
        {
            doctorInfo.HospitalId = doctor.HospitalId.Value.ToString();
            doctorInfo.HospitalName = hospitalInfo.Name;
        }

        // Add price info
        if (priceMap.TryGetValue(doctor.Id, out var priceInfo))
        {
            doctorInfo.ConsultationFee = (double)priceInfo.Amount;
            doctorInfo.ServiceTypeName = priceInfo.ServiceTypeName;
        }

        return doctorInfo;
    }

    public override async Task<Protos.FilterDoctorsForRecommendationResponse> FilterDoctorsForRecommendation(
        Protos.FilterDoctorsForRecommendationRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[DoctorGrpcService] FilterDoctorsForRecommendation called - Specialties: {SpecialtyIds}, Location: {ProvinceId}/{DistrictId}, MaxResults: {MaxResults}",
                string.Join(", ", request.SpecialtyIds), request.ProvinceId, request.DistrictId, request.MaxResults);

            // Parse and validate specialty IDs
            var specialtyIds = ParseSpecialtyIds(request.SpecialtyIds);
            if (!specialtyIds.Any())
            {
                _logger.LogWarning("[DoctorGrpcService] No valid specialty IDs provided");
                return new Protos.FilterDoctorsForRecommendationResponse();
            }

            // Get max results (default: 10)
            var maxResults = request.MaxResults > 0 ? request.MaxResults : 10;

            // Filter doctors
            var doctors = await _doctorService.FilterDoctorsForRecommendationAsync(
                specialtyIds,
                string.IsNullOrWhiteSpace(request.ProvinceId) ? null : request.ProvinceId,
                string.IsNullOrWhiteSpace(request.DistrictId) ? null : request.DistrictId,
                maxResults
            );

            if (!doctors.Any())
            {
                _logger.LogInformation("[DoctorGrpcService] No doctors found matching criteria");
                return new Protos.FilterDoctorsForRecommendationResponse();
            }

            // Get enrichment data
            var doctorIds = doctors.Select(d => d.Id).ToList();
            var ratingMap = await GetDoctorRatingsAsync(doctorIds);

            var hospitalIds = doctors
                .Where(d => d.HospitalId.HasValue)
                .Select(d => d.HospitalId!.Value)
                .Distinct()
                .ToList();
            var hospitalMap = await GetHospitalInfoAsync(hospitalIds);

            var priceMap = await GetDoctorPricesAsync(doctorIds, doctors);

            // Get specialty names
            var specialtyMap = await GetSpecialtyNamesAsync(doctors);

            // Build response
            var response = new Protos.FilterDoctorsForRecommendationResponse
            {
                TotalCount = doctors.Count
            };

            foreach (var doctor in doctors)
            {
                var doctorInfo = BuildDoctorRecommendationInfo(doctor, ratingMap, hospitalMap, priceMap, specialtyMap);
                response.Doctors.Add(doctorInfo);
            }

            _logger.LogInformation("[DoctorGrpcService] Returning {Count} doctors for AI recommendations", response.Doctors.Count);
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in FilterDoctorsForRecommendation");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.GetAllSpecialtiesResponse> GetAllSpecialties(Protos.GetAllSpecialtiesRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[DoctorGrpcService] Getting all active specialties");

            // Get all active specialties
            var specialties = await _specialtyService.GetActiveSpecialtiesSimpleAsync();

            var response = new Protos.GetAllSpecialtiesResponse();

            foreach (var specialty in specialties)
            {
                response.Specialties.Add(new Protos.SpecialtySimpleResponse
                {
                    Id = specialty.Id.ToString(),
                    Name = specialty.Name,
                    ImageUrl = specialty.ImageUrl ?? string.Empty
                });
            }

            _logger.LogInformation("[DoctorGrpcService] Returning {Count} active specialties", response.Specialties.Count);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetAllSpecialties");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Get doctors for assignment with full info (rating, experience)
    /// Used by hospital staff to assign doctor to pending appointments
    /// Note: Sorting by booking count will be done at Appointment Service level
    /// </summary>
    public override async Task<Protos.GetDoctorsForAssignmentResponse> GetDoctorsForAssignment(
        Protos.GetDoctorsForAssignmentRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "[DoctorGrpcService] GetDoctorsForAssignment called - Hospital: {HospitalId}, Specialty: {SpecialtyId}, Type: {AppointmentType}, DoctorIds: {DoctorIds}",
                request.HospitalId, request.SpecialtyId, request.AppointmentType,
                request.DoctorIds.Count > 0 ? string.Join(", ", request.DoctorIds) : "all");

            // Validate required fields
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                return new Protos.GetDoctorsForAssignmentResponse
                {
                    Success = false,
                    Message = "Invalid hospital ID format"
                };
            }

            if (!Guid.TryParse(request.SpecialtyId, out var specialtyId))
            {
                return new Protos.GetDoctorsForAssignmentResponse
                {
                    Success = false,
                    Message = INVALID_SPECIALTY_ID_FORMAT
                };
            }

            var appointmentType = request.AppointmentType?.ToUpperInvariant() ?? "IN_PERSON";

            // Parse optional doctor IDs filter
            var filterDoctorIds = request.DoctorIds
                .Where(id => Guid.TryParse(id, out _))
                .Select(Guid.Parse)
                .ToList();

            // Get doctors using DoctorService methods (all logic handled there)
            List<Models.DTOs.Responses.DoctorForAssignmentResponse> doctors;
            if (filterDoctorIds.Any())
            {
                // Filter by specific doctor IDs (for previous doctors section)
                doctors = await _doctorService.GetDoctorsByIdsForAssignmentAsync(filterDoctorIds, appointmentType);
            }
            else
            {
                // Get all doctors by hospital and specialty
                doctors = await _doctorService.GetDoctorsForAssignmentAsync(hospitalId, specialtyId, appointmentType);
            }

            if (!doctors.Any())
            {
                _logger.LogInformation("[DoctorGrpcService] No doctors found for hospital {HospitalId} and specialty {SpecialtyId}",
                    hospitalId, specialtyId);
                return new Protos.GetDoctorsForAssignmentResponse
                {
                    Success = true,
                    Message = "No doctors found",
                    TotalCount = 0
                };
            }

            // Build response (no sorting here - will be done at Appointment Service with booking count)
            var response = new Protos.GetDoctorsForAssignmentResponse
            {
                Success = true,
                Message = "Doctors retrieved successfully"
            };

            foreach (var doctor in doctors)
            {
                var doctorInfo = new Protos.DoctorForAssignmentInfo
                {
                    Id = doctor.Id.ToString(),
                    AccountId = doctor.AccountId.ToString(),
                    FullName = doctor.FullName,
                    AvatarUrl = doctor.AvatarUrl,
                    PositionName = doctor.PositionName,
                    SpecialtyName = doctor.SpecialtyName,
                    YearsOfExperience = doctor.YearsOfExperience,
                    Rating = doctor.Rating,
                    ReviewCount = doctor.ReviewCount,
                    ConsultationFee = (double)doctor.ConsultationFee,
                    IsActive = doctor.IsActive
                };
                response.Doctors.Add(doctorInfo);
            }

            response.TotalCount = response.Doctors.Count;

            _logger.LogInformation("[DoctorGrpcService] Returning {Count} doctors for assignment", response.Doctors.Count);
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DoctorGrpcService] Error in GetDoctorsForAssignment");
            return new Protos.GetDoctorsForAssignmentResponse
            {
                Success = false,
                Message = "Internal server error"
            };
        }
    }
}
