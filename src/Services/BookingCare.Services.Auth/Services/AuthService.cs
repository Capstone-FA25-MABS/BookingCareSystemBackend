using AutoMapper;
using BookingCare.Services.Auth.Constants;
using BookingCare.Services.Auth.Exceptions;
using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Models.Entities;
using BookingCare.Services.Auth.Repositories;
using BookingCare.Shared.Cache.Constants;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Exceptions;
using BookingCare.Shared.Common.Enums;
using System.Security.Cryptography;
using System.Text;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Protos;
using BookingCare.Services.Auth.Utils;
using BookingCare.Services.Auth.Providers;
using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Models;
using BookingCare.Shared.Saga.SagaDefinition;

namespace BookingCare.Services.Auth.Services;

/// <summary>
/// Service implementation for Auth service operations
/// </summary>
public class AuthService : BaseService, IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly JwtService _jwtService;
    private readonly CookieService _cookieService;
    private readonly IEventBus _eventBus;
    private readonly OtpVerifier.OtpVerifierClient _otpClient;
    private readonly ExternalAuthProviderService _externalAuthProviderService;
    private readonly ISagaManager _sagaManager;

    public AuthService(
        IAuthRepository authRepository,
        IMapper mapper,
        ILogger<AuthService> logger,
        IConfiguration configuration,
        JwtService jwtService,
        CookieService cookieService,
        IEventBus eventBus,
        ExternalAuthProviderService externalAuthProviderService,
        ISagaManager sagaManager,
        OtpVerifier.OtpVerifierClient? otpClient = null) : base(logger)
    {
        _authRepository = authRepository;
        _mapper = mapper;
        _configuration = configuration;
        _jwtService = jwtService;
        _cookieService = cookieService;
        _eventBus = eventBus;
        _externalAuthProviderService = externalAuthProviderService;
        _sagaManager = sagaManager;
        _otpClient = otpClient!;
    }

    #region Authentication Operations

    /// <summary>
    /// Authenticate account and generate JWT token with refresh token
    /// </summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Determine login identifier for logging
            var loginIdentifier = !string.IsNullOrEmpty(request.Email) ? request.Email : request.PhoneNumber;
            LogInfo("Login attempt for: {LoginIdentifier}", null, loginIdentifier!);

            // Find account by email or phone number
            var account = await FindAccountByEmailOrPhoneAsync(request.Email, request.PhoneNumber);
            if (account == null)
            {
                throw new AuthenticationException($"Invalid email, phone number, or password");
            }

            // Check account status first
            if (account.Status != Status.ACTIVE)
            {
                throw new AuthenticationException($"Account '{loginIdentifier}' is not active");
            }

            // Check if account is locked out (for specific error message)
            if (await _authRepository.IsAccountLockedOutAsync(account))
            {
                throw new AuthenticationException($"Account '{loginIdentifier}' is temporarily locked due to too many failed login attempts");
            }

            var isValid = await _authRepository.ValidateCredentialsWithLockoutAsync(account, request.Password);
            if (!isValid)
            {
                throw new AuthenticationException("Invalid email, phone number, or password");
            }

            LogInfo("Login successful for: {LoginIdentifier}", null, loginIdentifier!);

            return await GenerateAuthResponseAsync(account, "Login successful");

        }, "Login");
    }

    /// <summary>
    /// Register new account
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, Role role)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            await CreateAccountForSagaAsync(request, role);

            return new AuthResponse
            {
                Message = "Registration successful",
            };
        }, "Register");
    }

    /// <summary>
    /// Create account for Saga step - contains the core registration logic
    /// </summary>
    public async Task<(bool Success, string AccountId, string Message)> CreateAccountForSagaAsync(RegisterRequest request, Role role)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Registration attempt for email: {Email}", null, request.Email);

            // Validate account uniqueness
            await ValidateAccountUniquenessAsync(request);

            // Validate OTP for patient registration
            await ValidatePatientOtpAsync(request, role);

            // Get and validate role
            var targetRole = await GetAndValidateRoleAsync(role);

            // Create and assign account
            var createdAccount = await CreateAndAssignAccountAsync(request, targetRole);

            LogInfo("Registration successful for email: {Email}", null, request.Email);

            return (true, createdAccount.Id.ToString(), "Account created successfully");
        }, "CreateAccountForSaga");
    }

    /// <summary>
    /// Validate account uniqueness (email and phone)
    /// </summary>
    private async Task ValidateAccountUniquenessAsync(RegisterRequest request)
    {
        if (await _authRepository.EmailExistsAsync(request.Email))
        {
            throw new AccountConflictException(request.Email, "Email");
        }

        if (await _authRepository.PhoneNumberExistsAsync(request.PhoneNumber))
        {
            throw new AccountConflictException(request.PhoneNumber, "PhoneNumber");
        }
    }

    /// <summary>
    /// Validate OTP for patient registration
    /// </summary>
    private async Task ValidatePatientOtpAsync(RegisterRequest request, Role role)
    {
        if (role == Role.PATIENT)
        {
            var purpose = request.Purpose.ToKey();
            var channel = string.IsNullOrWhiteSpace(request.Channel) ? AuthConstants.CHANNEL_PHONE : request.Channel.ToLowerInvariant();
            var subject = channel == AuthConstants.CHANNEL_EMAIL ? $"{AuthConstants.CHANNEL_EMAIL}:{request.Email}" : $"{AuthConstants.CHANNEL_PHONE}:{request.PhoneNumber}";
            var verified = await VerifyOtpOrProofAsync(purpose, subject, request.Proof, request.IssuedAt);
            if (!verified) throw new ValidationException("OTP verification required before registration");
        }
    }

    /// <summary>
    /// Get and validate role exists
    /// </summary>
    private async Task<RoleEntity> GetAndValidateRoleAsync(Role role)
    {
        var targetRoleName = role switch { Role.DOCTOR => "Doctor", Role.CLINIC => "Clinic", _ => "Patient" };
        var targetRole = await _authRepository.GetRoleByNameAsync(targetRoleName);
        if (targetRole == null)
        {
            throw new ValidationException($"Role '{targetRoleName}' does not exist in the system. Cannot create account without valid role.");
        }
        return targetRole;
    }

    /// <summary>
    /// Create account and assign role
    /// </summary>
    private async Task<AccountEntity> CreateAndAssignAccountAsync(RegisterRequest request, RoleEntity targetRole)
    {
        var account = _mapper.Map<AccountEntity>(request);
        if (!string.IsNullOrWhiteSpace(request.Channel))
        {
            var channel = request.Channel.ToLowerInvariant();
            if (channel == AuthConstants.CHANNEL_PHONE)
            {
                account.PhoneNumberConfirmed = true;
            }
            else if (channel == AuthConstants.CHANNEL_EMAIL)
            {
                account.EmailConfirmed = true;
            }
        }
        var createdAccount = await _authRepository.CreateAccountAsync(account, request.Password);

        await _authRepository.AssignRoleToAccountAsync(createdAccount, targetRole);
        LogInfo("Role '{Role}' assigned to account: {Email}", null, targetRole.Name!, request.Email);

        return createdAccount;
    }

    /// <summary>
    /// Delete account for Saga compensation
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAccountForSagaAsync(string accountId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Deleting account for compensation: {AccountId}", null, accountId);

            if (!Guid.TryParse(accountId, out var id))
            {
                return (false, "Invalid account ID format");
            }

            var account = await _authRepository.GetAccountByIdAsync(id);
            if (account == null)
            {
                return (true, "Account not found or already deleted"); // Consider success if already deleted
            }

            var result = await _authRepository.DeleteAccountAsync(account);
            if (result)
            {
                LogInfo("Account deleted successfully for compensation: {AccountId}", null, accountId);
                return (true, "Account deleted successfully");
            }
            else
            {
                return (false, "Failed to delete account");
            }
        }, "DeleteAccountForSaga");
    }

    /// <summary>
    /// Refresh JWT token using refresh token
    /// </summary>
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Refresh token attempt");

            // Validate refresh token
            var (isValid, account, tokenEntity) = await _authRepository.ValidateRefreshTokenAsync(refreshToken);

            if (!isValid || account == null)
            {
                throw new AuthenticationException("Invalid or expired refresh token");
            }

            // Generate new access token
            var newAccessToken = await _jwtService.GenerateAccessTokenAsync(account);

            // Save tokens in cookies (reuse same refresh token if still valid)
            _cookieService.SaveTokensInCookies(account.Id, newAccessToken, tokenEntity!.Token);

            LogInfo("Token refreshed successfully for account: {AccountId}", null, account.Id);

            return new AuthResponse
            {
                Message = "Token refreshed successfully",
            };
        }, "RefreshToken");
    }

    /// <summary>
    /// Logout account and revoke refresh token
    /// </summary>
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Logout attempt");

            await _authRepository.DeleteRefreshTokenAsync(refreshToken);

            // Clear authentication cookies
            _cookieService.ClearAuthenticationCookies();

            LogInfo("Logout successful");
            return true;
        }, "Logout");
    }

    /// <summary>
    /// Change account password
    /// </summary>
    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request, Guid accountId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Password change attempt for account: {AccountId}", null, accountId);

            // Get account
            var account = await _authRepository.GetAccountByIdAsync(accountId) ?? throw new AccountNotFoundException(accountId);

            // Check if account has external login providers
            var hasExternalLogin = await _authRepository.HasExternalLoginAsync(account);

            // Validate current password for regular accounts
            if (!hasExternalLogin)
            {
                if (string.IsNullOrEmpty(request.CurrentPassword))
                {
                    throw new ValidationException("Current password is required for regular accounts");
                }

                // Validate current password
                var isValidCurrentPassword = await _authRepository.ValidateCredentialsAsync(account, request.CurrentPassword);
                if (!isValidCurrentPassword)
                {
                    throw new ValidationException("Current password is incorrect");
                }
            }
            else
            {
                // For external login accounts, current password is not required
                LogInfo("Account has external login providers, skipping current password validation", null, accountId);
            }

            // Validate password complexity
            var passwordCheck = await _authRepository.ValidatePasswordAsync(account, request.NewPassword);
            if (!passwordCheck.IsValid)
            {
                var errors = string.Join(", ", passwordCheck.Errors);
                throw new ValidationException($"Password validation failed: {errors}");
            }

            // Change password
            var result = await _authRepository.ChangePasswordAsync(account, request.NewPassword);
            if (!result)
            {
                throw new AuthException("Failed to change password");
            }

            LogInfo("Password changed successfully for account: {AccountId}", null, accountId);
            return true;
        }, "ChangePassword");
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Get base URL for building reset URL
            var baseUrl = _cookieService.GetBaseUrl();

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                // Email flow: generate reset token and publish email event with reset link
                var (found, accountId, token) = await _authRepository.GeneratePasswordResetTokenAsync(request.Email);
                if (!found)
                {
                    LogWarning("Forgot Password requested for non-existent email: {Email}", request.Email);
                    // Do not reveal existence. Always return success.
                    return true;
                }

                var resetUrl = BuildResetUrl(baseUrl, request.Email, token);
                var message = $"Click the link to reset your password: {resetUrl}";

                var @event = new NotificationSendEvent
                {
                    UserId = accountId,
                    Title = "Password Reset",
                    Message = message,
                    Type = AuthConstants.CHANNEL_EMAIL,
                    Data = new Dictionary<string, object>
                    {
                        { AuthConstants.CHANNEL_EMAIL, request.Email! },
                        { "subject", "Reset your password" },
                        { "html", false },
                        { "purpose", OtpPurpose.FORGOT_PASSWORD.ToKey() },
                        { "resetUrl", resetUrl }
                    },
                    ScheduledAt = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(@event);
                LogInfo("Password reset email event published for {Email}", null, request.Email);
                return true;
            }
            else
            {
                // Phone flow: publish OTP request event to Notification service
                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    throw new ValidationException("PhoneNumber is required when Email is not provided");
                }

                // Check if account exists for this phone number before sending OTP
                var phoneAccount = await _authRepository.GetAccountByPhoneNumberAsync(request.PhoneNumber);
                if (phoneAccount == null)
                {
                    LogWarning("Forgot Password requested for non-existent phone: {Phone}", null, request.PhoneNumber);
                    // Do not reveal existence. Always return success.
                    return true;
                }

                var deviceId = request.DeviceId ?? string.Empty;
                var smsMessage = "OTP verification for password reset";
                var @event = new NotificationSendEvent
                {
                    UserId = phoneAccount.Id,
                    Title = "OTP Verification",
                    Message = smsMessage,
                    Type = "sms",
                    Data = new Dictionary<string, object>
                    {
                        { "phone", request.PhoneNumber! },
                        { "deviceId", deviceId },
                        { "purpose", OtpPurpose.FORGOT_PASSWORD.ToKey() }
                    },
                    ScheduledAt = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(@event);
                LogInfo("Password reset OTP SMS event published for {Phone}", null, request.PhoneNumber);

                // After OTP verification (handled by Notification service), client will receive a reset URL
                // via a separate channel. We intentionally return success here without revealing status.
                return true;
            }
        }, "ForgotPassword");
    }

    /// <summary>
    /// Reset token and URL after successful OTP verification (phone flow)
    /// </summary>
    public async Task<ResetTokenResponse> ResetTokenAsync(ResetTokenRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Unified verification (Redis flag or HMAC proof)
            var purpose = request.Purpose.ToKey();
            var subject = $"phone:{request.PhoneNumber}";
            var verified = await VerifyOtpOrProofAsync(purpose, subject, request.Proof, request.IssuedAt);
            if (!verified)
            {
                // Return neutral response without revealing status
                return new ResetTokenResponse { ResetUrl = string.Empty };
            }

            var (found, email, token) = await _authRepository.GeneratePasswordResetTokenByPhoneAsync(request.PhoneNumber);
            if (!found)
            {
                // Do not reveal
                return new ResetTokenResponse { ResetUrl = string.Empty };
            }

            var baseUrl = _cookieService.GetBaseUrl();

            var resetUrl = BuildResetUrl(baseUrl, email, token);
            return new ResetTokenResponse
            {
                ResetUrl = resetUrl
            };
        }, "ResetToken");
    }

    private bool VerifyOtpProof(string purpose, string? proof, string? issuedAt, IEnumerable<string> subjects)
    {
        var secret = _configuration.GetSection("OtpVerification").GetValue<string>("Secret") ?? string.Empty;
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(proof) || string.IsNullOrEmpty(issuedAt))
            return false;

        if (!long.TryParse(issuedAt, out long issuedAtTicks))
            return false;

        var age = Math.Abs((DateTimeOffset.UtcNow.Ticks - issuedAtTicks) / TimeSpan.TicksPerMinute);
        if (age > 5) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        foreach (var subject in subjects)
        {
            var data = $"{purpose}:{subject}:{issuedAt}";
            var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
            if (string.Equals(expected, proof, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private async Task<bool> VerifyOtpOrProofAsync(string purpose, string subject, string? proof, string? issuedAt)
    {
        // 1) Try gRPC if enabled
        var otpSection = _configuration.GetSection("OtpVerification");
        var useGrpc = otpSection.GetValue<bool>("UseGrpc");
        var allowProofFallback = otpSection.GetValue<bool>("AllowProofFallback", true);
        var timeoutMs = otpSection.GetValue<int>("TimeoutMs", 2000);

        // 2) Try Redis local flag (best-effort, ignore connectivity errors)
        var flagKey = CacheKeys.Format(CacheKeys.OtpVerified, purpose, subject).ToLowerInvariant();
        if (useGrpc && _otpClient != null)
        {
            bool verify = false;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
                var response = await _otpClient.VerifyAsync(new VerifyRequest
                {
                    Key = flagKey,
                    Otp = "1"
                }, cancellationToken: cts.Token);
                verify = response.Verified;
            }
            catch (Grpc.Core.RpcException ex)
            {
                LogWarning("gRPC OTP verify failed: {Message}", ex.Message);
            }
            catch (OperationCanceledException)
            {
                LogWarning("gRPC OTP verify timeout after {TimeoutMs}ms", null, timeoutMs);
            }
            catch (Exception ex)
            {
                LogWarning("gRPC OTP verify unexpected error: {Message}", ex.Message);
            }

            if (verify)
                return true;
            if (!allowProofFallback)
                return false;
        }

        // 3) Fallback to proof
        return VerifyOtpProof(purpose, proof, issuedAt, new[] { subject });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Normalize email (handle %40 etc.)
            var normalizedEmail = request.Email;
            try { normalizedEmail = Uri.UnescapeDataString(normalizedEmail); } catch { /* ignore */ }

            // Validate password complexity with Identity's password validator
            var account = await _authRepository.GetAccountByEmailAsync(normalizedEmail);
            if (account == null)
            {
                LogWarning("Reset password attempt for non-existent account with email: {Email}", request.Email);
                // Do not reveal account existence
                return true;
            }

            var complexity = await _authRepository.ValidatePasswordAsync(account, request.NewPassword);
            if (!complexity.IsValid)
            {
                var errors = string.Join(", ", complexity.Errors);
                throw new ValidationException($"Password validation failed: {errors}");
            }

            // Normalize reset token (handle URL encoding and '+' space issue)
            var normalizedToken = request.ResetToken.Replace(' ', '+');
            try { normalizedToken = Uri.UnescapeDataString(normalizedToken); } catch { /* keep original if invalid encoding */ }

            // Validate reset token via Identity and reset
            var success = await _authRepository.ResetPasswordWithTokenAsync(account, normalizedToken, request.NewPassword);
            if (!success)
            {
                throw new ValidationException("Invalid or expired reset token");
            }

            return true;
        }, "ResetPassword");
    }

    #endregion

    #region Account Operations

    /// <summary>
    /// Toggle account active status (ACTIVE <-> INACTIVE)
    /// </summary>
    public async Task<Status?> ToggleAccountActiveStatusAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Toggling account active status: {AccountId}", null, id);

            var account = await _authRepository.GetAccountByIdAsync(id) ?? throw new AccountNotFoundException(id);

            account.Status = account.Status == Status.ACTIVE ? Status.INACTIVE : Status.ACTIVE;

            await _authRepository.UpdateAccountAsync(account);
            return account.Status;
        }, "ToggleAccountActiveStatus");
    }

    /// <summary>
    /// Lock account
    /// </summary>
    public async Task<bool> LockAccountAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Locking account: {AccountId}", null, id);
            var account = await _authRepository.GetAccountByIdAsync(id) ?? throw new AccountNotFoundException(id);
            return await _authRepository.LockAccountAsync(account);
        }, "LockAccount");
    }

    /// <summary>
    /// Unlock account
    /// </summary>
    public async Task<bool> UnlockAccountAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Unlocking account: {AccountId}", null, id);
            var account = await _authRepository.GetAccountByIdAsync(id) ?? throw new AccountNotFoundException(id);
            return await _authRepository.UnlockAccountAsync(account);
        }, "UnlockAccount");
    }

    #endregion

    #region Role Operations

    /// <summary>
    /// Create new role
    /// </summary>
    public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating new role: {RoleName}", null, request.Name);

            // Check if role name already exists
            if (await _authRepository.RoleNameExistsAsync(request.Name))
            {
                throw new RoleConflictException(request.Name, true);
            }

            var role = _mapper.Map<RoleEntity>(request);
            var createdRole = await _authRepository.CreateRoleAsync(role);

            LogInfo("Role created successfully: {RoleId}", null, createdRole.Id);
            return _mapper.Map<RoleResponse>(createdRole);
        }, "CreateRole");
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    public async Task<RoleResponse?> GetRoleByIdAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting role by ID: {RoleId}", null, id);

            var role = await _authRepository.GetRoleByIdAsync(id);
            if (role == null)
            {
                LogWarning("Role not found: {RoleId}", null, id);
                return null;
            }

            LogInfo("Role found: {RoleId}", null, role.Id);
            return _mapper.Map<RoleResponse>(role);
        }, "GetRoleById");
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    public async Task<RoleResponse?> GetRoleByNameAsync(string name)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting role by name: {RoleName}", null, name);

            var role = await _authRepository.GetRoleByNameAsync(name);
            if (role == null)
            {
                LogWarning("Role not found: {RoleName}", null, name);
                return null;
            }

            LogInfo("Role found: {RoleId}", null, role.Id);
            return _mapper.Map<RoleResponse>(role);
        }, "GetRoleByName");
    }

    /// <summary>
    /// Update role
    /// </summary>
    public async Task<RoleResponse> UpdateRoleAsync(UpdateRoleRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating role: {RoleId}", null, request.Id);

            var existingRole = await _authRepository.GetRoleByIdAsync(request.Id);
            if (existingRole == null)
            {
                throw new RoleNotFoundException(request.Id);
            }

            // Check if there are any changes (case-insensitive)
            var nameChanged = !string.Equals(request.Name, existingRole.Name, StringComparison.Ordinal);
            var descriptionChanged = !string.IsNullOrEmpty(request.Description) && request.Description != existingRole.Description;

            // If no changes, return existing role without calling repository
            if (!nameChanged && !descriptionChanged)
            {
                LogInfo("No changes detected for role: {RoleId}", null, existingRole.Id);
                return _mapper.Map<RoleResponse>(existingRole);
            }

            // Check name uniqueness if changed (case-insensitive)
            if (nameChanged)
            {
                if (await _authRepository.RoleNameExistsAsync(request.Name, existingRole.Id))
                {
                    throw new RoleConflictException(request.Name, true);
                }
                existingRole.Name = request.Name;
            }

            if (descriptionChanged)
            {
                existingRole.Description = request.Description;
            }

            var updatedRole = await _authRepository.UpdateRoleAsync(existingRole);
            LogInfo("Role updated successfully: {RoleId}", null, updatedRole.Id);
            return _mapper.Map<RoleResponse>(updatedRole);
        }, "UpdateRole");
    }

    /// <summary>
    /// Delete role
    /// </summary>
    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Deleting role: {RoleId}", null, id);

            var result = await _authRepository.DeleteRoleAsync(id);
            if (result)
            {
                LogInfo("Role deleted successfully: {RoleId}", null, id);
            }
            else
            {
                LogWarning("Role not found for deletion: {RoleId}", null, id);
            }

            return result;
        }, "DeleteRole");
    }

    /// <summary>
    /// Get roles with filtering and pagination
    /// </summary>
    public async Task<RoleListResponse> GetRolesAsync(RoleQueryRequest query)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting roles with query: Page={PageNumber}, Size={PageSize}", null, query.PageNumber, query.PageSize);

            var (roles, totalCount) = await _authRepository.GetRolesAsync(query);
            var roleResponses = _mapper.Map<List<RoleResponse>>(roles);

            var response = new RoleListResponse
            {
                Roles = roleResponses,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
            };

            LogInfo("Retrieved {Count} roles out of {TotalCount}", null, roles.Count, totalCount);
            return response;
        }, "GetRoles");
    }

    #endregion

    #region Permission Operations

    /// <summary>
    /// Create new permission
    /// </summary>
    public async Task<PermissionResponse> CreatePermissionAsync(CreatePermissionRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating new permission: {PermissionName}", null, request.Name);

            // Check if permission name already exists
            if (await _authRepository.PermissionNameExistsAsync(request.Name))
            {
                throw new PermissionConflictException(request.Name, true);
            }

            var permission = _mapper.Map<PermissionEntity>(request);
            var createdPermission = await _authRepository.CreatePermissionAsync(permission);

            LogInfo("Permission created successfully: {PermissionId}", null, createdPermission.Id);
            return _mapper.Map<PermissionResponse>(createdPermission);
        }, "CreatePermission");
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    public async Task<PermissionResponse?> GetPermissionByIdAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting permission by ID: {PermissionId}", null, id);

            var permission = await _authRepository.GetPermissionByIdAsync(id);
            if (permission == null)
            {
                LogWarning("Permission not found: {PermissionId}", null, id);
                return null;
            }

            LogInfo("Permission found: {PermissionId}", null, permission.Id);
            return _mapper.Map<PermissionResponse>(permission);
        }, "GetPermissionById");
    }

    /// <summary>
    /// Get permission by name
    /// </summary>
    public async Task<PermissionResponse?> GetPermissionByNameAsync(string name)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting permission by name: {PermissionName}", null, name);

            var permission = await _authRepository.GetPermissionByNameAsync(name);
            if (permission == null)
            {
                LogWarning("Permission not found: {PermissionName}", null, name);
                return null;
            }

            LogInfo("Permission found: {PermissionId}", null, permission.Id);
            return _mapper.Map<PermissionResponse>(permission);
        }, "GetPermissionByName");
    }

    /// <summary>
    /// Update permission
    /// </summary>
    public async Task<PermissionResponse> UpdatePermissionAsync(UpdatePermissionRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating permission: {PermissionId}", null, request.Id);

            var existingPermission = await _authRepository.GetPermissionByIdAsync(request.Id);
            if (existingPermission == null)
            {
                throw new PermissionNotFoundException(request.Id);
            }

            // Check if there are any changes (case-insensitive)
            var nameChanged = !string.Equals(request.Name, existingPermission.Name, StringComparison.Ordinal);
            var descriptionChanged = !string.IsNullOrEmpty(request.Description) && request.Description != existingPermission.Description;

            // If no changes, return existing permission without calling repository
            if (!nameChanged && !descriptionChanged)
            {
                LogInfo("No changes detected for permission: {PermissionId}", null, existingPermission.Id);
                return _mapper.Map<PermissionResponse>(existingPermission);
            }

            // Check name uniqueness if changed (case-insensitive)
            if (nameChanged)
            {
                if (await _authRepository.PermissionNameExistsAsync(request.Name, request.Id))
                {
                    throw new PermissionConflictException(request.Name, true);
                }
                existingPermission.Name = request.Name;
            }

            if (descriptionChanged)
            {
                existingPermission.Description = request.Description;
            }

            var updatedPermission = await _authRepository.UpdatePermissionAsync(existingPermission);
            LogInfo("Permission updated successfully: {PermissionId}", null, updatedPermission.Id);
            return _mapper.Map<PermissionResponse>(updatedPermission);
        }, "UpdatePermission");
    }

    /// <summary>
    /// Delete permission
    /// </summary>
    public async Task<bool> DeletePermissionAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Deleting permission: {PermissionId}", null, id);

            var result = await _authRepository.DeletePermissionAsync(id);
            if (result)
            {
                LogInfo("Permission deleted successfully: {PermissionId}", null, id);
            }
            else
            {
                LogWarning("Permission not found for deletion: {PermissionId}", null, id);
            }

            return result;
        }, "DeletePermission");
    }

    /// <summary>
    /// Get permissions with filtering and pagination
    /// </summary>
    public async Task<PermissionListResponse> GetPermissionsAsync(PermissionQueryRequest query)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting permissions with query: Page={PageNumber}, Size={PageSize}", null, query.PageNumber, query.PageSize);

            var (permissions, totalCount) = await _authRepository.GetPermissionsAsync(query);
            var permissionResponses = _mapper.Map<List<PermissionResponse>>(permissions);

            var response = new PermissionListResponse
            {
                Permissions = permissionResponses,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
            };

            LogInfo("Retrieved {Count} permissions out of {TotalCount}", null, permissions.Count, totalCount);
            return response;
        }, "GetPermissions");
    }

    #endregion

    #region Account-Role Operations

    /// <summary>
    /// Assign role to account
    /// </summary>
    public async Task<AccountRoleResponse> AssignRoleToAccountAsync(AssignRoleRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Assigning role to account: AccountId={AccountId}, RoleId={RoleId}", null, request.AccountId, request.RoleId);
            var account = await _authRepository.GetAccountByIdAsync(request.AccountId) ?? throw new AccountNotFoundException(request.AccountId);
            var role = await _authRepository.GetRoleByIdAsync(request.RoleId) ?? throw new RoleNotFoundException(request.RoleId);
            // Check if account already has this role
            if (await _authRepository.RoleAlreadyAssignedAsync(account, role.Name!))
            {
                LogWarning("Account {AccountId} already has role {RoleName}", account.Id.ToString(), role.Name!);
                throw new RoleAlreadyAssignedException(account.Id, role.Name!);
            }
            var accountRole = await _authRepository.AssignRoleToAccountAsync(account, role);

            LogInfo("Role assigned successfully: AccountId={AccountId}, RoleId={RoleId}", null, request.AccountId, request.RoleId);
            return _mapper.Map<AccountRoleResponse>(accountRole);
        }, "AssignRoleToAccount");
    }

    /// <summary>
    /// Remove role from account
    /// </summary>
    public async Task<bool> RemoveRoleFromAccountAsync(RemoveRoleRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Removing role from account: AccountId={AccountId}, RoleId={RoleId}", null, request.AccountId, request.RoleId);
            var account = await _authRepository.GetAccountByIdAsync(request.AccountId) ?? throw new AccountNotFoundException(request.AccountId);
            var role = await _authRepository.GetRoleByIdAsync(request.RoleId) ?? throw new RoleNotFoundException(request.RoleId);

            // Check if account has this role before attempting removal
            if (!await _authRepository.RoleAlreadyAssignedAsync(account, role.Name!))
            {
                LogWarning("Account {AccountId} does not have role {RoleName}", account.Id.ToString(), role.Name!);
                throw new RoleNotAssignedException(account.Id, role.Name!);
            }
            var result = await _authRepository.RemoveRoleFromAccountAsync(account, role);

            LogInfo("Role removal result: AccountId={AccountId}, RoleId={RoleId}, Success={Success}", null, request.AccountId, request.RoleId, result);
            return result;
        }, "RemoveRoleFromAccount");
    }

    /// <summary>
    /// Get account roles
    /// </summary>
    public async Task<List<RoleResponse>> GetAccountRolesAsync(Guid accountId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting account roles: AccountId={AccountId}", null, accountId);
            var account = await _authRepository.GetAccountByIdAsync(accountId) ?? throw new AccountNotFoundException(accountId);

            var roles = await _authRepository.GetAccountRolesAsync(account);
            var roleResponses = _mapper.Map<List<RoleResponse>>(roles);

            LogInfo("Retrieved {Count} roles for account: AccountId={AccountId}", null, roleResponses.Count, accountId);
            return roleResponses;
        }, "GetAccountRoles");
    }

    /// <summary>
    /// Get accounts by role
    /// </summary>
    public async Task<List<AccountResponse>> GetAccountsByRoleAsync(Guid roleId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting accounts by role: RoleId={RoleId}", null, roleId);
            var role = await _authRepository.GetRoleByIdAsync(roleId) ?? throw new RoleNotFoundException(roleId);

            var accounts = await _authRepository.GetAccountsByRoleAsync(role);
            var accountResponses = _mapper.Map<List<AccountResponse>>(accounts);

            LogInfo("Retrieved {Count} accounts for role: RoleId={RoleId}", null, accountResponses.Count, roleId);
            return accountResponses;
        }, "GetAccountsByRole");
    }

    #endregion

    #region Role-Permission Operations

    /// <summary>
    /// Assign permission to role
    /// </summary>
    public async Task<RolePermissionResponse> AssignPermissionToRoleAsync(AssignPermissionRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Assigning permission to role: RoleId={RoleId}, PermissionId={PermissionId}", null, request.RoleId, request.PermissionId);
            var role = await _authRepository.GetRoleByIdAsync(request.RoleId) ?? throw new RoleNotFoundException(request.RoleId);
            var permission = await _authRepository.GetPermissionByIdAsync(request.PermissionId) ?? throw new PermissionNotFoundException(request.PermissionId);

            if (await _authRepository.RoleHasPermissionAsync(role.Id, permission.Id))
            {
                LogWarning("Role {RoleId} does not have permission {PermissionName}", role.Id.ToString(), permission.Name);
                throw new PermissionAlreadyAssignedException(role.Id, permission.Id);
            }

            var rolePermission = await _authRepository.AssignPermissionToRoleAsync(request.RoleId, request.PermissionId);

            LogInfo("Permission assigned successfully: RoleId={RoleId}, PermissionId={PermissionId}", null, request.RoleId, request.PermissionId);
            return _mapper.Map<RolePermissionResponse>(rolePermission);
        }, "AssignPermissionToRole");
    }

    /// <summary>
    /// Remove permission from role
    /// </summary>
    public async Task<bool> RemovePermissionFromRoleAsync(RemovePermissionRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Removing permission from role: RoleId={RoleId}, PermissionId={PermissionId}", null, request.RoleId, request.PermissionId);
            var role = await _authRepository.GetRoleByIdAsync(request.RoleId) ?? throw new RoleNotFoundException(request.RoleId);
            var permission = await _authRepository.GetPermissionByIdAsync(request.PermissionId) ?? throw new PermissionNotFoundException(request.PermissionId);

            if (!await _authRepository.RoleHasPermissionAsync(role.Id, permission.Id))
            {
                LogWarning("Role {RoleId} does not have permission {PermissionId}", role.Id.ToString(), permission.Id.ToString());
                throw new PermissionNotAssignedException(role.Id, permission.Id);
            }

            var result = await _authRepository.RemovePermissionFromRoleAsync(request.RoleId, request.PermissionId);

            LogInfo("Permission removal result: RoleId={RoleId}, PermissionId={PermissionId}, Success={Success}", null, request.RoleId, request.PermissionId, result);
            return result;
        }, "RemovePermissionFromRole");
    }

    /// <summary>
    /// Get role permissions
    /// </summary>
    public async Task<List<PermissionResponse>> GetRolePermissionsAsync(Guid roleId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting role permissions: RoleId={RoleId}", null, roleId);
            var role = await _authRepository.GetRoleByIdAsync(roleId) ?? throw new RoleNotFoundException(roleId);

            var permissions = await _authRepository.GetRolePermissionsAsync(role.Id);
            var permissionResponses = _mapper.Map<List<PermissionResponse>>(permissions);

            LogInfo("Retrieved {Count} permissions for role: RoleId={RoleId}", null, permissionResponses.Count, roleId);
            return permissionResponses;
        }, "GetRolePermissions");
    }

    /// <summary>
    /// Get roles by permission
    /// </summary>
    public async Task<List<RoleResponse>> GetRolesByPermissionAsync(Guid permissionId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting roles by permission: PermissionId={PermissionId}", null, permissionId);
            _ = await _authRepository.GetPermissionByIdAsync(permissionId) ?? throw new PermissionNotFoundException(permissionId);

            var roles = await _authRepository.GetRolesByPermissionAsync(permissionId);
            var roleResponses = _mapper.Map<List<RoleResponse>>(roles);

            LogInfo("Retrieved {Count} roles for permission: PermissionId={PermissionId}", null, roleResponses.Count, permissionId);
            return roleResponses;
        }, "GetRolesByPermission");
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Find account by email or phone number
    /// </summary>
    private async Task<AccountEntity?> FindAccountByEmailOrPhoneAsync(string? email, string? phoneNumber)
    {
        // Try to find by email if provided
        if (!string.IsNullOrWhiteSpace(email))
        {
            var account = await _authRepository.GetAccountByEmailAsync(email);
            if (account != null)
            {
                return account;
            }
        }

        // Try to find by phone number if provided
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var account = await _authRepository.GetAccountByPhoneNumberAsync(phoneNumber);
            if (account != null)
            {
                return account;
            }
        }

        return null;
    }

    private static string BuildResetUrl(string baseUrl, string email, string token)
    {
        var separator = baseUrl.EndsWith('/') ? string.Empty : "/";
        return $"{baseUrl}{separator}reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    #endregion

    #region External Authentication Operations

    /// <summary>
    /// Authenticate account with Google access token
    /// </summary>
    public async Task<AuthResponse> GoogleLoginAsync(ExternalAuthRequest request)
    {
        return await ExternalLoginAsync(
            request.AccessToken,
            "Google",
            "LoginGoogle",
            async token => await _externalAuthProviderService.VerifyGoogleAccessTokenAsync(token),
            userInfo => userInfo?.Sub,
            userInfo => userInfo?.Email,
            userInfo => userInfo?.Name,
            userInfo => userInfo?.Picture,
            "Invalid Google access token",
            "Google login successful"
        );
    }

    /// <summary>
    /// Authenticate user with Facebook access token
    /// </summary>
    public async Task<AuthResponse> FacebookLoginAsync(ExternalAuthRequest request)
    {
        return await ExternalLoginAsync(
            request.AccessToken,
            "Facebook",
            "LoginFacebook",
            async token => await _externalAuthProviderService.VerifyFacebookAccessTokenAsync(token),
            userInfo => userInfo?.Id,
            userInfo => userInfo?.Email,
            userInfo => userInfo?.Name,
            userInfo => userInfo?.Picture,
            "Invalid Facebook access token",
            "Facebook login successful"
        );
    }

    /// <summary>
    /// Common external authentication flow for Google and Facebook
    /// </summary>
    private async Task<AuthResponse> ExternalLoginAsync<T>(
        string accessToken,
        string providerName,
        string operationName,
        Func<string, Task<T?>> tokenVerifier,
        Func<T, string?> userIdExtractor,
        Func<T, string?> emailExtractor,
        Func<T, string?> nameExtractor,
        Func<T, string?> pictureExtractor,
        string invalidTokenMessage,
        string successMessage) where T : class
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Verify access token and get user info
            var userInfo = await tokenVerifier(accessToken);
            if (userInfo == null)
            {
                throw new UnauthorizedAccessException(invalidTokenMessage);
            }

            var email = emailExtractor(userInfo);
            var userId = userIdExtractor(userInfo);
            var fullName = nameExtractor(userInfo) ?? ExtractNameFromEmail(email ?? "");
            var avatarUrl = pictureExtractor(userInfo);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException($"Invalid {providerName} user information");
            }

            // Check if account exists by email
            var existingAccount = await _authRepository.GetAccountByEmailAsync(email);
            if (existingAccount != null)
            {
                return await HandleExistingAccountLoginAsync(existingAccount, providerName, userId, successMessage);
            }
            else
            {
                return await CreateNewAccountAndLoginAsync(email, providerName, userId, fullName, avatarUrl, successMessage);
            }
        }, operationName);
    }

    /// <summary>
    /// Handle login for existing accounts
    /// </summary>
    private async Task<AuthResponse> HandleExistingAccountLoginAsync(
        AccountEntity existingAccount,
        string providerName,
        string userId,
        string successMessage)
    {
        // Check account status first
        if (existingAccount.Status != Status.ACTIVE)
        {
            throw new AuthenticationException("Account is not active");
        }

        // Check if account is locked out
        if (await _authRepository.IsAccountLockedOutAsync(existingAccount))
        {
            throw new AuthenticationException("Account is temporarily locked due to too many failed login attempts");
        }

        // Check if account already has this external login
        var hasExternalLogin = await _authRepository.HasExternalLoginAsync(existingAccount.Id, providerName, userId);
        if (!hasExternalLogin)
        {
            // Add external login to existing account
            await _authRepository.AddExternalLoginAsync(existingAccount.Id, providerName, userId);
        }

        return await GenerateAuthResponseAsync(existingAccount, successMessage);
    }

    /// <summary>
    /// Create new account and login using Saga pattern
    /// </summary>
    private async Task<AuthResponse> CreateNewAccountAndLoginAsync(
        string email,
        string providerName,
        string userId,
        string fullName,
        string? avatarUrl,
        string successMessage)
    {
        // Use Saga pattern to create complete user (Account + User Profile)
        var sagaResult = await CreateExternalUserWithSagaAsync(email, providerName, userId, fullName, avatarUrl);

        if (!sagaResult.Success)
        {
            throw new InvalidOperationException($"Failed to create user account: {sagaResult.Message}");
        }

        // Get the created account
        var createdAccount = await _authRepository.GetAccountByEmailAsync(email);
        if (createdAccount == null)
        {
            throw new InvalidOperationException("Account was not created successfully");
        }

        return await GenerateAuthResponseAsync(createdAccount, successMessage);
    }

    /// <summary>
    /// Create external user (Account + User Profile) using Saga pattern
    /// </summary>
    private async Task<(bool Success, string Message)> CreateExternalUserWithSagaAsync(
        string email,
        string providerName,
        string userId,
        string fullName,
        string? avatarUrl)
    {
        // Create saga context for external user registration
        var sagaContext = new SagaContext
        {
            SagaName = "ExternalUserRegistration",
            CorrelationId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        // Set external user data (limited information from Google/Facebook)
        sagaContext.SetData("Email", email);
        sagaContext.SetData("FullName", fullName);
        sagaContext.SetData("AvatarUrl", avatarUrl ?? "");
        sagaContext.SetData("ExternalProvider", providerName);
        sagaContext.SetData("ExternalUserId", userId);

        try
        {
            // Execute External User Registration Saga
            var result = await _sagaManager.ExecuteSagaAsync<ExternalUserRegistrationSaga>(sagaContext);

            if (result.Status == SagaStatus.Completed)
            {
                LogInfo("External user registration completed successfully for {Email} via {Provider}", null, email, providerName);
                return (true, "External user registration completed successfully");
            }
            else
            {
                LogError(new InvalidOperationException("External user registration failed"), "External user registration failed for {Email} via {Provider}: {Error}", null, email, providerName, result.ErrorMessage ?? "Unknown error");
                return (false, result.ErrorMessage ?? "External user registration failed");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error executing external user registration saga for {Email}: {Error}", email, ex.Message);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Create external account for Saga step - contains the core external account creation logic
    /// </summary>
    public async Task<(bool Success, string AccountId, string Message)> CreateExternalAccountForSagaAsync(
        string email,
        string fullName,
        string? avatarUrl,
        string externalProvider,
        string externalUserId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating external account for email: {Email} via {Provider}", null, email, externalProvider);

            // Check if account already exists
            if (await _authRepository.EmailExistsAsync(email))
            {
                throw new AccountConflictException(email, "Email");
            }

            // Create account entity for external user
            var account = new AccountEntity
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true, // External provider email is already verified
            };

            // Get Patient role (external users are always patients)
            var targetRole = await _authRepository.GetRoleByNameAsync("Patient");
            if (targetRole == null)
            {
                throw new ValidationException("Role 'Patient' does not exist in the system. Cannot create external account without valid role.");
            }

            var result = await _authRepository.CreateAccountAsync(account);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create account: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }


            // Add external login
            await _authRepository.AddExternalLoginAsync(account.Id, externalProvider, externalUserId);

            // Assign Patient role to account
            await _authRepository.AssignRoleToAccountAsync(account, targetRole);
            LogInfo("Role 'Patient' assigned to external account: {Email}", null, email);

            LogInfo("External account created successfully for email: {Email} via {Provider}", null, email, externalProvider);

            return (true, account.Id.ToString(), "External account created successfully");
        }, "CreateExternalAccountForSaga");
    }


    /// <summary>
    /// Extract name from email for external users
    /// </summary>
    private static string ExtractNameFromEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "";

        var localPart = email.Split('@')[0];
        // Replace common separators with spaces and capitalize
        var name = localPart.Replace(".", " ").Replace("_", " ").Replace("-", " ");
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
    }

    /// <summary>
    /// Generate authentication response with tokens
    /// </summary>
    private async Task<AuthResponse> GenerateAuthResponseAsync(AccountEntity account, string message)
    {
        // Generate JWT access token with roles and permissions
        var accessToken = await _jwtService.GenerateAccessTokenAsync(account);

        // Generate and store refresh token
        var refreshTokenEntity = await _authRepository.CreateRefreshTokenAsync(account.Id);

        // Save tokens in cookies
        _cookieService.SaveTokensInCookies(account.Id, accessToken, refreshTokenEntity.Token);

        return new AuthResponse
        {
            Message = message,
            Token = accessToken
        };
    }

    #endregion
}
