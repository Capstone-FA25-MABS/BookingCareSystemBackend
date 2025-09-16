using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookingCare.Shared.Common.Services;

/// <summary>
/// Service for encrypting and decrypting cookie values for enhanced security
/// </summary>
public class CookieEncryptionService
{
    private readonly byte[] _encryptionKey;
    private readonly ILogger<CookieEncryptionService> _logger;

    public CookieEncryptionService(IConfiguration configuration, ILogger<CookieEncryptionService> logger)
    {
        _logger = logger;
        
        // Get encryption key from configuration or environment variable
        var keyString = configuration["Cookie:EncryptionKey"] 
                       ?? Environment.GetEnvironmentVariable("COOKIE_ENCRYPTION_KEY") 
                       ?? "***";
        
        // Ensure key is exactly 32 bytes for AES-256
        _encryptionKey = DeriveKeyFromString(keyString, 32);
    }

    /// <summary>
    /// Encrypt a string value for secure cookie storage
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>Base64 encoded encrypted string</returns>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            // Write IV to the beginning of the stream
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            var encryptedBytes = msEncrypt.ToArray();
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting cookie value");
            return string.Empty;
        }
    }

    /// <summary>
    /// Decrypt a Base64 encoded encrypted string from cookie
    /// </summary>
    /// <param name="encryptedText">Base64 encoded encrypted string</param>
    /// <returns>Decrypted plain text</returns>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;

            // Extract IV from the beginning of the encrypted data
            var iv = new byte[aes.IV.Length];
            Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting cookie value");
            return string.Empty;
        }
    }

    /// <summary>
    /// Generate encrypted cookie name from plain text name using SHA256 hash
    /// </summary>
    /// <param name="plainName">Plain text cookie name</param>
    /// <returns>Encrypted cookie name with 'bc_' prefix</returns>
    public string GenerateEncryptedCookieName(string plainName)
    {
        // Use a deterministic approach: hash the plain name and use it as the cookie name
        // This ensures the same plain name always generates the same encrypted name
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainName));
        
        // Take first 16 bytes and convert to hex for a shorter, URL-safe cookie name
        var shortHash = Convert.ToHexString(hashBytes).Substring(0, 32);
        return $"bc_{shortHash}"; // 'bc' prefix for BookingCare
    }

    /// <summary>
    /// Check if a cookie name corresponds to a BookingCare authentication cookie
    /// </summary>
    /// <param name="cookieName">Cookie name to check</param>
    /// <returns>True if it's a BookingCare authentication cookie</returns>
    public bool IsBookingCareCookie(string cookieName)
    {
        // Check basic format: 'bc_' + 32 hex characters
        if (!cookieName.StartsWith("bc_") || cookieName.Length != 35)
        {
            return false;
        }

        // Check if the hash part contains only valid hex characters
        var hashPart = cookieName.Substring(3); // Remove 'bc_' prefix
        return hashPart.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }

    /// <summary>
    /// Validate if a cookie name could have been generated by our system for any of the known patterns
    /// </summary>
    /// <param name="cookieName">Cookie name to validate</param>
    /// <param name="prefix">App prefix (admin, client, etc.)</param>
    /// <returns>True if the cookie name matches our patterns</returns>
    public bool IsValidAuthCookieName(string cookieName, string prefix)
    {
        if (!IsBookingCareCookie(cookieName))
        {
            return false;
        }

        // Check against known patterns
        var knownPatterns = new[]
        {
            $"refresh_token_{prefix}",
            $"{prefix}_current_user"
            // Note: access_token patterns are dynamic based on userId, 
            // so they need to be checked separately
        };

        return knownPatterns.Any(pattern => GenerateEncryptedCookieName(pattern) == cookieName);
    }

    /// <summary>
    /// Derive a key of specified length from a string using PBKDF2
    /// </summary>
    /// <param name="keyString">Source string for key derivation</param>
    /// <param name="keyLength">Desired key length in bytes</param>
    /// <returns>Derived key bytes</returns>
    private static byte[] DeriveKeyFromString(string keyString, int keyLength)
    {
        // Use a fixed salt for consistency (in production, consider using a configurable salt)
        var salt = Encoding.UTF8.GetBytes("***");
        
        using var pbkdf2 = new Rfc2898DeriveBytes(keyString, salt, 10000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keyLength);
    }
}
