using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml;

namespace KSS.Helper
{
    /// <summary>
    /// Thrown when an upstream service responds with a non-2xx status. Carries
    /// the upstream HTTP status and the body's `.message` field (or the raw
    /// body if it isn't JSON) so callers can re-throw as a domain exception —
    /// e.g. a 4xx with `DUPLICATE_NATIONAL_ID` should bubble back to the
    /// browser as a clean 400, not a generic 500.
    /// </summary>
    public class UpstreamApiException : Exception
    {
        public int Status { get; }
        public UpstreamApiException(string message, int status) : base(message)
        {
            Status = status;
        }
    }

    public class APIClient
    {
        private readonly HttpClient httpClient;

        public APIClient()
        {
            HttpClientHandler handler = new() { UseDefaultCredentials = true };

            httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseAddress()),
                Timeout = TimeSpan.FromSeconds(1000)
            };

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public APIClient(string baseUrl)
        {
            HttpClientHandler handler = new() { UseDefaultCredentials = true };

            httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(1000)
            };

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task<T> Get<T>(string url)
        {
            try
            {
                var result = await httpClient.GetAsync(url);

                if (result.IsSuccessStatusCode)
                    return await result.Content.ReadAsAsync<T>();
                else
                    throw new HttpRequestException($"Request failed with status code {result.StatusCode}");
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("An error occurred while making the HTTP request.", ex);
            }
        }
        public async Task<T> Post<T>(string url, T data)
        {
            try
            {
                var result = await httpClient.PostAsJsonAsync(url, data);

                if (result.IsSuccessStatusCode)
                    return await result.Content.ReadAsAsync<T>();
                else
                    throw new HttpRequestException($"Request failed with status code {result.StatusCode}");
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("An error occurred while making the HTTP request.", ex);
            }
        }
        public async Task<R> Post<R, T>(string url, T data)
        {
            HttpResponseMessage result;
            try
            {
                result = await httpClient.PostAsJsonAsync(url, data);
            }
            catch (Exception ex)
            {
                // Transport-level failure (DNS, refused, timeout). Caller falls
                // through to the controller's catch (Exception) → 500.
                throw new HttpRequestException("Failed to reach upstream service.", ex);
            }

            if (result.IsSuccessStatusCode)
                return await result.Content.ReadAsAsync<R>();

            // Upstream returned a status code — surface its `.message` so the
            // caller (e.g. PersonApiClient) can re-throw as BusinessRuleException
            // and the original 4xx code reaches the browser intact.
            var body = await result.Content.ReadAsStringAsync();
            var message = ExtractMessage(body) ?? $"Upstream returned {(int)result.StatusCode}";
            throw new UpstreamApiException(message, (int)result.StatusCode);
        }

        private static string? ExtractMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("message", out var m)
                    && m.ValueKind == JsonValueKind.String)
                {
                    return m.GetString();
                }
            }
            catch
            {
                // Not JSON — fall back to the raw body below.
            }
            return body;
        }
        public async Task<T> Put<T>(string url, T data)
        {
            try
            {
                var result = await httpClient.PutAsJsonAsync(url, data);

                if (result.IsSuccessStatusCode)
                    return await result.Content.ReadAsAsync<T>();
                else
                    throw new HttpRequestException($"Request failed with status code {result.StatusCode}");
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("An error occurred while making the HTTP request.", ex);
            }
        }
        private string BaseAddress()
        {
            ConfigurationBuilder configurationBuilder = new();

            configurationBuilder.AddJsonFile(@"C:\\Application\\NegConfig\\config.json\", optional: false, reloadOnChange: true);

            var specificConfiguration = configurationBuilder.Build();

            return specificConfiguration["APIBaseUrl"]!;
        }
    }
}
