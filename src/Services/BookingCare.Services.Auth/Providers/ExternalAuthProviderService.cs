using BookingCare.Services.Auth.Models.DTOs;

namespace BookingCare.Services.Auth.Providers;

#region Google Provider

/// <summary>
/// Google OAuth provider implementation
/// </summary>
public class GoogleAuthProvider : ExternalAuthProviderBase
{
    private readonly string _clientId;

    public GoogleAuthProvider(HttpClient httpClient, ILogger<GoogleAuthProvider> logger, IConfiguration config)
        : base(httpClient, logger)
    {
        _clientId = GetRequiredConfig(config, "ExternalAuth:Google:ClientId", "Google ClientId");
    }

    public override string Name => "Google";

    public override async Task<UserInfoBase?> VerifyAsync(string accessToken)
    {
        try
        {
            // Get user info from Google
            var userInfo = await GetJsonAsync<GoogleUserInfoResponse>(
                $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}", 
                logContent: true);

            if (userInfo == null || string.IsNullOrEmpty(userInfo.Id) || string.IsNullOrEmpty(userInfo.Email))
            {
                Logger.LogWarning("Invalid user info received from Google");
                return null;
            }

            // Validate token audience
            var tokenInfo = await GetJsonAsync<GoogleTokenValidationResponse>(
                $"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={accessToken}");

            if (tokenInfo?.Audience != _clientId)
            {
                Logger.LogWarning("Google token audience mismatch. Expected={Expected}, Got={Actual}", 
                    _clientId, tokenInfo?.Audience);
                return null;
            }

            return new GoogleUserInfo
            {
                Sub = userInfo.Id,
                Email = userInfo.Email,
                Name = userInfo.Name ?? string.Empty,
                Picture = userInfo.Picture ?? string.Empty,
                EmailVerified = userInfo.VerifiedEmail
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error verifying Google access token");
            return null;
        }
    }
}

#endregion

#region Facebook Provider

/// <summary>
/// Facebook OAuth provider implementation
/// </summary>
public class FacebookAuthProvider : ExternalAuthProviderBase
{
    private readonly string _appId;
    private readonly string _appSecret;

    public FacebookAuthProvider(HttpClient httpClient, ILogger<FacebookAuthProvider> logger, IConfiguration config)
        : base(httpClient, logger)
    {
        _appId = GetRequiredConfig(config, "ExternalAuth:Facebook:AppId", "Facebook AppId");
        _appSecret = GetRequiredConfig(config, "ExternalAuth:Facebook:AppSecret", "Facebook AppSecret");
    }

    public override string Name => "Facebook";

    public override async Task<UserInfoBase?> VerifyAsync(string accessToken)
    {
        try
        {
            // Verify token
            var verifyResult = await GetJsonAsync<FacebookTokenVerifyResult>(
                $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={_appId}|{_appSecret}", 
                logContent: true);

            if (verifyResult?.Data == null || verifyResult.Data.AppId != _appId || !verifyResult.Data.IsValid)
            {
                Logger.LogWarning("Facebook token verification failed");
                return null;
            }

            // Get user info
            var userInfo = await GetJsonAsync<FacebookUserInfo>(
                $"https://graph.facebook.com/me?fields=id,name,email,picture&access_token={accessToken}", 
                logContent: true);

            if (userInfo == null || string.IsNullOrEmpty(userInfo.Id) || string.IsNullOrEmpty(userInfo.Email))
            {
                Logger.LogWarning("Invalid user info received from Facebook");
                return null;
            }

            return userInfo;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error verifying Facebook access token");
            return null;
        }
    }
}

#endregion

/// <summary>
/// Central service to manage multiple external authentication providers
/// </summary>
public class ExternalAuthProviderService
{
    private readonly Dictionary<string, IExternalAuthProvider> _providers;
    private readonly ILogger<ExternalAuthProviderService> _logger;

    public ExternalAuthProviderService(
        IEnumerable<IExternalAuthProvider> providers,
        ILogger<ExternalAuthProviderService> logger)
    {
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
        
        _logger.LogInformation("Initialized ExternalAuthProviderService with providers: {Providers}", 
            string.Join(", ", _providers.Keys));
    }

    /// <summary>
    /// Verify access token for Google provider
    /// </summary>
    public async Task<GoogleUserInfo?> VerifyGoogleAccessTokenAsync(string accessToken)
    {
        var result = await VerifyAsync("Google", accessToken);
        return result as GoogleUserInfo;
    }

    /// <summary>
    /// Verify access token for Facebook provider
    /// </summary>
    public async Task<FacebookUserInfo?> VerifyFacebookAccessTokenAsync(string accessToken)
    {
        var result = await VerifyAsync("Facebook", accessToken);
        return result as FacebookUserInfo;
    }

    /// <summary>
    /// Verify access token for any supported provider
    /// </summary>
    public async Task<UserInfoBase?> VerifyAsync(string providerName, string accessToken)
    {
        if (!_providers.TryGetValue(providerName, out var provider))
        {
            _logger.LogError("Unsupported provider: {ProviderName}. Available providers: {AvailableProviders}", 
                providerName, string.Join(", ", _providers.Keys));
            throw new InvalidOperationException($"Unsupported provider: {providerName}");
        }

        _logger.LogInformation("Verifying access token for provider: {ProviderName}", providerName);
        return await provider.VerifyAsync(accessToken);
    }

    /// <summary>
    /// Get list of supported provider names
    /// </summary>
    public IReadOnlyCollection<string> GetSupportedProviders()
    {
        return _providers.Keys.ToList().AsReadOnly();
    }
}