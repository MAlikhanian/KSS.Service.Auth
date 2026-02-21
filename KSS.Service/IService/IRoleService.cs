using KSS.Dto;
using KSS.Entity;

namespace KSS.Service.IService
{
    public interface IRoleService : IBaseService<Role, RoleDto, RoleDto, RoleDto>
    {
        Task<RoleDto> CreateRoleAsync(CreateRoleRequestDto request);
        Task AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds);
        Task AssignRolesToUserAsync(AssignRoleRequestDto request);
        Task RemoveRolesFromUserAsync(Guid userId, List<Guid> roleIds);
        Task<List<RoleDto>> GetAllRolesWithPermissionsAsync();
        Task<List<string>> GetUserRoleNamesAsync(Guid userId);
        Task<List<string>> GetUserPermissionNamesAsync(Guid userId);
    }
}
