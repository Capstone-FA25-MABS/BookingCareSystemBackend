using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Shared.FileUpload.Services;

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

/// <summary>
/// Wrapper class to convert Stream to IFormFile for FileUploadOrchestrator
/// Owns the stream and will dispose it when this wrapper is disposed
/// </summary>
internal sealed class FormFileWrapper : IFormFile, IDisposable
{
    private readonly Stream _stream;
    private readonly string _fileName;
    private readonly string _contentType;
    private bool _disposed;

    public FormFileWrapper(Stream stream, string fileName, string contentType)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _contentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
    }

    public string ContentType => _contentType;
    public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";
    public IHeaderDictionary Headers => new HeaderDictionary();
    public long Length => _stream.Length;
    public string Name => "file";
    public string FileName => _fileName;

    public void CopyTo(Stream target)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _stream.Position = 0; // Reset position before copying
        _stream.CopyTo(target);
        _stream.Position = 0; // Reset for potential reuse
    }

    public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _stream.Position = 0; // Reset position before copying
        await _stream.CopyToAsync(target, cancellationToken);
        _stream.Position = 0; // Reset for potential reuse
    }

    public Stream OpenReadStream()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _stream.Position = 0; // Reset position
        return _stream;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }
}

