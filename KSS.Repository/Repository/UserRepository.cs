using KSS.Entity;
using KSS.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using AuthDbContext = KSS.Data.DbContexts.MainDbContext;

namespace KSS.Repository.Repository
{
    public class UserRepository : BaseRepository<AuthDbContext, User>, IUserRepository
    {
        private readonly AuthDbContext _dbContext;

        public UserRepository(AuthDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<string>> GetUserRolesAsync(Guid userId)
        {
            return await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_dbContext.Roles.Where(r => r.IsActive),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name)
                .ToListAsync();
        }

        public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
        {
            return await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_dbContext.Roles.Where(r => r.IsActive),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Id)
                .Join(_dbContext.RolePermissions,
                    roleId => roleId,
                    rp => rp.RoleId,
                    (roleId, rp) => rp.PermissionId)
                .Join(_dbContext.Permissions,
                    permissionId => permissionId,
                    p => p.Id,
                    (permissionId, p) => p.Name)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>
        /// Assign the default "User" role to a newly registered user.
        /// </summary>
        public async Task AssignDefaultRoleAsync(Guid userId)
        {
            var userRole = await _dbContext.Roles
                .Where(r => r.Name == "User" && r.IsActive)
                .FirstOrDefaultAsync();

            if (userRole == null) return; // Role not seeded yet — skip silently

            var alreadyAssigned = await _dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == userRole.Id);

            if (!alreadyAssigned)
            {
                _dbContext.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = userRole.Id,
                    AssignedAt = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
