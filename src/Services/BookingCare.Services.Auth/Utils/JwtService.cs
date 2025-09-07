using BookingCare.Services.Auth.Models.Entities;
using BookingCare.Services.Auth.Repositories;
using BookingCare.Shared.Common.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BookingCare.Services.Auth.Utils;

/// <summary>
/// Service for JWT token generation and validation with role-based security
/// </summary>
public class JwtService : BaseService
{
    private readonly IConfiguration _configuration;
    private readonly IAuthRepository _authRepository;

    public JwtService(
        IConfiguration configuration,
        IAuthRepository authRepository,
        ILogger<JwtService> logger) : base(logger)
    {
        _configuration = configuration;
        _authRepository = authRepository;
    }

    /// <summary>
    /// Generate JWT access token with roles and permissions
    /// </summary>
    public async Task<string> GenerateAccessTokenAsync(AccountEntity account)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Generating access token for account: {AccountId}", null, account.Id);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetSecretKey());

            // Get account roles and permissions
            var roles = await _authRepository.GetAccountRolesAsync(account);
            var permissions = await _authRepository.GetAccountPermissionsAsync(account);

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
            var tokenString = tokenHandler.WriteToken(token);

            LogInfo("Access token generated successfully for account: {AccountId}", null, account.Id);
            return tokenString;
        }, "GenerateAccessToken");
    }

    /// <summary>
    /// Get JWT secret key from configuration
    /// </summary>
    private string GetSecretKey()
    {
        var secretKey = _configuration["Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            LogError(new InvalidOperationException("JWT SecretKey is not configured"), "JWT SecretKey is not configured");
            throw new InvalidOperationException("JWT SecretKey is not configured");
        }

        if (secretKey.Length < 32)
        {
            LogError(new InvalidOperationException("JWT SecretKey must be at least 32 characters long for security"), "JWT SecretKey must be at least 32 characters long for security");
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

}
