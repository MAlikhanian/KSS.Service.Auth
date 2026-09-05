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

        public async Task<PersonDto> CreatePersonAsync(CreatePersonRequestDto request, Guid? tenantCompanyId = null)
        {
            // Person's CreatePersonWithTranslationDto expects names inside a
            // Translations array, not flat fields. The signup form collects a
            // single first/last name pair — write it under the caller's
            // preferred language so the name renders in that locale, and let
            // the user fill the other language from the profile page.
            var payload = new
            {
                request.SexId,
                request.PreferredLanguageId,
                request.NationalId,
                request.DateOfBirth,
                request.BirthCountryId,
                request.BirthRegionId,
                request.BirthCityId,
                Translations = new[]
                {
                    new
                    {
                        LanguageId = request.PreferredLanguageId,
                        request.FirstName,
                        request.LastName,
                        FatherName = (string?)null,
                    },
                },
            };

            try
            {
                // Person's PersonService assigns the new person to X-Company-Id when
                // present. Sent ONLY here, and only for a host Auth itself resolved.
                var headers = tenantCompanyId is Guid companyId
                    ? new Dictionary<string, string> { ["X-Company-Id"] = companyId.ToString() }
                    : null;

                return await _apiClient.Post<PersonDto, object>("Api/Person/AddWithTranslation", payload, headers);
            }
            catch (UpstreamApiException ex) when (ex.Status >= 400 && ex.Status < 500)
            {
                // Person returned a domain error (e.g. DUPLICATE_NATIONAL_ID).
                // Re-throw as BusinessRuleException so UserController turns it
                // into the same clean 400 + code the rest of signup uses.
                throw new BusinessRuleException(ex.Message);
            }
        }
    }
}
