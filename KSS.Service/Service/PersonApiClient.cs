using KSS.Dto;
using KSS.Helper;
using KSS.Service.IService;
using Microsoft.Extensions.Configuration;

namespace KSS.Service.Service
{
    public class PersonApiClient : IPersonApiClient
    {
        private readonly APIClient _apiClient;

        public PersonApiClient(IConfiguration configuration)
        {
            var personApiUrl = configuration["PersonApi:BaseUrl"]
                ?? configuration["PersonApi__BaseUrl"]
                ?? "http://localhost:5055";

            _apiClient = new APIClient(personApiUrl);
        }

        public async Task<PersonDto> CreatePersonAsync(CreatePersonRequestDto request)
        {
            var payload = new
            {
                request.FirstName,
                request.LastName,
                request.SexId,
                request.PreferredLanguageId,
                NationalId = request.NationalId,
                request.DateOfBirth,
                request.BirthCountryId,
                request.BirthRegionId,
                request.BirthCityId,
                request.NationalityCountryId
            };

            return await _apiClient.Post<PersonDto, object>("Api/Person/AddWithTranslation", payload);
        }
    }
}
