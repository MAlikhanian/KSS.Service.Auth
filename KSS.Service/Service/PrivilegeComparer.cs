using KSS.Data.DbContexts;
using KSS.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace KSS.Service.Service
{
    /// <summary>
    /// Live permission-set reads for privilege comparison. See
    /// <see cref="IPrivilegeComparer"/> for the definition of a user's set.
    /// </summary>
    public class PrivilegeComparer : IPrivilegeComparer
    {
        private readonly MainDbContext _dbContext;

        public PrivilegeComparer(MainDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(Guid userId)
        {
            var codes = await (
                    from userRole in _dbContext.UserRoles
                    where userRole.UserId == userId && userRole.DeletedAt == null
                    join rolePermission in _dbContext.RolePermissions
                        on userRole.RoleId equals rolePermission.RoleId
                    where rolePermission.DeletedAt == null
                    join permission in _dbContext.Permissions
                        on rolePermission.PermissionId equals permission.Id
                    select permission.Code)
                .Distinct()
                .ToListAsync();

            return new HashSet<string>(codes, StringComparer.Ordinal);
        }

        // An active account is IsActive AND not soft-deleted; the two fields are
        // independent, so a soft-deleted account can still carry IsActive = true.
        public Task<bool> IsActiveUserAsync(Guid userId)
            => _dbContext.Users.AnyAsync(user => user.Id == userId && user.IsActive && user.DeletedAt == null);

        public async Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetRolePermissionsAsync(IEnumerable<Guid> roleIds)
        {
            var requested = roleIds.Distinct().ToList();

            var existing = await _dbContext.Roles
                .Where(role => requested.Contains(role.Id))
                .Select(role => role.Id)
                .ToListAsync();

            var rows = await (
                    from rolePermission in _dbContext.RolePermissions
                    where requested.Contains(rolePermission.RoleId) && rolePermission.DeletedAt == null
                    join permission in _dbContext.Permissions
                        on rolePermission.PermissionId equals permission.Id
                    select new { rolePermission.RoleId, permission.Code })
                .ToListAsync();

            var result = new Dictionary<Guid, IReadOnlySet<string>>();
            foreach (var roleId in existing)
            {
                result[roleId] = new HashSet<string>(
                    rows.Where(row => row.RoleId == roleId).Select(row => row.Code),
                    StringComparer.Ordinal);
            }

            return result;
        }

        public async Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetActiveUserPermissionSetsAsync()
        {
            var rows = await (
                    from user in _dbContext.Users
                    where user.IsActive && user.DeletedAt == null
                    join userRole in _dbContext.UserRoles
                        on user.Id equals userRole.UserId
                    where userRole.DeletedAt == null
                    join rolePermission in _dbContext.RolePermissions
                        on userRole.RoleId equals rolePermission.RoleId
                    where rolePermission.DeletedAt == null
                    join permission in _dbContext.Permissions
                        on rolePermission.PermissionId equals permission.Id
                    select new { UserId = user.Id, permission.Code })
                .Distinct()
                .ToListAsync();

            return rows
                .GroupBy(row => row.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlySet<string>)new HashSet<string>(group.Select(row => row.Code), StringComparer.Ordinal));
        }
    }
}
