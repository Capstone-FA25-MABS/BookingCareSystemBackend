using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Services.Hospital.Enums;
using BookingCare.Shared.FileUpload.Services;
using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Hospital.Services.Implementations;

public class HospitalRegistrationService : BaseService, IHospitalRegistrationService
{
    private readonly IHospitalRegistrationRepository _registrationRepository;
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly IEventBus _eventBus;

    public HospitalRegistrationService(
        IHospitalRegistrationRepository registrationRepository,
        FileUploadOrchestrator uploadOrchestrator,
        IEventBus eventBus,
        ILogger<HospitalRegistrationService> logger) : base(logger)
    {
        _registrationRepository = registrationRepository;
        _uploadOrchestrator = uploadOrchestrator;
        _eventBus = eventBus;
    }

    public async Task<HospitalRegistrationResponseDto> CreateRegistrationAsync(CreateHospitalRegistrationRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating hospital partnership registration for {HospitalName}", null, request.HospitalName);

            // Check if email already exists
            var existingByEmail = await _registrationRepository.GetByEmailAsync(request.Email);

            if (existingByEmail != null && existingByEmail.Status == RegistrationStatus.CONFIRMED)
            {
                throw new HospitalRegistrationException($"Email {request.Email} đã được sử dụng cho đơn đăng ký khác");
            }

            // Check if tax code already exists
            var existingByTaxCode = await _registrationRepository.GetByTaxCodeAsync(request.TaxCode);
            if (existingByTaxCode != null && existingByTaxCode.Status == RegistrationStatus.CONFIRMED)
            {
                throw new HospitalRegistrationException($"Mã số thuế {request.TaxCode} đã được sử dụng cho đơn đăng ký khác");
            }

            // Create registration entity with placeholder URLs (files will be uploaded asynchronously)
            var registration = new HospitalRegistrationEntity
            {
                HospitalName = request.HospitalName,
                Email = request.Email,
                Phone = request.Phone,
                Address = request.Address,
                LicenseFile = "PENDING_UPLOAD", // Placeholder - will be updated by event handler
                BusinessCertificateFile = "PENDING_UPLOAD",
                IdentityCardFile = "PENDING_UPLOAD",
                TaxCode = request.TaxCode,
                Status = RegistrationStatus.PENDING,
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
                    FileName = request.LicenseFile.FileName,
                    ContentType = request.LicenseFile.ContentType,
                    FileData = licenseFileData,
                    Folder = "hospital-registrations/license-files",
                    EntityType = "LicenseFile"
                },
                BusinessCertificateFile = new FileUploadData
                {
                    FileName = request.BusinessCertificateFile.FileName,
                    ContentType = request.BusinessCertificateFile.ContentType,
                    FileData = businessCertFileData,
                    Folder = "hospital-registrations/business-certificates",
                    EntityType = "BusinessCertificateFile"
                },
                IdentityCardFile = new FileUploadData
                {
                    FileName = request.IdentityCardFile.FileName,
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
                HospitalName = created.HospitalName,
                Email = created.Email,
                Phone = created.Phone,
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

            var (registrations, totalCount) = await _registrationRepository.GetAllAsync(
                filter.SearchTerm,
                status,
                filter.FromDate,
                filter.ToDate,
                filter.Page,
                filter.PageSize,
                filter.SortBy,
                filter.SortOrder);

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
                throw new HospitalRegistrationException("Trạng thái mới phải khác với trạng thái hiện tại");
            }

            // Validate required fields based on status
            if (newStatus == RegistrationStatus.CANCELLED && string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new HospitalRegistrationException("Lý do từ chối là bắt buộc khi hủy đơn đăng ký");
            }

            if (newStatus == RegistrationStatus.CONFIRMED && !request.HospitalId.HasValue)
            {
                throw new HospitalRegistrationException("Hospital ID là bắt buộc khi xác nhận đơn đăng ký");
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
                HospitalName = updated.HospitalName,
                Email = updated.Email,
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
                throw new HospitalRegistrationException("Đơn đăng ký đã được phê duyệt trước đó");
            }

            // Check if cancelled
            if (registration.Status == RegistrationStatus.CANCELLED)
            {
                throw new HospitalRegistrationException("Không thể phê duyệt đơn đăng ký đã bị từ chối");
            }

            // Upload contract file
            var contractFileResult = await UploadFileAsync(
                request.ContractFile,
                "hospital-registrations/contracts",
                "ContractFile");

            if (!contractFileResult.Success)
            {
                throw new HospitalRegistrationException($"Không thể tải lên file hợp đồng: {contractFileResult.ErrorMessage}");
            }

            var contractFileUrl = contractFileResult.UploadResult!.CloudFrontUrl ?? contractFileResult.UploadResult!.FileUrl;

            // Update registration status to CONFIRMED
            registration.Status = RegistrationStatus.CONFIRMED;
            registration.ContractFile = contractFileUrl;

            var updated = await _registrationRepository.UpdateAsync(registration);

            // Generate a strong password for the hospital account
            var generatedPassword = GenerateStrongPassword(16);

            // Publish event to trigger Hospital Account Creation Saga in Auth Service
            var accountCreationEvent = new HospitalAccountCreationRequestedEvent
            {
                RegistrationId = updated.Id,
                HospitalName = updated.HospitalName,
                Email = updated.Email,
                Phone = updated.Phone,
                Address = updated.Address,
                TaxCode = updated.TaxCode,
                ContractFileUrl = contractFileUrl!,
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
                throw new HospitalRegistrationException("Đơn đăng ký đã bị từ chối trước đó");
            }

            // Check if already confirmed
            if (registration.Status == RegistrationStatus.CONFIRMED)
            {
                throw new HospitalRegistrationException("Không thể từ chối đơn đăng ký đã được phê duyệt");
            }

            // Update registration status to CANCELLED with reason
            registration.Status = RegistrationStatus.CANCELLED;
            registration.Reason = request.Reason;

            var updated = await _registrationRepository.UpdateAsync(registration);

            // Publish event for email notification
            var statusUpdatedEvent = new HospitalRegistrationStatusUpdatedEvent
            {
                RegistrationId = updated.Id,
                HospitalName = updated.HospitalName,
                Email = updated.Email,
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
            HospitalName = entity.HospitalName,
            Email = entity.Email,
            Phone = entity.Phone,
            Address = entity.Address,
            LicenseFile = entity.LicenseFile,
            BusinessCertificateFile = entity.BusinessCertificateFile,
            IdentityCardFile = entity.IdentityCardFile,
            TaxCode = entity.TaxCode,
            Status = entity.Status,
            StatusText = GetStatusText(entity.Status),
            ContractFile = entity.ContractFile,
            HospitalId = entity.HospitalId,
            Reason = entity.Reason,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private string GetStatusText(RegistrationStatus status)
    {
        return status switch
        {
            RegistrationStatus.PENDING => "Đang chờ xử lý",
            RegistrationStatus.CONFIRMED => "Đã xác nhận",
            RegistrationStatus.CANCELLED => "Đã hủy",
            _ => "Không xác định"
        };
    }

    private async Task<(bool Success, FileUploadResult? UploadResult, string? ErrorMessage)> UploadFileAsync(
        IFormFile file,
        string folder,
        string entityType)
    {
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

    private async Task<byte[]> ConvertFormFileToByteArray(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private string GenerateStrongPassword(int length)
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string specialChars = "@#$%^&*()_+-=[]{}|;:,.<>?";
        const string allChars = upperCase + lowerCase + digits + specialChars;

        var random = new Random();
        var password = new char[length];

        // Ensure at least one character from each category
        password[0] = upperCase[random.Next(upperCase.Length)];
        password[1] = lowerCase[random.Next(lowerCase.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = specialChars[random.Next(specialChars.Length)];

        // Fill the rest with random characters
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Shuffle the password
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}

