using KSS.Dto;
using KSS.Entity;

namespace KSS.Service.IService
{
    public interface IUserService : IBaseService<User, UserDto, UserDto, UserDto>
    {
        Task<UserDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string jwtSecret);
        Task<UserDto?> GetByUsernameAsync(string username);
        Task<UserDto?> GetByEmailAsync(string email);
    }
}
