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
using System.Security.Cryptography;

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
                    FileName = SanitizeFileName(request.LicenseFile.FileName),
                    ContentType = request.LicenseFile.ContentType,
                    FileData = licenseFileData,
                    Folder = "hospital-registrations/license-files",
                    EntityType = "LicenseFile"
                },
                BusinessCertificateFile = new FileUploadData
                {
                    FileName = SanitizeFileName(request.BusinessCertificateFile.FileName),
                    ContentType = request.BusinessCertificateFile.ContentType,
                    FileData = businessCertFileData,
                    Folder = "hospital-registrations/business-certificates",
                    EntityType = "BusinessCertificateFile"
                },
                IdentityCardFile = new FileUploadData
                {
                    FileName = SanitizeFileName(request.IdentityCardFile.FileName),
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

            // Upload contract file
            var contractFileResult = await UploadFileAsync(
                request.ContractFile,
                "hospital-registrations/contracts",
                "ContractFile");

            if (!contractFileResult.Success)
            {
                throw new FileUploadException($"Không thể tải lên file hợp đồng: {contractFileResult.ErrorMessage}");
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
                RepresentativeName = updated.RepresentativeName,
                RepresentativeEmail = updated.RepresentativeEmail,
                RepresentativePhone = updated.RepresentativePhone,
                HospitalName = updated.HospitalName,
                HospitalEmail = updated.HospitalEmail,
                HospitalPhone = updated.HospitalPhone,
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
            ContractFile = entity.ContractFile,
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
        // Sanitize file name to prevent non-ASCII character issues
        var sanitizedFileName = SanitizeFileName(file.FileName);

        // Create a wrapper with sanitized file name
        var sanitizedFile = new SanitizedFormFileWrapper(file, sanitizedFileName);

        var config = new FileUploadConfig
        {
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" },
            MaxSizeInMB = 10,
            Folder = folder,
            SuccessMessage = $"{entityType} uploaded successfully",
            EntityType = entityType
        };

        var uploadResult = await _uploadOrchestrator.UploadFileAsync(
            sanitizedFile,
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

    /// <summary>
    /// Sanitizes file name to contain only ASCII characters
    /// Removes diacritics and replaces non-ASCII characters with underscores
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return $"file_{Guid.NewGuid():N}";
        }

        // Get file extension
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Remove diacritics (Vietnamese accents)
        var normalizedString = nameWithoutExtension.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                // Keep only ASCII characters (letters, digits, dash, underscore)
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.')
                {
                    stringBuilder.Append(c);
                }
                else if (c == ' ')
                {
                    stringBuilder.Append('_');
                }
            }
        }

        var sanitizedName = stringBuilder.ToString();

        // If sanitization resulted in empty string, generate a unique name
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            sanitizedName = $"file_{Guid.NewGuid():N}";
        }

        return sanitizedName + extension;
    }
}

/// <summary>
/// Wrapper class to provide a sanitized file name for IFormFile
/// This prevents issues with non-ASCII characters in HTTP headers
/// </summary>
internal sealed class SanitizedFormFileWrapper : IFormFile
{
    private readonly IFormFile _originalFile;
    private readonly string _sanitizedFileName;

    public SanitizedFormFileWrapper(IFormFile originalFile, string sanitizedFileName)
    {
        _originalFile = originalFile ?? throw new ArgumentNullException(nameof(originalFile));
        _sanitizedFileName = sanitizedFileName ?? throw new ArgumentNullException(nameof(sanitizedFileName));
    }

    public string ContentType => _originalFile.ContentType;
    public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_sanitizedFileName}\"";
    public IHeaderDictionary Headers => _originalFile.Headers;
    public long Length => _originalFile.Length;
    public string Name => _originalFile.Name;
    public string FileName => _sanitizedFileName;

    public void CopyTo(Stream target) => _originalFile.CopyTo(target);
    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        => _originalFile.CopyToAsync(target, cancellationToken);
    public Stream OpenReadStream() => _originalFile.OpenReadStream();
}
