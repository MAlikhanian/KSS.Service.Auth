using KSS.Dto;

namespace KSS.Service.IService
{
    public interface IPersonApiClient
    {
        /// <summary>
        /// Creates the Person for a new user. When <paramref name="tenantCompanyId"/>
        /// is supplied it is sent as X-Company-Id, which makes Person's existing
        /// auto-assign create the CompanyPerson membership. Null means no tenant —
        /// no membership row, today's behaviour.
        /// </summary>
        Task<PersonDto> CreatePersonAsync(CreatePersonRequestDto request, Guid? tenantCompanyId = null);
    }
}
