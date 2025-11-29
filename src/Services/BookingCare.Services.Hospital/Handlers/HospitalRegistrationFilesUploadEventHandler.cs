using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Shared.FileUpload.Services;
using BookingCare.Shared.FileUpload.Wrappers;

namespace BookingCare.Services.Hospital.Handlers;

/// <summary>
/// Event handler to process file uploads asynchronously for hospital registration
/// </summary>
public class HospitalRegistrationFilesUploadEventHandler : IIntegrationEventHandler<HospitalRegistrationFilesUploadEvent>
{
    private readonly ILogger<HospitalRegistrationFilesUploadEventHandler> _logger;
    private readonly IHospitalRegistrationRepository _registrationRepository;
    private readonly FileUploadOrchestrator _uploadOrchestrator;

    public HospitalRegistrationFilesUploadEventHandler(
        ILogger<HospitalRegistrationFilesUploadEventHandler> logger,
        IHospitalRegistrationRepository registrationRepository,
        FileUploadOrchestrator uploadOrchestrator)
    {
        _logger = logger;
        _registrationRepository = registrationRepository;
        _uploadOrchestrator = uploadOrchestrator;
    }

    public async Task HandleAsync(HospitalRegistrationFilesUploadEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[HospitalRegistrationFilesUploadEventHandler] Processing file uploads for registration: {RegistrationId}",
                @event.RegistrationId
            );

            // Get registration entity
            var registration = await _registrationRepository.GetByIdAsync(@event.RegistrationId);
            if (registration == null)
            {
                _logger.LogError(
                    "[HospitalRegistrationFilesUploadEventHandler] Registration not found: {RegistrationId}",
                    @event.RegistrationId
                );
                return;
            }

            // Upload License File
            var licenseFileUrl = await UploadFileFromEventData(
                @event.LicenseFile,
                @event.RegistrationId,
                cancellationToken
            );
            if (!string.IsNullOrEmpty(licenseFileUrl))
            {
                registration.LicenseFile = licenseFileUrl;
                _logger.LogInformation("[HospitalRegistrationFilesUploadEventHandler] License file uploaded successfully: {Url}", licenseFileUrl);
            }
            else
            {
                registration.LicenseFile = "UPLOAD_FAILED";
                _logger.LogError("[HospitalRegistrationFilesUploadEventHandler] License file upload failed");
            }

            // Upload Business Certificate File
            var businessCertFileUrl = await UploadFileFromEventData(
                @event.BusinessCertificateFile,
                @event.RegistrationId,
                cancellationToken
            );
            if (!string.IsNullOrEmpty(businessCertFileUrl))
            {
                registration.BusinessCertificateFile = businessCertFileUrl;
                _logger.LogInformation("[HospitalRegistrationFilesUploadEventHandler] Business certificate file uploaded successfully: {Url}", businessCertFileUrl);
            }
            else
            {
                registration.BusinessCertificateFile = "UPLOAD_FAILED";
                _logger.LogError("[HospitalRegistrationFilesUploadEventHandler] Business certificate file upload failed");
            }

            // Upload Identity Card File
            var identityCardFileUrl = await UploadFileFromEventData(
                @event.IdentityCardFile,
                @event.RegistrationId,
                cancellationToken
            );
            if (!string.IsNullOrEmpty(identityCardFileUrl))
            {
                registration.IdentityCardFile = identityCardFileUrl;
                _logger.LogInformation("[HospitalRegistrationFilesUploadEventHandler] Identity card file uploaded successfully: {Url}", identityCardFileUrl);
            }
            else
            {
                registration.IdentityCardFile = "UPLOAD_FAILED";
                _logger.LogError("[HospitalRegistrationFilesUploadEventHandler] Identity card file upload failed");
            }

            // Always update registration with file URLs (success or failed status)
            await _registrationRepository.UpdateAsync(registration);

            _logger.LogInformation(
                "[HospitalRegistrationFilesUploadEventHandler] Updated registration {RegistrationId} with upload results. License: {LicenseStatus}, BusinessCert: {BusinessCertStatus}, IdentityCard: {IdentityCardStatus}",
                @event.RegistrationId,
                !string.IsNullOrEmpty(licenseFileUrl) ? "SUCCESS" : "FAILED",
                !string.IsNullOrEmpty(businessCertFileUrl) ? "SUCCESS" : "FAILED",
                !string.IsNullOrEmpty(identityCardFileUrl) ? "SUCCESS" : "FAILED"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalRegistrationFilesUploadEventHandler] Error processing file uploads for registration: {RegistrationId}",
                @event.RegistrationId
            );
        }
    }

    private async Task<string?> UploadFileFromEventData(
        FileUploadData fileData,
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create MemoryStream from byte array
            var memoryStream = new MemoryStream(fileData.FileData);

            // Create IFormFile-like object (owns the stream and will dispose it)
            using var formFile = new FormFileWrapper(memoryStream, fileData.FileName, fileData.ContentType);

            var config = new FileUploadConfig
            {
                AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", ".gif", ".webp" },
                MaxSizeInMB = 10,
                Folder = fileData.Folder,
                SuccessMessage = $"{fileData.EntityType} uploaded successfully",
                EntityType = fileData.EntityType
            };

            var uploadResult = await _uploadOrchestrator.UploadFileAsync(
                formFile,
                config,
                registrationId,
                _logger,
                cancellationToken
            );

            if (uploadResult.Success && uploadResult.UploadResult != null)
            {
                return uploadResult.UploadResult.CloudFrontUrl ?? uploadResult.UploadResult.FileUrl;
            }

            _logger.LogError(
                "[HospitalRegistrationFilesUploadEventHandler] Failed to upload {EntityType}: {ErrorMessage}",
                fileData.EntityType,
                uploadResult.ErrorMessage
            );

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[HospitalRegistrationFilesUploadEventHandler] Exception uploading {EntityType}",
                fileData.EntityType
            );
            return null;
        }
    }
}

