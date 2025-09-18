using BookingCare.Services.Auth.Models.DTOs;
using System.Text.Json;
using BookingCare.Services.Auth.Utils;

namespace BookingCare.Services.Auth.Providers
{
    /// <summary>
    /// Base class for external authentication providers
    /// </summary>
    public abstract class ExternalAuthProviderBase : IExternalAuthProvider
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogger Logger;
        protected readonly JsonSerializerOptions JsonOptions;

        protected ExternalAuthProviderBase(HttpClient httpClient, ILogger logger)
        {
            HttpClient = httpClient;
            Logger = logger;
            JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new StringToBoolConverter() }
            };
        }

        /// <summary>
        /// Make HTTP GET request and deserialize JSON response
        /// </summary>
        protected async Task<T?> GetJsonAsync<T>(string url, bool logContent = false) where T : class
        {
            try
            {
                Logger.LogInformation("Making request to {Url}", url.Contains("access_token") ? url.Split('&')[0] + "&access_token=***" : url);

                var response = await HttpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogWarning("{Provider} request failed. Status={Status}, Reason={Reason}",
                        Name, response.StatusCode, response.ReasonPhrase);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                if (logContent)
                {
                    Logger.LogInformation("{Provider} response: {Content}", Name, content);
                }

                return JsonSerializer.Deserialize<T>(content, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError(ex, "{Provider} HTTP request failed: {Message}", Name, ex.Message);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogError(ex, "{Provider} request timed out: {Message}", Name, ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Provider} unexpected error: {Message}", Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get configuration value with validation
        /// </summary>
        protected static string GetRequiredConfig(IConfiguration config, string key, string displayName)
        {
            var value = config[key];
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"{displayName} is not configured");
            }
            return value;
        }

        public abstract string Name { get; }
        public abstract Task<UserInfoBase?> VerifyAsync(string accessToken);
    }
}
