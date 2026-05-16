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
        Task AssignRolesToUserAsync(AssignRoleRequestDto request);
    }
}
