using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Services.Hospital.Enums;
using BookingCare.Shared.FileUpload.Services;
using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.FileUpload.Helpers;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Common.Services;
using System.Security.Cryptography;

namespace BookingCare.Services.Hospital.Services.Implementations;

public class HospitalRegistrationService : BaseService, IHospitalRegistrationService
{
    private readonly IHospitalRegistrationRepository _registrationRepository;
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly IEventBus _eventBus;
    private readonly IContractGenerationService _contractGenerationService;
    private readonly IContractSigningService _contractSigningService;
    private readonly IContractSigningTokenRepository _tokenRepository;

    public HospitalRegistrationService(
        IHospitalRegistrationRepository registrationRepository,
        FileUploadOrchestrator uploadOrchestrator,
        IEventBus eventBus,
        IContractGenerationService contractGenerationService,
        IContractSigningService contractSigningService,
        IContractSigningTokenRepository tokenRepository,
        ILogger<HospitalRegistrationService> logger) : base(logger)
    {
        _registrationRepository = registrationRepository;
        _uploadOrchestrator = uploadOrchestrator;
        _eventBus = eventBus;
        _contractGenerationService = contractGenerationService;
        _contractSigningService = contractSigningService;
        _tokenRepository = tokenRepository;
    }

    public async Task<HospitalRegistrationResponseDto> CreateRegistrationAsync(CreateHospitalRegistrationRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating hospital partnership registration for {HospitalName}", null, request.HospitalName);

            // Check if hospital email already exists
            var existingByHospitalEmail = await _registrationRepository.GetByHospitalEmailAsync(request.HospitalEmail);

            if (existingByHospitalEmail != null && existingByHospitalEmail.Status == RegistrationStatus.CONFIRMED)
            {
                throw new DuplicateRegistrationException($"Email bệnh viện {request.HospitalEmail} đã được sử dụng cho đơn đăng ký khác");
            }

            // Check if representative email already exists
            var existingByRepEmail = await _registrationRepository.GetByRepresentativeEmailAsync(request.RepresentativeEmail);

            if (existingByRepEmail != null && existingByRepEmail.Status == RegistrationStatus.CONFIRMED)
            {
                throw new DuplicateRegistrationException($"Email người đại diện {request.RepresentativeEmail} đã được sử dụng cho đơn đăng ký khác");
            }

            // Check if tax code already exists
            var existingByTaxCode = await _registrationRepository.GetByTaxCodeAsync(request.TaxCode);
            if (existingByTaxCode != null && existingByTaxCode.Status == RegistrationStatus.CONFIRMED)
            {
                throw new DuplicateRegistrationException($"Mã số thuế {request.TaxCode} đã được sử dụng cho đơn đăng ký khác");
            }

            // Create registration entity with placeholder URLs (files will be uploaded asynchronously)
            var registration = new HospitalRegistrationEntity
            {
                RepresentativeName = request.RepresentativeName,
                RepresentativeEmail = request.RepresentativeEmail,
                RepresentativePhone = request.RepresentativePhone,
                HospitalName = request.HospitalName,
                HospitalEmail = request.HospitalEmail,
                HospitalPhone = request.HospitalPhone,
                Address = request.Address,
                LicenseFile = "PENDING_UPLOAD", // Placeholder - will be updated by event handler
                BusinessCertificateFile = "PENDING_UPLOAD",
                IdentityCardFile = "PENDING_UPLOAD",
                TaxCode = request.TaxCode,
                Status = RegistrationStatus.PENDING,

                // eKYC Information (Privacy-friendly: only verification status, no PII)
                EkycSessionId = request.EkycSessionId,
                FaceMatchScore = request.FaceMatchScore,
                LivenessScore = request.LivenessScore,
                EkycStatus = request.IsEkycVerified == true 
                    ? EkycStatus.VERIFIED
                    : EkycStatus.NOT_STARTED,
                EkycVerifiedAt = request.IsEkycVerified == true
                    ? DateTime.UtcNow
                    : null,
            };

            var created = await _registrationRepository.CreateAsync(registration);

            // Convert files to byte arrays for event
            var licenseFileData = await ConvertFormFileToByteArray(request.LicenseFile);
            var businessCertFileData = await ConvertFormFileToByteArray(request.BusinessCertificateFile);
            var identityCardFileData = await ConvertFormFileToByteArray(request.IdentityCardFile);

            // Publish event for async file upload
            var fileUploadEvent = new HospitalRegistrationFilesUploadEvent
            {
                RegistrationId = created.Id,
                LicenseFile = new FileUploadData
                {
                    FileName = FileNameSanitizer.Sanitize(request.LicenseFile.FileName),
                    ContentType = request.LicenseFile.ContentType,
                    FileData = licenseFileData,
                    Folder = "hospital-registrations/license-files",
                    EntityType = "LicenseFile"
                },
                BusinessCertificateFile = new FileUploadData
                {
                    FileName = FileNameSanitizer.Sanitize(request.BusinessCertificateFile.FileName),
                    ContentType = request.BusinessCertificateFile.ContentType,
                    FileData = businessCertFileData,
                    Folder = "hospital-registrations/business-certificates",
                    EntityType = "BusinessCertificateFile"
                },
                IdentityCardFile = new FileUploadData
                {
                    FileName = FileNameSanitizer.Sanitize(request.IdentityCardFile.FileName),
                    ContentType = request.IdentityCardFile.ContentType,
                    FileData = identityCardFileData,
                    Folder = "hospital-registrations/identity-cards",
                    EntityType = "IdentityCardFile"
                }
            };

            await _eventBus.PublishAsync(fileUploadEvent);

            // Publish event for email notification
            var emailEvent = new HospitalRegistrationSubmittedEvent
            {
                RegistrationId = created.Id,
                RepresentativeName = created.RepresentativeName,
                RepresentativeEmail = created.RepresentativeEmail,
                RepresentativePhone = created.RepresentativePhone,
                HospitalName = created.HospitalName,
                HospitalEmail = created.HospitalEmail,
                HospitalPhone = created.HospitalPhone,
                Address = created.Address,
                TaxCode = created.TaxCode,
                SubmittedAt = created.CreatedAt
            };

            await _eventBus.PublishAsync(emailEvent);

            LogInfo("Hospital registration created successfully with ID {RegistrationId}. Files will be uploaded asynchronously.", null, created.Id);

            return MapToResponseDto(created);

        }, "CreateRegistration");
    }

    public async Task<HospitalRegistrationResponseDto> GetRegistrationByIdAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var registration = await _registrationRepository.GetByIdAsync(id);
            if (registration == null)
            {
                throw new HospitalRegistrationNotFoundException(id);
            }

            return MapToResponseDto(registration);

        }, "GetRegistrationById");
    }

    public async Task<HospitalRegistrationListResponseDto> GetAllRegistrationsAsync(HospitalRegistrationFilterRequestDto filter)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            RegistrationStatus? status = filter.Status.HasValue
                ? filter.Status.Value
                : null;

            var queryParameters = new HospitalRegistrationQueryParameters
            {
                SearchTerm = filter.SearchTerm,
                Status = status,
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                Page = filter.Page,
                PageSize = filter.PageSize,
                SortBy = filter.SortBy,
                SortOrder = filter.SortOrder
            };

            var (registrations, totalCount) = await _registrationRepository.GetAllAsync(queryParameters);

            var registrationDtos = registrations.Select(MapToResponseDto).ToList();

            return new HospitalRegistrationListResponseDto
            {
                Registrations = registrationDtos,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };

        }, "GetAllRegistrations");
    }

    public async Task<HospitalRegistrationResponseDto> UpdateRegistrationStatusAsync(
        Guid id,
        UpdateRegistrationStatusRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating registration status for {RegistrationId} to {Status}", null, id, request.Status);

            var registration = await _registrationRepository.GetByIdAsync(id);
            if (registration == null)
            {
                throw new HospitalRegistrationNotFoundException(id);
            }

            var newStatus = (RegistrationStatus)request.Status;

            // Validate status transition
            if (registration.Status == newStatus)
            {
                throw new InvalidRegistrationStatusException("Trạng thái mới phải khác với trạng thái hiện tại");
            }

            // Validate required fields based on status
            if (newStatus == RegistrationStatus.CANCELLED && string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new HospitalRegistrationValidationException("Lý do từ chối là bắt buộc khi hủy đơn đăng ký");
            }

            if (newStatus == RegistrationStatus.CONFIRMED && !request.HospitalId.HasValue)
            {
                throw new HospitalRegistrationValidationException("Hospital ID là bắt buộc khi xác nhận đơn đăng ký");
            }

            // Upload contract file if provided
            string? contractFileUrl = null;
            if (request.ContractFile != null)
            {
                var contractFileResult = await UploadFileAsync(
                    request.ContractFile,
                    "hospital-registrations/contracts",
                    "ContractFile");

                if (contractFileResult.Success)
                {
                    contractFileUrl = contractFileResult.UploadResult!.CloudFrontUrl ?? contractFileResult.UploadResult!.FileUrl;
                }
            }

            // Update registration
            registration.Status = newStatus;
            registration.Reason = request.Reason;
            registration.ContractFile = contractFileUrl ?? registration.ContractFile;
            registration.HospitalId = request.HospitalId;
            registration.UpdatedAt = DateTime.Now;

            var updated = await _registrationRepository.UpdateAsync(registration);

            // Publish event for email notification
            var integrationEvent = new HospitalRegistrationStatusUpdatedEvent
            {
                RegistrationId = updated.Id,
                RepresentativeName = updated.RepresentativeName,
                RepresentativeEmail = updated.RepresentativeEmail,
                HospitalName = updated.HospitalName,
                HospitalEmail = updated.HospitalEmail,
                Status = (int)updated.Status,
                StatusText = GetStatusText(updated.Status),
                Reason = updated.Reason,
                ContractFileUrl = updated.ContractFile,
                HospitalId = updated.HospitalId,
                UpdatedAt = updated.UpdatedAt
            };

            await _eventBus.PublishAsync(integrationEvent);

            LogInfo("Registration status updated successfully for {RegistrationId}", null, id);

            return MapToResponseDto(updated);

        }, "UpdateRegistrationStatus");
    }

    public async Task<HospitalRegistrationResponseDto> UpdateRegistrationAsync(
        Guid id,
        UpdateRegistrationRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating registration for {RegistrationId}", null, id);

            var registration = await _registrationRepository.GetByIdAsync(id);
            if (registration == null)
            {
                throw new HospitalRegistrationNotFoundException(id);
            }

            // Only allow updates for confirmed registrations
            if (registration.Status != RegistrationStatus.CONFIRMED)
            {
                throw new InvalidRegistrationStatusException("Chỉ có thể cập nhật đơn đăng ký đã được phê duyệt");
            }

            // Upload new contract file
            var contractFileResult = await UploadFileAsync(
                request.ContractFile,
                "hospital-registrations/contracts",
                "ContractFile");

            if (!contractFileResult.Success)
            {
                throw new FileUploadException(
                    contractFileResult.ErrorMessage ?? "Không thể tải lên file hợp đồng");
            }

            // Update contract file URL
            registration.ContractFile = contractFileResult.UploadResult!.CloudFrontUrl ?? contractFileResult.UploadResult!.FileUrl;

            var updated = await _registrationRepository.UpdateAsync(registration);

            LogInfo("Registration updated successfully for {RegistrationId}", null, id);

            return MapToResponseDto(updated);

        }, "UpdateRegistration");
    }

    public async Task<bool> DeleteRegistrationAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var exists = await _registrationRepository.ExistsAsync(id);
            if (!exists)
            {
                throw new HospitalRegistrationNotFoundException(id);
            }

            var result = await _registrationRepository.DeleteAsync(id);
            LogInfo("Registration deleted successfully: {RegistrationId}", null, id);

            return result;

        }, "DeleteRegistration");
    }

    public async Task<HospitalRegistrationResponseDto> ApproveRegistrationAsync(
        Guid id,
        ApproveRegistrationRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Approving hospital registration {RegistrationId}", null, id);

            var registration = await _registrationRepository.GetByIdAsync(id);
            if (registration == null)
            {
                throw new HospitalRegistrationNotFoundException(id);
            }

            // Check if already approved
            if (registration.Status == RegistrationStatus.CONFIRMED)
            {
                throw new InvalidRegistrationStatusException("Đơn đăng ký đã được phê duyệt trước đó");
            }

            // Check if cancelled
            if (registration.Status == RegistrationStatus.CANCELLED)
            {
                throw new InvalidRegistrationStatusException("Không thể phê duyệt đơn đăng ký đã bị từ chối");
            }

            // NEW WORKFLOW: Contract must be signed before approval
            if (registration.Status != RegistrationStatus.CONTRACT_SIGNED)
            {
                throw new InvalidRegistrationStatusException(
                    $"Không thể phê duyệt đơn đăng ký với trạng thái {registration.Status}. " +
                    "Hợp đồng phải được bệnh viện ký trước khi phê duyệt.");
            }

            // Verify contract file exists (should be set when hospital signed)
            if (string.IsNullOrEmpty(registration.ContractFile))
            {
                throw new InvalidRegistrationStatusException(
                    "File hợp đồng đã ký không tồn tại. Vui lòng kiểm tra lại.");
            }

            // Update registration status to CONFIRMED
            registration.Status = RegistrationStatus.CONFIRMED;

            var updated = await _registrationRepository.UpdateAsync(registration);

            // Generate a strong password for the hospital account
            var generatedPassword = GenerateStrongPassword(16);

            // Publish event to trigger Hospital Account Creation Saga in Auth Service
            var accountCreationEvent = new HospitalAccountCreationRequestedEvent
            {
                RegistrationId = updated.Id,
                RepresentativeName = updated.RepresentativeName,
                RepresentativeEmail = updated.RepresentativeEmail,
                RepresentativePhone = updated.RepresentativePhone,
                HospitalName = updated.HospitalName,
                HospitalEmail = updated.HospitalEmail,
                HospitalPhone = updated.HospitalPhone,
                Address = updated.Address,
                TaxCode = updated.TaxCode,
                ContractFileUrl = updated.ContractFile!, // Use the signed contract file
                GeneratedPassword = generatedPassword
            };

            await _eventBus.PublishAsync(accountCreationEvent);

            LogInfo("Hospital registration approved successfully: {RegistrationId}. Account creation saga triggered.", null, id);

            return MapToResponseDto(updated);

        }, "ApproveRegistration");
    }

    public async Task<HospitalRegistrationResponseDto> RejectRegistrationAsync(
        Guid id,
        RejectRegistrationRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Rejecting hospital registration {RegistrationId}", null, id);

            var registration = await _registrationRepository.GetByIdAsync(id);
            if (registration == null)
            {
                throw new HospitalRegistrationNotFoundException(id);
            }

            // Check if already cancelled
            if (registration.Status == RegistrationStatus.CANCELLED)
            {
                throw new InvalidRegistrationStatusException("Đơn đăng ký đã bị từ chối trước đó");
            }

            // Check if already confirmed
            if (registration.Status == RegistrationStatus.CONFIRMED)
            {
                throw new InvalidRegistrationStatusException("Không thể từ chối đơn đăng ký đã được phê duyệt");
            }

            // Update registration status to CANCELLED with reason
            registration.Status = RegistrationStatus.CANCELLED;
            registration.Reason = request.Reason;

            var updated = await _registrationRepository.UpdateAsync(registration);

            // Publish event for email notification
            var statusUpdatedEvent = new HospitalRegistrationStatusUpdatedEvent
            {
                RegistrationId = updated.Id,
                RepresentativeName = updated.RepresentativeName,
                RepresentativeEmail = updated.RepresentativeEmail,
                HospitalName = updated.HospitalName,
                HospitalEmail = updated.HospitalEmail,
                Status = (int)updated.Status,
                Reason = updated.Reason,
                ContractFileUrl = updated.ContractFile,
                UpdatedAt = updated.UpdatedAt
            };

            await _eventBus.PublishAsync(statusUpdatedEvent);

            LogInfo("Hospital registration rejected successfully: {RegistrationId}", null, id);

            return MapToResponseDto(updated);

        }, "RejectRegistration");
    }

    private HospitalRegistrationResponseDto MapToResponseDto(HospitalRegistrationEntity entity)
    {
        return new HospitalRegistrationResponseDto
        {
            Id = entity.Id,
            RepresentativeName = entity.RepresentativeName,
            RepresentativeEmail = entity.RepresentativeEmail,
            RepresentativePhone = entity.RepresentativePhone,
            HospitalName = entity.HospitalName,
            HospitalEmail = entity.HospitalEmail,
            HospitalPhone = entity.HospitalPhone,
            Address = entity.Address,
            LicenseFile = entity.LicenseFile,
            BusinessCertificateFile = entity.BusinessCertificateFile,
            IdentityCardFile = entity.IdentityCardFile,
            TaxCode = entity.TaxCode,
            Status = entity.Status,
            StatusText = GetStatusText(entity.Status),

            // eKYC Information (Privacy-friendly: only verification status, no PII)
            EkycStatus = entity.EkycStatus,
            EkycStatusText = GetEkycStatusText(entity.EkycStatus),
            EkycSessionId = entity.EkycSessionId,
            EkycVerifiedAt = entity.EkycVerifiedAt,
            FaceMatchScore = entity.FaceMatchScore,
            LivenessScore = entity.LivenessScore,

            // Contract Information
            ContractNumber = entity.ContractNumber,
            ContractFile = entity.ContractFile,
            ContractDraftFile = entity.ContractDraftFile,
            HospitalSignature = entity.HospitalSignature,
            SignedAt = entity.SignedAt,
            AdminSignatureId = entity.AdminSignatureId,

            HospitalId = entity.HospitalId,
            Reason = entity.Reason,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static string GetStatusText(RegistrationStatus status)
    {
        return status switch
        {
            RegistrationStatus.PENDING => "Đang chờ xử lý",
            RegistrationStatus.CONTRACT_GENERATED => "Đã tạo hợp đồng",
            RegistrationStatus.CONTRACT_SIGNED => "Đã ký hợp đồng",
            RegistrationStatus.CONFIRMED => "Đã xác nhận",
            RegistrationStatus.CANCELLED => "Đã hủy",
            _ => "Không xác định"
        };
    }

    private static string GetEkycStatusText(EkycStatus status)
    {
        return status switch
        {
            EkycStatus.NOT_STARTED => "Chưa xác thực",
            EkycStatus.IN_PROGRESS => "Đang xác thực",
            EkycStatus.VERIFIED => "Đã xác thực",
            EkycStatus.FAILED => "Xác thực thất bại",
            _ => "Không xác định"
        };
    }

    private async Task<(bool Success, FileUploadResult? UploadResult, string? ErrorMessage)> UploadFileAsync(
        IFormFile file,
        string folder,
        string entityType)
    {
        // Note: FileUploadOrchestrator automatically sanitizes filenames to prevent ASCII issues
        var config = new FileUploadConfig
        {
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" },
            MaxSizeInMB = 10,
            Folder = folder,
            SuccessMessage = $"{entityType} uploaded successfully",
            EntityType = entityType
        };

        var uploadResult = await _uploadOrchestrator.UploadFileAsync(
            file,
            config,
            Guid.Empty,
            Logger,
            CancellationToken.None);

        return (uploadResult.Success, uploadResult.UploadResult, uploadResult.ErrorMessage);
    }

    private static async Task<byte[]> ConvertFormFileToByteArray(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private static string GenerateStrongPassword(int length)
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string specialChars = "@#$%^&*()_+-=[]{}|;:,.<>?";
        const string allChars = upperCase + lowerCase + digits + specialChars;

        using var rng = RandomNumberGenerator.Create();
        var password = new char[length];

        // Ensure at least one character from each category
        password[0] = upperCase[GetSecureRandomNumber(rng, upperCase.Length)];
        password[1] = lowerCase[GetSecureRandomNumber(rng, lowerCase.Length)];
        password[2] = digits[GetSecureRandomNumber(rng, digits.Length)];
        password[3] = specialChars[GetSecureRandomNumber(rng, specialChars.Length)];

        // Fill the rest with random characters
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[GetSecureRandomNumber(rng, allChars.Length)];
        }

        // Shuffle the password
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = GetSecureRandomNumber(rng, i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }

    private static int GetSecureRandomNumber(RandomNumberGenerator rng, int max)
    {
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var randomValue = BitConverter.ToUInt32(bytes, 0);
        return (int)(randomValue % (uint)max);
    }

    public async Task<GenerateContractForRegistrationResponseDto> GenerateContractAsync(Guid registrationId, string adminId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Generating contract for registration: {RegistrationId}", null, registrationId);

            // Get registration
            var registration = await _registrationRepository.GetByIdAsync(registrationId);
            if (registration == null)
            {
                throw new HospitalRegistrationNotFoundException(registrationId);
            }

            // Validate status - must be PENDING
            if (registration.Status != RegistrationStatus.PENDING)
            {
                throw new InvalidRegistrationStatusException(
                    $"Cannot generate contract for registration with status {registration.Status}. Status must be PENDING.");
            }

            // Generate contract
            var contractResult = await _contractGenerationService.GenerateContractAsync(registrationId, adminId);

            // Update registration with contract draft and dates
            registration.ContractDraftFile = contractResult.ContractFileUrl;
            registration.AdminSignatureId = contractResult.AdminSignatureId;
            registration.ContractNumber = contractResult.ContractNumber;
            registration.ContractDate = contractResult.GeneratedAt;
            registration.ContractEffectiveDate = contractResult.GeneratedAt;
            registration.ContractExpiryDate = contractResult.GeneratedAt.AddYears(1);
            registration.Status = RegistrationStatus.CONTRACT_GENERATED;

            await _registrationRepository.UpdateAsync(registration);

            // Generate signing token and link
            var signingLink = await _contractSigningService.GetSigningLinkAsync(registrationId);

            // Get token expiry
            var token = await _tokenRepository.GetActiveTokenByRegistrationIdAsync(registrationId);
            var linkExpiresAt = token?.ExpiresAt ?? DateTime.UtcNow.AddDays(7);

            // Publish event to send email with signing link
            await PublishContractGeneratedEventAsync(registration, signingLink, linkExpiresAt);

            LogInfo("Successfully generated contract for registration: {RegistrationId}", null, registrationId);

            return new GenerateContractForRegistrationResponseDto
            {
                RegistrationId = registrationId,
                ContractFileUrl = contractResult.ContractFileUrl,
                ContractNumber = contractResult.ContractNumber,
                SigningLink = signingLink,
                GeneratedAt = contractResult.GeneratedAt,
                LinkExpiresAt = linkExpiresAt,
                Status = registration.Status.ToString()
            };
        }, "GenerateContractForRegistration");
    }

    private async Task PublishContractGeneratedEventAsync(
        HospitalRegistrationEntity registration,
        string signingLink,
        DateTime linkExpiresAt)
    {
        try
        {
            var @event = new HospitalContractGeneratedEvent
            {
                RegistrationId = registration.Id,
                HospitalName = registration.HospitalName,
                HospitalEmail = registration.HospitalEmail,
                HospitalPhone = registration.HospitalPhone,
                Address = registration.Address,
                TaxCode = registration.TaxCode,
                RepresentativeName = registration.RepresentativeName,
                RepresentativeEmail = registration.RepresentativeEmail,
                RepresentativePhone = registration.RepresentativePhone,
                ContractNumber = registration.ContractNumber ?? string.Empty,
                ContractDraftUrl = registration.ContractDraftFile ?? string.Empty,
                SigningLink = signingLink,
                LinkExpiresAt = linkExpiresAt
            };

            await _eventBus.PublishAsync(@event);

            LogInfo("Published HospitalContractGeneratedEvent for registration: {RegistrationId}", null, registration.Id);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error publishing contract generated event for registration: {RegistrationId}", null, registration.Id);
            // Don't throw - event publishing failure shouldn't break the contract generation
        }
    }
}
