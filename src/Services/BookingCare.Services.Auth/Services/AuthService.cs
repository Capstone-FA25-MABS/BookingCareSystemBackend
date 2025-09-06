using AutoMapper;
using BookingCare.Services.Auth.Exceptions;
using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Models.Entities;
using BookingCare.Services.Auth.Repositories;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Exceptions;
using BookingCare.Shared.Common.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using Microsoft.Extensions.Caching.Distributed;
using BookingCare.Shared.Common.AppRouting;
using Microsoft.Extensions.Options;
using BookingCare.Services.Notification.Protos;

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
    private readonly RefreshTokenService _refreshTokenService;
    private readonly CookieService _cookieService;
    private readonly IEventBus _eventBus;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDistributedCache? _distributedCache;
    private readonly FrontendOptions _frontendOptions;
    private readonly OtpVerifier.OtpVerifierClient _otpClient;

    public AuthService(
        IAuthRepository authRepository,
        IMapper mapper,
        ILogger<AuthService> logger,
        IConfiguration configuration,
        JwtService jwtService,
        RefreshTokenService refreshTokenService,
        CookieService cookieService,
        IEventBus eventBus,
        IHttpContextAccessor httpContextAccessor,
        IDistributedCache? distributedCache = null,
        IOptions<FrontendOptions>? frontendOptions = null,
        OtpVerifier.OtpVerifierClient? otpClient = null) : base(logger)
    {
        _authRepository = authRepository;
        _mapper = mapper;
        _configuration = configuration;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _cookieService = cookieService;
        _eventBus = eventBus;
        _httpContextAccessor = httpContextAccessor;
        _distributedCache = distributedCache;
        _frontendOptions = frontendOptions?.Value ?? new FrontendOptions();
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
            LogInfo("Login attempt for: {EmailOrPhone}", null, request.EmailOrPhone);

            // Validate business rules
            ValidateLoginRequest(request);

            // Find account by email or phone number
            var account = await FindAccountByEmailOrPhoneAsync(request.EmailOrPhone);
            if (account == null)
            {
                throw new AuthenticationException($"Account with email or phone '{request.EmailOrPhone}' not found");
            }

            // Check if account is locked out first
            if (await _authRepository.IsAccountLockedOutAsync(account.Id))
            {
                throw new AuthenticationException($"Account '{request.EmailOrPhone}' is temporarily locked due to too many failed login attempts");
            }

            // Validate credentials with lockout support
            var isValid = await _authRepository.ValidateCredentialsWithLockoutAsync(account.Email!, request.Password);
            if (!isValid)
            {
                throw new AuthenticationException($"Invalid password for account '{request.EmailOrPhone}'");
            }

            // Check account status
            if (account.Status != Status.ACTIVE)
            {
                throw new AuthenticationException($"Account '{request.EmailOrPhone}' is not active");
            }

            // Generate JWT access token with roles and permissions
            var accessToken = await _jwtService.GenerateAccessTokenAsync(account);
        
            // Generate and store refresh token
            var refreshTokenEntity = await _refreshTokenService.CreateRefreshTokenAsync(account.Id);

            // Save tokens in cookies
            _cookieService.SaveTokensInCookies(account.Id, accessToken, refreshTokenEntity.Token);

            LogInfo("Login successful for: {EmailOrPhone}", null, request.EmailOrPhone);
                    
            return new AuthResponse
            {
                IsSuccess = true,
                Message = "Login successful",
                AccessToken = accessToken,
                RefreshToken = refreshTokenEntity.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15), // Match JWT expiration
                Account = _mapper.Map<AccountResponse>(account)
            };
        }, "Login");
    }

    /// <summary>
    /// Register new account
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, Role role)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Registration attempt for email: {Email}", null, request.Email);

            // Validate business rules
            ValidateRegisterRequest(request);

            // Role-specific validation
            ValidateRegisterByRole(request, role);

            // Additional security: for Patient registration, require prior OTP verification (phone or email)
            if (role == Role.PATIENT)
            {
                var purpose = request.Purpose.ToKey();
                var channel = string.IsNullOrWhiteSpace(request.Channel) ? "phone" : request.Channel.ToLowerInvariant();
                var subject = channel == "email" ? $"email:{request.Email}" : $"phone:{request.PhoneNumber}";
                var verified = await VerifyOtpOrProofAsync(purpose, subject, request.Proof, request.IssuedAt, consumeFlag: true);
                if (!verified) throw new ValidationException("OTP verification required before registration");
            }

            // Check if email already exists
            if (await _authRepository.EmailExistsAsync(request.Email))
            {
                throw new AccountConflictException(request.Email, "Email");
            }

            // Check if phone number already exists
            if (await _authRepository.PhoneNumberExistsAsync(request.PhoneNumber))
            {
                throw new AccountConflictException(request.PhoneNumber, "PhoneNumber");
            }

            // Create account entity
            var account = _mapper.Map<AccountEntity>(request);

            // Create account
            var createdAccount = await _authRepository.CreateAccountAsync(account, request.Password);

            // Assign role based on request (default Patient)
            try
            {
                var targetRoleName = role switch { Role.DOCTOR => "Doctor", Role.CLINIC => "Clinic", _ => "Patient" };
                var defaultRole = await _authRepository.GetRoleByNameAsync(targetRoleName);
                if (defaultRole != null)
                {
                    await _authRepository.AssignRoleToAccountAsync(createdAccount.Id, defaultRole.Id);
                    LogInfo("Role '{Role}' assigned to account: {Email}", null, targetRoleName, request.Email);
                }
                else
                {
                    LogWarning("Role '{Role}' not found. Account created without role: {Email}", null, targetRoleName, request.Email);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail registration
                LogError(ex, "Failed to assign role to account: {Email}", null, request.Email);
            }

            // Generate JWT token
            //var token = GenerateJwtToken(createdAccount);
            //var refreshToken = GenerateRefreshToken();

            LogInfo("Registration successful for email: {Email}", null, request.Email);

            return new AuthResponse
            {
                IsSuccess = true,
                Message = "Registration successful",
            };
        }, "Register");
    }

    /// <summary>
    /// Refresh JWT token using refresh token
    /// </summary>
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Refresh token attempt");

            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new AuthenticationException("Refresh token is required");
            }

            // Validate refresh token
            var (isValid, account, tokenEntity) = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken);
            
            if (!isValid || account == null)
            {    
                throw new AuthenticationException("Invalid or expired refresh token");
            }

            // Check if account is still active
            if (account.Status != Status.ACTIVE)
            {
                throw new AuthenticationException($"Account is not active");
            }

            // Generate new access token
            var newAccessToken = await _jwtService.GenerateAccessTokenAsync(account);

            // Save tokens in cookies (reuse same refresh token if still valid)
            _cookieService.SaveTokensInCookies(account.Id, newAccessToken, tokenEntity!.Token);

            LogInfo("Token refreshed successfully for account: {AccountId}", null, account.Id);

            return new AuthResponse
            {
                IsSuccess = true,
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

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _refreshTokenService.DeleteRefreshTokenAsync(refreshToken);
            }

            // Clear authentication cookies
            _cookieService.ClearAuthenticationCookies();

            LogInfo("Logout successful");
            return true;
        }, "Logout");
    }

    /// <summary>
    /// Change account password
    /// </summary>
    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Password change attempt for account: {AccountId}", null, request.AccountId);

            // Validate business rules
            ValidateChangePasswordRequest(request);

            // Get account
            var account = await _authRepository.GetAccountByIdAsync(request.AccountId);
            if (account == null)
            {
                throw new AccountNotFoundException(request.AccountId);
            }

            // Check if account has external login providers
            var hasExternalLogin = await _authRepository.HasExternalLoginAsync(request.AccountId);

            // Validate current password for regular accounts
            if (!hasExternalLogin)
            {
                if (string.IsNullOrEmpty(request.CurrentPassword))
                {
                    throw new AuthenticationException("Current password is required for regular accounts");
                }

                // Validate current password
                var isValidCurrentPassword = await _authRepository.ValidateCredentialsAsync(account.Email!, request.CurrentPassword);
                if (!isValidCurrentPassword)
                {
                    throw new AuthenticationException("Current password is incorrect");
                }
            }
            else
            {
                // For external login accounts, current password is not required
                LogInfo("Account has external login providers, skipping current password validation", null, request.AccountId);
            }

            // Validate NewPassword and ConfirmNewPassword
            if (request.NewPassword != request.ConfirmNewPassword)
            {
                throw new ValidationException("New password and confirm password do not match");
            }

            // Validate password complexity
            var passwordCheck = await _authRepository.ValidatePasswordAsync(account, request.NewPassword);
            if (!passwordCheck.IsValid)
            {
                var errors = string.Join(", ", passwordCheck.Errors);
                throw new ValidationException($"Password validation failed: {errors}");
            }

            // Change password
            var result = await _authRepository.ChangePasswordAsync(request.AccountId, request.NewPassword);
            if (!result)
            {
                throw new AuthException("Failed to change password");
            }

            LogInfo("Password changed successfully for account: {AccountId}", null, request.AccountId);
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
            // Validate input: either Email or PhoneNumber must be provided
            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                throw new ValidationException("Either Email or PhoneNumber is required");
            }

            // Determine app origin for building reset URL
            var origin = _httpContextAccessor.HttpContext?.Request.Headers["Origin"].FirstOrDefault() ?? string.Empty;
            var appPrefix = AppRoutingHelper.GetAppPrefix(origin, _frontendOptions);
            var baseUrl = AppRoutingHelper.ResolveBaseUrl(appPrefix, _frontendOptions);
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = _configuration["Frontend:default:BaseUrl"] ?? string.Empty;
            }

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
                    Type = "email",
                    Data = new Dictionary<string, object>
                    {
                        { "email", request.Email! },
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
    /// Issue reset token and URL after successful OTP verification (phone flow)
    /// </summary>
    public async Task<IssueResetTokenResponse> IssueResetTokenAsync(IssueResetTokenRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.PhoneNumber, nameof(request.PhoneNumber));

            // Unified verification (Redis flag or HMAC proof)
            var purpose = request.Purpose.ToKey();
            var subject = $"phone:{request.PhoneNumber}";
            var verified = await VerifyOtpOrProofAsync(purpose, subject, request.Proof, request.IssuedAt, consumeFlag: true);
            if (!verified)
            {
                // Return neutral response without revealing status
                return new IssueResetTokenResponse { Email = string.Empty, ResetToken = string.Empty, ResetUrl = string.Empty };
            }

            var (found, email, accountId, token) = await _authRepository.GeneratePasswordResetTokenByPhoneAsync(request.PhoneNumber);
            if (!found)
            {
                // Do not reveal
                return new IssueResetTokenResponse { Email = string.Empty, ResetToken = string.Empty, ResetUrl = string.Empty };
            }

            var origin = _httpContextAccessor.HttpContext?.Request.Headers["Origin"].FirstOrDefault() ?? string.Empty;
            var appPrefix = AppRoutingHelper.GetAppPrefix(origin, _frontendOptions);
            var baseUrl = AppRoutingHelper.ResolveBaseUrl(appPrefix, _frontendOptions);
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = _configuration["Frontend:default:BaseUrl"] ?? string.Empty;
            }

            var resetUrl = BuildResetUrl(baseUrl, email, token);
            return new IssueResetTokenResponse
            {
                Email = email,
                ResetToken = token,
                ResetUrl = resetUrl
            };
        }, "IssueResetToken");
    }

    private bool VerifyOtpProof(string purpose, string? proof, long? issuedAt, IEnumerable<string> subjects)
    {
        var secret = _configuration.GetSection("OtpVerification").GetValue<string>("Secret") ?? string.Empty;
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(proof) || !issuedAt.HasValue)
            return false;

        var age = Math.Abs((DateTimeOffset.UtcNow.Ticks - issuedAt.Value) / TimeSpan.TicksPerMinute);
        if (age > 5) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        foreach (var subject in subjects)
        {
            var data = $"{purpose}:{subject}:{issuedAt.Value}";
            var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
            if (string.Equals(expected, proof, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private async Task<bool> VerifyOtpOrProofAsync(string purpose, string subject, string? proof, long? issuedAt, bool consumeFlag)
    {
        // 1) Try gRPC if enabled
        var otpSection = _configuration.GetSection("OtpVerification");
        var useGrpc = otpSection.GetValue<bool>("UseGrpc");
        var allowProofFallback = otpSection.GetValue<bool>("AllowProofFallback", true);
        var timeoutMs = otpSection.GetValue<int>("TimeoutMs", 1000);

        // 2) Try Redis local flag (best-effort, ignore connectivity errors)
        var flagKey = $"otp:verified:{purpose}:{subject}".ToLowerInvariant();
        if (useGrpc && _otpClient != null)
        {
            bool grpcOk = false;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
                var resp = await _otpClient.VerifyAsync(new VerifyRequest
                {
                    Key = flagKey,
                    Otp = "1"
                }, cancellationToken: cts.Token);
                grpcOk = resp.Verified;
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

            if (grpcOk)
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
            // Layered validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.Email, nameof(request.Email));
            ValidateRequiredString(request.ResetToken, nameof(request.ResetToken));
            ValidateRequiredString(request.NewPassword, nameof(request.NewPassword));
            ValidateRequiredString(request.ConfirmNewPassword, nameof(request.ConfirmNewPassword));

            if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
            {
                throw new ValidationException("New password and confirm password do not match");
            }

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
            var success = await _authRepository.ResetPasswordWithTokenAsync(normalizedEmail, normalizedToken, request.NewPassword);
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
    /// Get account by email
    /// </summary>
    public async Task<AccountResponse?> GetAccountByEmailAsync(string email)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateRequiredString(email, nameof(email));
            LogInfo("Fetching account by email: {Email}", null, email);

            var account = await _authRepository.GetAccountByEmailAsync(email);
            return account != null ? _mapper.Map<AccountResponse>(account) : null;
        }, "GetAccountByEmail");
    }

    /// <summary>
    /// Get account by phone number
    /// </summary>
    public async Task<AccountResponse?> GetAccountByPhoneNumberAsync(string phoneNumber)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateRequiredString(phoneNumber, nameof(phoneNumber));
            LogInfo("Fetching account by phone: {Phone}", null, phoneNumber);

            var account = await _authRepository.GetAccountByPhoneNumberAsync(phoneNumber);
            return account != null ? _mapper.Map<AccountResponse>(account) : null;
        }, "GetAccountByPhoneNumber");
    }

    /// <summary>
    /// Toggle account active status (ACTIVE <-> INACTIVE)
    /// </summary>
    public async Task<Status?> ToggleAccountActiveStatusAsync(Guid id)  
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Toggling account active status: {AccountId}", null, id);

            var account = await _authRepository.GetAccountByIdAsync(id);
            if (account == null)
                return (Status?)null;

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
            return await _authRepository.LockAccountAsync(id);
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
            return await _authRepository.UnlockAccountAsync(id);
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

            // Check name uniqueness if changed
            if (request.Name != existingRole.Name)
            {
                if (await _authRepository.RoleNameExistsAsync(request.Name, existingRole.Id))
                {
                    throw new RoleConflictException(request.Name, true);
                }
            }

            // Update tracked entity to avoid EF Core double-tracking issues
            existingRole.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Description))
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

            // Check name uniqueness if changed
            if (request.Name != existingPermission.Name)
            {
                if (await _authRepository.PermissionNameExistsAsync(request.Name, request.Id))
                {
                    throw new PermissionConflictException(request.Name, true);
                }
            }

            existingPermission.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Description))
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
        try
        {
            var accountRole = await _authRepository.AssignRoleToAccountAsync(request.AccountId, request.RoleId);
            return _mapper.Map<AccountRoleResponse>(accountRole);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error assigning role to account: AccountId={AccountId}, RoleId={RoleId}", request.AccountId, request.RoleId);
            throw new AuthException("Role assignment failed", innerException: ex);
        }
    }

    /// <summary>
    /// Remove role from account
    /// </summary>
    public async Task<bool> RemoveRoleFromAccountAsync(RemoveRoleRequest request)
    {
        try
        {
            return await _authRepository.RemoveRoleFromAccountAsync(request.AccountId, request.RoleId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing role from account: AccountId={AccountId}, RoleId={RoleId}", request.AccountId, request.RoleId);
            throw new AuthException("Role removal failed", innerException: ex);
        }
    }

    /// <summary>
    /// Get account roles
    /// </summary>
    public async Task<List<RoleResponse>> GetAccountRolesAsync(Guid accountId)
    {
        try
        {
            var roles = await _authRepository.GetAccountRolesAsync(accountId);
            return _mapper.Map<List<RoleResponse>>(roles);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting account roles: {AccountId}", accountId);
            throw new AuthException("Failed to get account roles", innerException: ex);
        }
    }

    /// <summary>
    /// Get accounts by role
    /// </summary>
    public async Task<List<AccountResponse>> GetAccountsByRoleAsync(Guid roleId)
    {
        try
        {
            var accounts = await _authRepository.GetAccountsByRoleAsync(roleId);
            return _mapper.Map<List<AccountResponse>>(accounts);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting accounts by role: {RoleId}", roleId);
            throw new AuthException("Failed to get accounts by role", innerException: ex);
        }
    }

    /// <summary>
    /// Check if account has role
    /// </summary>
    public async Task<bool> AccountHasRoleAsync(Guid accountId, Guid roleId)
    {
        try
        {
            return await _authRepository.AccountHasRoleAsync(accountId, roleId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking if account has role: AccountId={AccountId}, RoleId={RoleId}", accountId, roleId);
            throw new AuthException("Failed to check account role", innerException: ex);
        }
    }

    /// <summary>
    /// Check if account has role by name
    /// </summary>
    public async Task<bool> AccountHasRoleAsync(Guid accountId, string roleName)
    {
        try
        {
            return await _authRepository.AccountHasRoleAsync(accountId, roleName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking if account has role: AccountId={AccountId}, RoleName={RoleName}", accountId, roleName);
            throw new AuthException("Failed to check account role", innerException: ex);
        }
    }

    #endregion

    #region Role-Permission Operations

    /// <summary>
    /// Assign permission to role
    /// </summary>
    public async Task<RolePermissionResponse> AssignPermissionToRoleAsync(AssignPermissionRequest request)
    {
        try
        {
            var rolePermission = await _authRepository.AssignPermissionToRoleAsync(request.RoleId, request.PermissionId);
            return _mapper.Map<RolePermissionResponse>(rolePermission);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error assigning permission to role: RoleId={RoleId}, PermissionId={PermissionId}", request.RoleId, request.PermissionId);
            throw new AuthException("Permission assignment failed", innerException: ex);
        }
    }

    /// <summary>
    /// Remove permission from role
    /// </summary>
    public async Task<bool> RemovePermissionFromRoleAsync(RemovePermissionRequest request)
    {
        try
        {
            return await _authRepository.RemovePermissionFromRoleAsync(request.RoleId, request.PermissionId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing permission from role: RoleId={RoleId}, PermissionId={PermissionId}", request.RoleId, request.PermissionId);
            throw new AuthException("Permission removal failed", innerException: ex);
        }
    }

    /// <summary>
    /// Get role permissions
    /// </summary>
    public async Task<List<PermissionResponse>> GetRolePermissionsAsync(Guid roleId)
    {
        try
        {
            var permissions = await _authRepository.GetRolePermissionsAsync(roleId);
            return _mapper.Map<List<PermissionResponse>>(permissions);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting role permissions: {RoleId}", roleId);
            throw new AuthException("Failed to get role permissions", innerException: ex);
        }
    }

    /// <summary>
    /// Get roles by permission
    /// </summary>
    public async Task<List<RoleResponse>> GetRolesByPermissionAsync(Guid permissionId)
    {
        try
        {
            var roles = await _authRepository.GetRolesByPermissionAsync(permissionId);
            return _mapper.Map<List<RoleResponse>>(roles);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting roles by permission: {PermissionId}", permissionId);
            throw new AuthException("Failed to get roles by permission", innerException: ex);
        }
    }

    /// <summary>
    /// Check if role has permission
    /// </summary>
    public async Task<bool> RoleHasPermissionAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            return await _authRepository.RoleHasPermissionAsync(roleId, permissionId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking if role has permission: RoleId={RoleId}, PermissionId={PermissionId}", roleId, permissionId);
            throw new AuthException("Failed to check role permission", innerException: ex);
        }
    }

    /// <summary>
    /// Check if role has permission by name
    /// </summary>
    public async Task<bool> RoleHasPermissionAsync(Guid roleId, string permissionName)
    {
        try
        {
            return await _authRepository.RoleHasPermissionAsync(roleId, permissionName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking if role has permission: RoleId={RoleId}, PermissionName={PermissionName}", roleId, permissionName);
            throw new AuthException("Failed to check role permission", innerException: ex);
        }
    }

    #endregion

    #region Authorization Operations

    /// <summary>
    /// Check if account is authorized for specific permission
    /// </summary>
    public async Task<bool> IsAuthorizedAsync(Guid accountId, string permissionName)
    {
        try
        {
            var accountRoles = await _authRepository.GetAccountRolesAsync(accountId);
            
            foreach (var role in accountRoles)
            {
                if (await _authRepository.RoleHasPermissionAsync(role.Id, permissionName))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking authorization: AccountId={AccountId}, Permission={Permission}", accountId, permissionName);
            throw new AuthException("Authorization check failed", innerException: ex);
        }
    }

    /// <summary>
    /// Check if account is authorized for any of the specified permissions
    /// </summary>
    public async Task<bool> IsAuthorizedAsync(Guid accountId, List<string> permissionNames)
    {
        try
        {
            foreach (var permissionName in permissionNames)
            {
                if (await IsAuthorizedAsync(accountId, permissionName))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking authorization: AccountId={AccountId}, Permissions={Permissions}", accountId, string.Join(", ", permissionNames));
            throw new AuthException("Authorization check failed", innerException: ex);
        }
    }

    /// <summary>
    /// Get all permissions for an account
    /// </summary>
    public async Task<List<string>> GetAccountPermissionsAsync(Guid accountId)
    {
        try
        {
            var permissions = new List<string>();
            var accountRoles = await _authRepository.GetAccountRolesAsync(accountId);
            
            foreach (var role in accountRoles)
            {
                var rolePermissions = await _authRepository.GetRolePermissionsAsync(role.Id);
                foreach (var permission in rolePermissions)
                {
                    if (!permissions.Contains(permission.Name))
                    {
                        permissions.Add(permission.Name);
                    }
                }
            }

            return permissions;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting account permissions: {AccountId}", accountId);
            throw new AuthException("Failed to get account permissions", innerException: ex);
        }
    }

    #endregion

    #region Validation Methods

    /// <summary>
    /// Validates login request
    /// </summary>
    private void ValidateLoginRequest(LoginRequest request)
    {
        ValidateRequired(request, nameof(request));
        
        if (string.IsNullOrWhiteSpace(request.EmailOrPhone))
        {
            throw new ValidationException("Email or phone number is required");
        }
        
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Password is required");
        }
    }

    /// <summary>
    /// Validates registration request
    /// </summary>
    private void ValidateRegisterRequest(RegisterRequest request)
    {
        ValidateRequired(request, nameof(request));
        ValidateRequiredString(request.Email, nameof(request.Email));
        ValidateRequiredString(request.Password, nameof(request.Password));
        ValidateRequiredString(request.ConfirmPassword, nameof(request.ConfirmPassword));
        ValidateRequiredString(request.PhoneNumber, nameof(request.PhoneNumber));  

        // Validate password length (DTO already has MinLength(8) attribute, but double-check here)
        if (request.Password.Length < 8)
        {
            throw new AccountValidationException("Password must be at least 8 characters long");
        }

        // Validate password complexity (DTO already has RegularExpression attribute, but double-check here)
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]"))
        {
            throw new AccountValidationException("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character");
        }

        // Validate password confirmation
        if (request.Password != request.ConfirmPassword)
        {
            throw new AccountValidationException("Password and confirm password do not match");
        }

        // Validate address length
        if (string.IsNullOrEmpty(request.Address) || request.Address.Length > 500)
        {
            throw new AccountValidationException("Address is required and must not exceed 500 characters");
        }
    }

    private void ValidateRegisterByRole(RegisterRequest request, Role role)
    {
        var normalizedRole = role;

        // Common password confirmation
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new AccountValidationException("Password and confirm password do not match");
        }

        if (normalizedRole == Role.PATIENT)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new AccountValidationException("FullName is required for Patient");
            if (!request.Gender.HasValue)
                throw new AccountValidationException("Gender is required for Patient");
            if (!request.Birthday.HasValue)
                throw new AccountValidationException("Birthday is required for Patient");
            if (request.Birthday.Value >= DateTime.Today)
                throw new AccountValidationException("Birthday cannot be today or in the future");
            if (request.Birthday.Value < DateTime.Today.AddYears(-120))
                throw new AccountValidationException("Birthday seems invalid (too far in the past)");
        }
        else if (normalizedRole == Role.DOCTOR)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new AccountValidationException("FullName is required for Doctor");
            if (!request.Gender.HasValue)
                throw new AccountValidationException("Gender is required for Doctor");
            if (request.DoctorProfile == null)
                throw new AccountValidationException("DoctorProfile is required for Doctor");
            if (request.DoctorProfile.PositionId == Guid.Empty)
                throw new AccountValidationException("DoctorProfile.PositionId is required");
            if (request.DoctorProfile.SpecialtyId == Guid.Empty)
                throw new AccountValidationException("DoctorProfile.SpecialtyId is required");
            if (request.DoctorProfile.ClinicId == Guid.Empty)
                throw new AccountValidationException("DoctorProfile.ClinicId is required");
            if (string.IsNullOrWhiteSpace(request.DoctorProfile.Bio))
                throw new AccountValidationException("DoctorProfile.Bio is required");
            if (request.DoctorProfile.YearsOfExperience < 0 || request.DoctorProfile.YearsOfExperience > 80)
                throw new AccountValidationException("DoctorProfile.YearsOfExperience must be between 0 and 80");
        }
        else if (normalizedRole == Role.CLINIC)
        {
            if (request.ClinicProfile == null)
                throw new AccountValidationException("ClinicProfile is required for Clinic");
            if (string.IsNullOrWhiteSpace(request.ClinicProfile.Name))
                throw new AccountValidationException("ClinicProfile.Name is required");
            if (string.IsNullOrWhiteSpace(request.ClinicProfile.Description))
                throw new AccountValidationException("ClinicProfile.Description is required");
            if (request.ClinicProfile.Name.Length > 200)
                throw new AccountValidationException("ClinicProfile.Name must not exceed 200 characters");
            if (request.ClinicProfile.Description.Length > 2000)
                throw new AccountValidationException("ClinicProfile.Description must not exceed 2000 characters");
        }
    }

    /// <summary>
    /// Validates account update request
    /// </summary>
    private void ValidateUpdateAccountRequest(UpdateAccountRequest request)
    {
        ValidateRequired(request, nameof(request));
        ValidateGuid(request.Id, nameof(request.Id));
    }

    /// <summary>
    /// Validates role creation request
    /// </summary>
    private void ValidateCreateRoleRequest(CreateRoleRequest request)
    {
        ValidateRequired(request, nameof(request));
        ValidateRequiredString(request.Name, nameof(request.Name));
    }

    /// <summary>
    /// Validates role update request
    /// </summary>
    private void ValidateUpdateRoleRequest(UpdateRoleRequest request)
    {
        ValidateRequired(request, nameof(request));
        ValidateGuid(request.Id, nameof(request.Id));
        ValidateRequiredString(request.Name, nameof(request.Name));
    }

    /// <summary>
    /// Validates permission creation request
    /// </summary>
    private void ValidateCreatePermissionRequest(CreatePermissionRequest request)
    {
        ValidateRequired(request, nameof(request));
        ValidateRequiredString(request.Name, nameof(request.Name));
    }

    /// <summary>
    /// Validates permission update request
    /// </summary>
    private void ValidateUpdatePermissionRequest(UpdatePermissionRequest request)
    {
        ValidateRequired(request, nameof(request));
        ValidateGuid(request.Id, nameof(request.Id));
        ValidateRequiredString(request.Name, nameof(request.Name));
    }

    /// <summary>
    /// Validates change password request
    /// </summary>
    private void ValidateChangePasswordRequest(ChangePasswordRequest request)
    {
        ValidateRequired(request, nameof(request));
        ValidateGuid(request.AccountId, nameof(request.AccountId));
        ValidateRequiredString(request.NewPassword, nameof(request.NewPassword));
        ValidateRequiredString(request.ConfirmNewPassword, nameof(request.ConfirmNewPassword));
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Generate device ID for refresh token tracking
    /// </summary>
    private string GenerateDeviceId(string emailOrPhone)
    {
        // Simple device identification - in production, this should be more sophisticated
        // Could use browser fingerprinting, device UUID, etc.
        var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(emailOrPhone + DateTime.UtcNow.Date.ToString("yyyy-MM-dd")));
        return Convert.ToBase64String(hash)[..16]; // Take first 16 characters
    }

    /// <summary>
    /// Find account by email or phone number
    /// </summary>
    private async Task<AccountEntity?> FindAccountByEmailOrPhoneAsync(string emailOrPhone)
    {
        // Try to find by email first
        var account = await _authRepository.GetAccountByEmailAsync(emailOrPhone);
        if (account != null)
        {
            return account;
        }

        // If not found by email, try to find by phone number
        account = await _authRepository.GetAccountByPhoneNumberAsync(emailOrPhone);
        return account;
    }

    private static string BuildResetUrl(string baseUrl, string email, string token)
    {
        if (string.IsNullOrEmpty(baseUrl))
        {
            return $"/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        }
        var separator = baseUrl.EndsWith('/') ? string.Empty : "/";
        return $"{baseUrl}{separator}reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    private static string GetAppPrefix(string origin)
    {
        if (string.IsNullOrEmpty(origin)) return "default";
        if (origin.Contains("3002")) return "admin";
        if (origin.Contains("3000")) return "client";
        return "default";
    }

    #endregion
}
