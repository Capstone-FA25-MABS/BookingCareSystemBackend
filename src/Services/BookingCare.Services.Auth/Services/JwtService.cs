using BookingCare.Services.Auth.Models.Entities;
using BookingCare.Services.Auth.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BookingCare.Services.Auth.Services;

/// <summary>
/// Service for JWT token generation and validation with role-based security
/// </summary>
public class JwtService
{
    private readonly IConfiguration _configuration;
    private readonly IAuthRepository _authRepository;
    private readonly ILogger<JwtService> _logger;

    public JwtService(
        IConfiguration configuration,
        IAuthRepository authRepository,
        ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _authRepository = authRepository;
        _logger = logger;
    }

    /// <summary>
    /// Generate JWT access token with roles and permissions
    /// </summary>
    public async Task<string> GenerateAccessTokenAsync(AccountEntity account)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetSecretKey());

            // Get account roles and permissions
            var roles = await _authRepository.GetAccountRolesAsync(account.Id);
            var permissions = await GetAccountPermissionsAsync(account.Id);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Email, account.Email ?? ""),
                new Claim(ClaimTypes.Name,  account.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add roles to claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name ?? ""));
                claims.Add(new Claim("role", role.Name ?? ""));
            }

            // Add permissions to claims
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            // Add custom claims for security
            claims.Add(new Claim("account_status", account.Status.ToString()));
            claims.Add(new Claim("phone_number", account.PhoneNumber ?? ""));
            claims.Add(new Claim("created_at", account.CreatedAt.ToString("O")));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = GetIssuer(),
                Audience = GetAudience(),
                NotBefore = DateTime.UtcNow
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating access token for account: {AccountId}", account.Id);
            throw new InvalidOperationException("Failed to generate access token", ex);
        }
    }

    /// <summary>
    /// Generate secure refresh token
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    

    /// <summary>
    /// Get account permissions for JWT claims
    /// </summary>
    private async Task<List<string>> GetAccountPermissionsAsync(Guid accountId)
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
            _logger.LogError(ex, "Error getting account permissions for JWT: {AccountId}", accountId);
            return new List<string>();
        }
    }

    /// <summary>
    /// Get JWT secret key from configuration
    /// </summary>
    private string GetSecretKey()
    {
        var secretKey = _configuration["Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured");
        }

        if (secretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long for security");
        }

        return secretKey;
    }

    /// <summary>
    /// Get JWT issuer from configuration
    /// </summary>
    private string GetIssuer()
    {
        return _configuration["Jwt:Issuer"] ?? "BookingCare.Auth";
    }

    /// <summary>
    /// Get JWT audience from configuration
    /// </summary>
    private string GetAudience()
    {
        return _configuration["Jwt:Audience"] ?? "BookingCare.API";
    }

    /// <summary>
    /// Get access token expiration time in minutes
    /// </summary>
    private int GetAccessTokenExpirationMinutes()
    {
        if (int.TryParse(_configuration["Jwt:AccessTokenExpirationMinutes"], out int minutes))
        {
            return Math.Max(minutes, 5); // Minimum 5 minutes
        }
        return 15; // Default 15 minutes
    }

    /// <summary>
    /// Get refresh token expiration time in days
    /// </summary>
    public int GetRefreshTokenExpirationDays()
    {
        if (int.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out int days))
        {
            return Math.Max(days, 1); // Minimum 1 day
        }
        return 7; // Default 7 days
    }
}
