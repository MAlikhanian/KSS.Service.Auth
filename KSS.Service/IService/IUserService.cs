using KSS.Dto;
using KSS.Entity;

namespace KSS.Service.IService
{
    public interface IUserService : IBaseService<User, UserDto, UserDto, UserDto>
    {
        /// <summary>
        /// Registers a user. <paramref name="tenantCompanyId"/> is the company bound
        /// to the hostname the request arrived on, resolved SERVER-SIDE by the
        /// controller — never taken from the request body. Null means no tenant.
        /// </summary>
        Task<UserDto> RegisterAsync(RegisterRequestDto request, Guid? tenantCompanyId = null);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string jwtSecret);
        Task<UserDto?> GetByUsernameAsync(string username);
        Task<UserDto?> GetByEmailAsync(string email);
        Task<UserDto?> GetByPersonIdAsync(Guid personId);

        /// <summary>
        /// Bulk lookup: for the given PersonIds, return a map PersonId → UserId
        /// for every input that has a User row. Used by dashboard report
        /// endpoints in Company and Person services.
        /// </summary>
        Task<IDictionary<Guid, Guid>> MapPersonsToUsersAsync(IEnumerable<Guid> personIds);

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
