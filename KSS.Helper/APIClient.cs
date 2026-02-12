using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Xml;

namespace KSS.Helper
{
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
            try
            {
                var result = await httpClient.PostAsJsonAsync(url, data);

                if (result.IsSuccessStatusCode)
                    return await result.Content.ReadAsAsync<R>();
                else
                    throw new HttpRequestException($"Request failed with status code {result.StatusCode}");
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("An error occurred while making the HTTP request.", ex);
            }
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

            return specificConfiguration["APIBaseUrl"];
        }
    }
}
