using System.Net.Http.Json;
using KSS.Service.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KSS.Service.Service
{
    public class CaptchaClient : ICaptchaClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CaptchaClient> _logger;

        public CaptchaClient(IConfiguration configuration, ILogger<CaptchaClient> logger)
        {
            var baseUrl = configuration["CaptchaApi:BaseUrl"]
                ?? configuration["CaptchaApi__BaseUrl"]
                ?? "http://localhost:8000";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
            _logger = logger;
        }

        /// <summary>
        /// For tests: the HTTP handler is supplied. The container never selects this constructor,
        /// because neither HttpMessageHandler nor Uri is registered as a service.
        /// </summary>
        public CaptchaClient(HttpMessageHandler handler, Uri baseAddress, ILogger<CaptchaClient> logger)
        {
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromSeconds(10)
            };
            _logger = logger;
        }

        // Every rejection writes one warning naming its reason. The payload is never logged, and
        // neither is an exception message (it can echo request data); only the exception type is.
        public async Task<bool> VerifyAsync(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                _logger.LogWarning("Captcha verification failed: reason=empty-payload");
                return false;
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "Api/Challenge/Verify",
                    new { payload });

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Captcha verification failed: reason=http-status status={Status}",
                        (int)response.StatusCode);
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<VerifyResult>();
                if (result?.Valid == true)
                    return true;

                _logger.LogWarning("Captcha verification failed: reason=invalid");
                return false;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning("Captcha verification failed: reason=timeout after={Seconds}s",
                    (int)_httpClient.Timeout.TotalSeconds);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Captcha verification failed: reason=exception type={Type}", ex.GetType().Name);
                return false;
            }
        }

        private sealed class VerifyResult
        {
            public bool Valid { get; set; }
        }
    }
}
