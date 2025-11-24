using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Shared.FileUpload.Services;
using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Exceptions;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Implementations;

/// <summary>
/// Service implementation for admin signature management
/// </summary>
public class AdminSignatureService : BaseService, IAdminSignatureService
{
    private readonly IAdminSignatureRepository _signatureRepository;
    private readonly FileUploadOrchestrator _uploadOrchestrator;

    public AdminSignatureService(
        IAdminSignatureRepository signatureRepository,
        FileUploadOrchestrator uploadOrchestrator,
        ILogger<AdminSignatureService> logger) : base(logger)
    {
        _signatureRepository = signatureRepository;
        _uploadOrchestrator = uploadOrchestrator;
    }

    public async Task<AdminSignatureResponseDto?> GetByIdAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var signature = await _signatureRepository.GetByIdAsync(id);
            return signature != null ? MapToResponseDto(signature) : null;
        }, "GetAdminSignatureById");
    }

    public async Task<AdminSignatureResponseDto?> GetActiveSignatureAsync(string adminId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var signature = await _signatureRepository.GetActiveByAdminIdAsync(adminId);
            return signature != null ? MapToResponseDto(signature) : null;
        }, "GetActiveAdminSignature");
    }

    public async Task<IEnumerable<AdminSignatureResponseDto>> GetAllAsync()
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var signatures = await _signatureRepository.GetAllAsync();
            return signatures.Select(MapToResponseDto);
        }, "GetAllAdminSignatures");
    }

    public async Task<AdminSignatureResponseDto> CreateAsync(string adminId, CreateAdminSignatureRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating admin signature for admin: {AdminId}", null, adminId);

            // Validate file
            ValidateSignatureFile(request.SignatureFile);

            // Upload signature file
            var uploadResult = await UploadSignatureFileAsync(request.SignatureFile, adminId);

            if (!uploadResult.Success || uploadResult.UploadResult == null)
            {
                throw new FileUploadException(
                    uploadResult.ErrorMessage ?? "Failed to upload signature file");
            }

            // Deactivate all existing signatures for this admin
            await _signatureRepository.DeactivateAllByAdminIdAsync(adminId);

            // Create new signature entity
            var signature = new AdminSignatureEntity
            {
                AdminId = adminId,
                FullName = request.FullName,
                Position = request.Position,
                SignatureImageUrl = uploadResult.UploadResult?.CloudFrontUrl ?? uploadResult.UploadResult?.FileUrl ?? string.Empty,
                IsActive = true
            };

            var createdSignature = await _signatureRepository.CreateAsync(signature);

            LogInfo("Successfully created admin signature: {SignatureId}", null, createdSignature.Id);

            return MapToResponseDto(createdSignature);
        }, "CreateAdminSignature");
    }

    public async Task<AdminSignatureResponseDto> UpdateAsync(Guid id, string adminId, UpdateAdminSignatureRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating admin signature: {SignatureId}", null, id);

            var signature = await _signatureRepository.GetByIdAsync(id);
            if (signature == null)
            {
                throw new NotFoundException($"Admin signature with ID {id} not found", "SIGNATURE_NOT_FOUND");
            }

            // Verify ownership
            if (signature.AdminId != adminId)
            {
                throw new UnauthorizedException("You are not authorized to update this signature", "UNAUTHORIZED_SIGNATURE_UPDATE");
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                signature.FullName = request.FullName;
            }

            if (!string.IsNullOrWhiteSpace(request.Position))
            {
                signature.Position = request.Position;
            }

            if (request.IsActive.HasValue)
            {
                signature.IsActive = request.IsActive.Value;
            }

            // Upload new signature file if provided
            if (request.SignatureFile != null)
            {
                ValidateSignatureFile(request.SignatureFile);

                var uploadResult = await UploadSignatureFileAsync(request.SignatureFile, adminId);

                if (!uploadResult.Success || uploadResult.UploadResult == null)
                {
                    throw new FileUploadException(
                        uploadResult.ErrorMessage ?? "Failed to upload signature file");
                }

                signature.SignatureImageUrl = uploadResult.UploadResult?.CloudFrontUrl ?? uploadResult.UploadResult?.FileUrl ?? string.Empty;
            }

            var updatedSignature = await _signatureRepository.UpdateAsync(signature);

            LogInfo("Successfully updated admin signature: {SignatureId}", null, id);

            return MapToResponseDto(updatedSignature);
        }, "UpdateAdminSignature");
    }

    public async Task<bool> DeleteAsync(Guid id, string adminId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Deleting admin signature: {SignatureId}", null, id);

            var signature = await _signatureRepository.GetByIdAsync(id);
            if (signature == null)
            {
                throw new NotFoundException($"Admin signature with ID {id} not found", "SIGNATURE_NOT_FOUND");
            }

            // Verify ownership
            if (signature.AdminId != adminId)
            {
                throw new UnauthorizedException("You are not authorized to delete this signature", "UNAUTHORIZED_SIGNATURE_DELETE");
            }

            var result = await _signatureRepository.DeleteAsync(id);

            LogInfo("Successfully deleted admin signature: {SignatureId}", null, id);

            return result;
        }, "DeleteAdminSignature");
    }

    #region Private Helper Methods

    private void ValidateSignatureFile(IFormFile file)
    {
        // Validate file size (max 5MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            throw new FileUploadException("Signature file size must not exceed 5MB");
        }

        // Validate file type (only images)
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new FileUploadException(
                "Invalid file type. Only PNG, JPG, JPEG, GIF, and SVG files are allowed");
        }
    }

    private async Task<(bool Success, FileUploadResult? UploadResult, string? ErrorMessage)> UploadSignatureFileAsync(
        IFormFile file,
        string adminId)
    {
        try
        {
            // Upload configuration
            // Note: FileUploadOrchestrator automatically sanitizes filenames to prevent ASCII issues
            var config = new FileUploadConfig
            {
                Folder = "admin-signatures",
                AllowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg" },
                MaxSizeInMB = 5, // 5MB max for signature images
                EntityType = "AdminSignature"
            };

            var uploadResult = await _uploadOrchestrator.UploadFileAsync(
                file,
                config,
                Guid.Empty,
                Logger,
                CancellationToken.None);

            if (uploadResult.Success && uploadResult.UploadResult?.FileUrl != null)
            {
                return (true, uploadResult.UploadResult, null);
            }

            return (false, null, uploadResult.ErrorMessage ?? "Unknown upload error");
        }
        catch (Exception ex)
        {
            LogError(ex, "Error uploading signature file for admin: {AdminId}", null, adminId);
            return (false, null, ex.Message);
        }
    }

    private static AdminSignatureResponseDto MapToResponseDto(AdminSignatureEntity entity)
    {
        return new AdminSignatureResponseDto
        {
            Id = entity.Id,
            AdminId = entity.AdminId,
            FullName = entity.FullName,
            Position = entity.Position,
            SignatureImageUrl = entity.SignatureImageUrl,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    #endregion
}
