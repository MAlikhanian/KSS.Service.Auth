using KSS.Dto;

namespace KSS.Service.IService
{
    public interface IPersonApiClient
    {
        Task<PersonDto> CreatePersonAsync(CreatePersonRequestDto request);
    }
}
