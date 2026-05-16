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
        Task<UserDto?> GetByPersonIdAsync(Guid personId);

        // Security management — used by the /person/security page.
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
        Task AdminResetPasswordAsync(Guid userId, string newPassword);
        Task LockAsync(Guid userId, int lockMinutes);
        Task UnlockAsync(Guid userId);
        Task MarkEmailVerifiedAsync(Guid userId);
        Task MarkPhoneVerifiedAsync(Guid userId);
        Task SetActiveAsync(Guid userId, bool isActive);
        Task RevokeSessionsAsync(Guid userId);
    }
}
