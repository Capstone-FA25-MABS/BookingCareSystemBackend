using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Enums;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Exceptions;
using BookingCare.Shared.FileUpload.Services;
using BookingCare.Shared.FileUpload.Wrappers;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Protos;
using System.Security.Cryptography;
using BookingCare.Shared.Cache.Constants;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Services.Implementations;

/// <summary>
/// Service for contract signing operations
/// </summary>
public class ContractSigningService : BaseService, IContractSigningService
{
    private readonly IContractSigningTokenRepository _tokenRepository;
    private readonly IHospitalRegistrationRepository _registrationRepository;
    private readonly IContractGenerationService _contractGenerationService;
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly IEventBus _eventBus;
    private readonly OtpVerifier.OtpVerifierClient _otpGrpcClient;
    private readonly IConfiguration _configuration;

    public ContractSigningService(
        IContractSigningTokenRepository tokenRepository,
        IHospitalRegistrationRepository registrationRepository,
        IContractGenerationService contractGenerationService,
        FileUploadOrchestrator uploadOrchestrator,
        IEventBus eventBus,
        OtpVerifier.OtpVerifierClient otpGrpcClient,
        IConfiguration configuration,
        ILogger<ContractSigningService> logger) : base(logger)
    {
        _tokenRepository = tokenRepository;
        _registrationRepository = registrationRepository;
        _contractGenerationService = contractGenerationService;
        _uploadOrchestrator = uploadOrchestrator;
        _eventBus = eventBus;
        _otpGrpcClient = otpGrpcClient;
        _configuration = configuration;
    }

    public async Task<string> GenerateSigningTokenAsync(Guid registrationId, int expiryDays = 7)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Generating signing token for registration: {RegistrationId}", null, registrationId);

            // Verify registration exists and has contract draft
            var registration = await _registrationRepository.GetByIdAsync(registrationId);
            if (registration == null)
            {
                throw new NotFoundException($"Registration with ID {registrationId} not found", "REGISTRATION_NOT_FOUND");
            }

            if (registration.Status != RegistrationStatus.CONTRACT_GENERATED)
            {
                throw new InvalidRegistrationStatusException(
                    "Contract must be generated before creating signing token");
            }

            if (string.IsNullOrEmpty(registration.ContractDraftFile))
            {
                throw new InvalidOperationException(
                    "Contract draft file not found");
            }

            // Invalidate any existing tokens
            await _tokenRepository.InvalidateTokensByRegistrationIdAsync(registrationId);

            // Generate secure token
            var token = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddDays(expiryDays);

            // Create token entity
            var tokenEntity = new ContractSigningTokenEntity
            {
                RegistrationId = registrationId,
                Token = token,
                ExpiresAt = expiresAt,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _tokenRepository.CreateAsync(tokenEntity);

            LogInfo("Successfully generated signing token for registration: {RegistrationId}", null, registrationId);

            return token;
        }, "GenerateSigningToken");
    }

    public async Task<ValidateTokenResponseDto> ValidateTokenAsync(string token)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var tokenEntity = await _tokenRepository.GetByTokenAsync(token);

            if (tokenEntity == null)
            {
                return new ValidateTokenResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Token không hợp lệ"
                };
            }

            // Check if token is expired
            if (tokenEntity.ExpiresAt < DateTime.UtcNow)
            {
                return new ValidateTokenResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Token đã hết hạn. Vui lòng liên hệ admin để nhận link mới."
                };
            }

            // Check if token is already used
            if (tokenEntity.IsUsed)
            {
                return new ValidateTokenResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Hợp đồng đã được ký. Mỗi link chỉ có thể sử dụng một lần."
                };
            }

            // Get registration info
            var registration = tokenEntity.Registration;
            if (registration == null)
            {
                return new ValidateTokenResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Không tìm thấy thông tin đăng ký"
                };
            }

            // Generate contract number for display
            var contractNumber = $"HĐ-BC-{registration.CreatedAt:yyyyMMdd}-{registration.Id.ToString("N").Substring(0, 8).ToUpper()}";

            return new ValidateTokenResponseDto
            {
                IsValid = true,
                ContractInfo = new ContractSigningInfoDto
                {
                    RegistrationId = registration.Id,
                    HospitalName = registration.HospitalName,
                    RepresentativeName = registration.RepresentativeName,
                    RepresentativeEmail = registration.RepresentativeEmail,
                    ContractDraftUrl = registration.ContractDraftFile ?? string.Empty,
                    ContractNumber = contractNumber,
                    ExpiresAt = tokenEntity.ExpiresAt
                }
            };
        }, "ValidateToken");
    }

    public async Task<bool> SendSigningOtpAsync(string token)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Sending OTP for contract signing", null);

            // Validate token first
            var validation = await ValidateTokenAsync(token);
            if (!validation.IsValid || validation.ContractInfo == null)
            {
                throw new InvalidOperationException(validation.ErrorMessage ?? "Invalid token");
            }

            // Send OTP via gRPC
            var otpRequest = new SendOtpRequest
            {
                Email = validation.ContractInfo.RepresentativeEmail,
                Phone = string.Empty,
                Purpose = "CONTRACT_SIGNING"
            };

            var otpResponse = await _otpGrpcClient.SendOtpAsync(otpRequest);

            if (!otpResponse.Success)
            {
                LogWarning("Failed to send OTP: {Message}", null, otpResponse.Message ?? "Unknown error");
                throw new InvalidOperationException(otpResponse.Message ?? "Failed to send OTP");
            }

            LogInfo("Successfully sent OTP to: {Email}", null, validation.ContractInfo.RepresentativeEmail);

            return true;
        }, "SendSigningOtp");
    }

    public async Task<SignContractResponseDto> SignContractAsync(SignContractRequestDto request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Processing contract signing", null);

            // Validate token
            var validation = await ValidateTokenAsync(request.Token);
            if (!validation.IsValid || validation.ContractInfo == null)
            {
                return new SignContractResponseDto
                {
                    Success = false,
                    Message = validation.ErrorMessage ?? "Token không hợp lệ"
                };
            }

            // Verify OTP via gRPC
            if (string.IsNullOrEmpty(request.OtpCode))
            {
                return new SignContractResponseDto
                {
                    Success = false,
                    Message = "Vui lòng nhập mã OTP"
                };
            }

            // Build the same key format as when sending OTP
            // Format: BookingCare:Notification:otp:purpose:contract-signing:email:{email}
            var otpKey = CacheKeys.Format(CacheKeys.OtpPurposeEmail, OtpPurpose.CONTRACT_SIGNING.ToKey(), validation.ContractInfo.RepresentativeEmail).ToLowerInvariant();

            var otpVerifyRequest = new VerifyRequest
            {
                Key = otpKey,
                Otp = request.OtpCode
            };

            try
            {
                var otpVerifyResponse = await _otpGrpcClient.VerifyAsync(otpVerifyRequest);
                if (!otpVerifyResponse.Verified)
                {
                    return new SignContractResponseDto
                    {
                        Success = false,
                        Message = "Mã OTP không chính xác hoặc đã hết hạn"
                    };
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Error verifying OTP for contract signing", null);
                return new SignContractResponseDto
                {
                    Success = false,
                    Message = "Không thể xác thực mã OTP. Vui lòng thử lại."
                };
            }

            // Get token entity
            var tokenEntity = await _tokenRepository.GetByTokenAsync(request.Token);
            if (tokenEntity == null)
            {
                return new SignContractResponseDto
                {
                    Success = false,
                    Message = "Token không hợp lệ"
                };
            }

            // Get registration
            var registration = await _registrationRepository.GetByIdAsync(validation.ContractInfo.RegistrationId);
            if (registration == null)
            {
                return new SignContractResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy thông tin đăng ký"
                };
            }

            // Upload signature image
            var signatureUrl = await UploadSignatureAsync(request.SignatureBase64, registration.Id);

            // Update contract with hospital signature
            var signedContractUrl = await _contractGenerationService.AddHospitalSignatureToContractAsync(
                registration.Id,
                signatureUrl,
                DateTime.UtcNow);

            // Update registration
            registration.HospitalSignature = signatureUrl;
            registration.SignedAt = DateTime.UtcNow;
            registration.Status = RegistrationStatus.CONTRACT_SIGNED;
            registration.ContractFile = signedContractUrl; // Save final signed contract
            registration.UpdatedAt = DateTime.UtcNow;

            await _registrationRepository.UpdateAsync(registration);

            // Mark token as used
            await _tokenRepository.MarkAsUsedAsync(
                tokenEntity.Id,
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown");

            // Publish event for notification
            await PublishContractSignedEventAsync(registration);

            LogInfo("Successfully signed contract for registration: {RegistrationId}", null, registration.Id);

            return new SignContractResponseDto
            {
                Success = true,
                Message = "Ký hợp đồng thành công! Admin sẽ xem xét và phê duyệt trong thời gian sớm nhất.",
                SignedContractUrl = signedContractUrl,
                SignedAt = registration.SignedAt
            };
        }, "SignContract");
    }

    public async Task<string> GetSigningLinkAsync(Guid registrationId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Check if there's an active token
            var existingToken = await _tokenRepository.GetActiveTokenByRegistrationIdAsync(registrationId);

            string token;
            if (existingToken != null)
            {
                token = existingToken.Token;
            }
            else
            {
                // Generate new token
                token = await GenerateSigningTokenAsync(registrationId);
            }

            // Build full URL
            var baseUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
            return $"{baseUrl}/contract-signing/{token}";
        }, "GetSigningLink");
    }

    #region Private Helper Methods

    private string GenerateSecureToken()
    {
        // Generate a cryptographically secure random token
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private async Task<string> UploadSignatureAsync(string signatureBase64, Guid registrationId)
    {
        try
        {
            // Remove data URL prefix if present
            var base64Data = signatureBase64;
            if (signatureBase64.Contains(","))
            {
                base64Data = signatureBase64.Split(',')[1];
            }

            // Convert base64 to bytes
            var signatureBytes = Convert.FromBase64String(base64Data);

            // Create file name
            var fileName = $"hospital-signature-{registrationId}-{DateTime.Now:yyyyMMddHHmmss}.png";

            // Create MemoryStream and FormFile wrapper
            var memoryStream = new MemoryStream(signatureBytes);
            var formFile = new FormFileWrapper(memoryStream, fileName, "image/png");

            var config = new FileUploadConfig
            {
                Folder = "contracts/signatures",
                AllowedExtensions = new[] { ".png", ".jpg", ".jpeg" },
                MaxSizeInMB = 2, // 2MB max for signature images
                EntityType = "HospitalSignature"
            };

            // Upload using FileUploadOrchestrator
            var uploadResult = await _uploadOrchestrator.UploadFileAsync(
                formFile,
                config,
                Guid.Empty, // No specific account ID for hospital signatures
                Logger,
                CancellationToken.None
            );

            // Dispose resources
            formFile.Dispose();

            if (!uploadResult.Success || uploadResult.UploadResult == null)
            {
                throw new FileUploadException($"Failed to upload signature: {uploadResult.ErrorMessage}");
            }

            var fileUrl = uploadResult.UploadResult.CloudFrontUrl ?? uploadResult.UploadResult.FileUrl;
            LogInfo("Successfully uploaded hospital signature to: {Url}", null, fileUrl ?? "Unknown URL");

            return fileUrl ?? throw new FileUploadException("Upload result returned null file URL");
        }
        catch (Exception ex)
        {
            LogError(ex, "Error uploading hospital signature", null);
            throw new FileUploadException("Failed to upload signature", ex);
        }
    }

    private async Task PublishContractSignedEventAsync(HospitalRegistrationEntity registration)
    {
        try
        {
            var @event = new HospitalContractSignedEvent
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
                SignedAt = registration.SignedAt ?? DateTime.UtcNow,
                SignedContractUrl = registration.ContractFile ?? string.Empty
            };

            await _eventBus.PublishAsync(@event);

            LogInfo("Published HospitalContractSignedEvent for registration: {RegistrationId}", null, registration.Id);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error publishing contract signed event for registration: {RegistrationId}", null, registration.Id);
            // Don't throw - event publishing failure shouldn't break the signing process
        }
    }

    #endregion
}
