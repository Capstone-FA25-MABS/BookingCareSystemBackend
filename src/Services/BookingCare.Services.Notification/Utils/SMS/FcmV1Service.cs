using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

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
        _opt = opt.Value ?? throw new ArgumentNullException(nameof(opt));
        _logger = logger;
        
        if (string.IsNullOrEmpty(_opt.ServiceAccountPath))
            throw new ArgumentException("ServiceAccountPath not configured in appsettings.json");

        if (string.IsNullOrEmpty(_opt.ProjectId))
            throw new ArgumentException("ProjectId not configured in appsettings.json");

        // Load GoogleCredential from json file
        _credential = GoogleCredential.FromFile(_opt.ServiceAccountPath)
            .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var token = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        return token;
    }

    public async Task<string> SendDataMessageAsync(string deviceToken, object data)
    {
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
            
            return $"Status: {(int)res.StatusCode}, Body: {body}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FCM message to device {DeviceToken}", deviceToken);
            throw;
        }
    }

    public string NormalizePhone(string input)
    {
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("84")) return "+" + digits;
        if (digits.StartsWith("0")) return "+84" + digits.Substring(1);
        return "+" + digits;
    }
}
