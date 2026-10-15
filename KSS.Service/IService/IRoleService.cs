using KSS.Dto;
using KSS.Entity;

namespace KSS.Service.IService
{
    /// <summary>
    /// Role service. The catalog itself (Role + RolePermission) is read-only —
    /// maintained via DB migrations. Only UserRole assignments stay editable.
    /// </summary>
    public interface IRoleService : IBaseService<Role, RoleDto, RoleDto, RoleDto>
    {
        Task<List<RoleDto>> GetAllRolesWithPermissionsAsync();
        Task<List<string>> GetUserRoleNamesAsync(Guid userId);
        Task<List<string>> GetUserPermissionNamesAsync(Guid userId);
        /// <summary>
        /// Replaces the target's role assignments, after checking through
        /// IAccountAdministrationGuard that the caller may do so.
        /// </summary>
        Task AssignRolesToUserAsync(Guid callerUserId, AssignRoleRequestDto request);
    }
}
