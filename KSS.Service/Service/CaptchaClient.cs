using System.Net.Http.Json;
using KSS.Service.IService;
using Microsoft.Extensions.Configuration;

namespace KSS.Service.Service
{
    public class CaptchaClient : ICaptchaClient
    {
        private readonly HttpClient _httpClient;

        public CaptchaClient(IConfiguration configuration)
        {
            var baseUrl = configuration["CaptchaApi:BaseUrl"]
                ?? configuration["CaptchaApi__BaseUrl"]
                ?? "http://localhost:8000";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<bool> VerifyAsync(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return false;

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "Api/Challenge/Verify",
                    new { payload });

                if (!response.IsSuccessStatusCode)
                    return false;

                var result = await response.Content.ReadFromJsonAsync<VerifyResult>();
                return result?.Valid == true;
            }
            catch
            {
                return false;
            }
        }

        private sealed class VerifyResult
        {
            public bool Valid { get; set; }
        }
    }
}
