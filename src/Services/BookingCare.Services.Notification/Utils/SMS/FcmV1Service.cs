using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using BookingCare.Services.Notification.Exceptions;

namespace BookingCare.Services.Notification.Utils.SMS;

public class FcmV1Service
{
    private readonly IHttpClientFactory _http;
    private readonly FcmOptions _opt;
    private readonly GoogleCredential _credential;
    private readonly ILogger<FcmV1Service> _logger;

    public FcmV1Service(IHttpClientFactory http, IOptions<FcmOptions> opt, ILogger<FcmV1Service> logger)
    {
        _http = http;
        _opt = opt.Value ?? throw new FcmConfigurationException($"FCM options configuration is null: {nameof(opt)}");
        _logger = logger;

        if (string.IsNullOrEmpty(_opt.ServiceAccountPath))
            throw new FcmConfigurationException("ServiceAccountPath not configured in appsettings.json");

        if (string.IsNullOrEmpty(_opt.ProjectId))
            throw new FcmConfigurationException("ProjectId not configured in appsettings.json");

        // Load GoogleCredential from json file
        try
        {
            _credential = GoogleCredential.FromFile(_opt.ServiceAccountPath)
                .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        }
        catch (Exception ex)
        {
            throw new FcmConfigurationException($"Failed to load FCM service account file from '{_opt.ServiceAccountPath}'", ex);
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        try
        {
            var token = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            return token;
        }
        catch (Exception ex)
        {
            throw new FcmAuthenticationException("Failed to obtain FCM access token", ex);
        }
    }

    public async Task<string> SendDataMessageAsync(string deviceToken, object data)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            throw new FcmTokenValidationException(deviceToken, "FCM token cannot be null or empty");
        }

        try
        {
            var accessToken = await GetAccessTokenAsync();
            var url = $"https://fcm.googleapis.com/v1/projects/{_opt.ProjectId}/messages:send";

            var message = new
            {
                message = new
                {
                    token = deviceToken,
                    data,
                    android = new { priority = "HIGH" }
                }
            };

            var json = JsonSerializer.Serialize(message);
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var client = _http.CreateClient();
            var res = await client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            _logger.LogInformation("FCM response: Status: {StatusCode}, Body: {Body}", res.StatusCode, body);

            // Handle different HTTP response codes
            if (!res.IsSuccessStatusCode)
            {
                switch (res.StatusCode)
                {
                    case System.Net.HttpStatusCode.BadRequest:
                        throw new FcmDeliveryException($"FCM request failed with bad request: {body}", deviceToken);
                    case System.Net.HttpStatusCode.Unauthorized:
                        throw new FcmAuthenticationException($"FCM authentication failed: {body}");
                    case System.Net.HttpStatusCode.TooManyRequests:
                        throw new FcmQuotaExceededException($"FCM quota exceeded: {body}");
                    case System.Net.HttpStatusCode.ServiceUnavailable:
                        throw new FcmServiceUnavailableException($"FCM service unavailable: {body}");
                    default:
                        throw new FcmDeliveryException($"FCM request failed with status {res.StatusCode}: {body}", deviceToken);
                }
            }

            return $"Status: {(int)res.StatusCode}, Body: {body}";
        }
        catch (FcmException)
        {
            // Re-throw FCM specific exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending FCM message to device {DeviceToken}", deviceToken);
            throw new FcmException($"Unexpected error sending FCM message to device {deviceToken}", "FCM_UNEXPECTED_ERROR", System.Net.HttpStatusCode.InternalServerError, ex);
        }
    }

    public static string NormalizePhone(string input)
    {
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("84")) return "+" + digits;
        if (digits.StartsWith('0')) return "+84" + digits.Substring(1);
        return "+" + digits;
    }
}