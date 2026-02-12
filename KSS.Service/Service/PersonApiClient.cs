using System.Net.Http.Json;
using System.Text.Json;
using KSS.Dto;
using KSS.Service.IService;
using Microsoft.Extensions.Configuration;

namespace KSS.Service.Service
{
    public class PersonApiClient : IPersonApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PersonApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            
            var personApiUrl = _configuration["PersonApi:BaseUrl"] 
                ?? _configuration["PersonApi__BaseUrl"]
                ?? "http://localhost:5055";
            
            _httpClient.BaseAddress = new Uri(personApiUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<PersonDto> CreatePersonAsync(CreatePersonRequestDto request)
        {
            try
            {
                // Send Person data with FirstName and LastName to Person service
                // Person service will handle creating both Person and PersonTranslation
                // Using anonymous object to include FirstName/LastName which Person service will use for PersonTranslation
                var personRequest = new
                {
                    Id = Guid.NewGuid(),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    SexId = request.SexId,
                    PreferredLanguageId = request.PreferredLanguageId, // Persian language ID (1)
                    NationalId = string.IsNullOrEmpty(request.NationalId) ? Guid.NewGuid().ToString("N")[..20] : request.NationalId,
                    DateOfBirth = request.DateOfBirth,
                    BirthCountryId = request.BirthCountryId,
                    BirthRegionId = request.BirthRegionId,
                    BirthCityId = request.BirthCityId,
                    NationalityCountryId = request.NationalityCountryId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Call Person API - Person service will create both Person and PersonTranslation
                // Using AddWithTranslationAsync endpoint which handles FirstName/LastName and creates PersonTranslation automatically
                var personResponse = await _httpClient.PostAsJsonAsync("Api/Person/AddWithTranslationAsync", personRequest);
                
                if (!personResponse.IsSuccessStatusCode)
                {
                    var errorContent = await personResponse.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to create person: {personResponse.StatusCode} - {errorContent}");
                }

                // The API returns ActionResult<Person>, so we need to read the content
                var responseContent = await personResponse.Content.ReadAsStringAsync();
                var createdPerson = System.Text.Json.JsonSerializer.Deserialize<PersonDto>(responseContent, new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                if (createdPerson == null)
                {
                    throw new InvalidOperationException("Failed to deserialize created person");
                }

                return createdPerson;
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error creating person: {ex.Message}", ex);
            }
        }
    }
}
