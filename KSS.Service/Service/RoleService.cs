using AutoMapper;
using KSS.Data.DbContexts;
using KSS.Dto;
using KSS.Entity;
using KSS.Helper;
using KSS.Repository.IRepository;
using KSS.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace KSS.Service.Service
{
    /// <summary>
    /// Role service. Role + RolePermission catalogs are read-only (DBA-managed
    /// via migrations). The only editable concern here is UserRole assignment
    /// (admin granting roles to users).
    /// </summary>
    public class RoleService : BaseService<Role, RoleDto, RoleDto, RoleDto>, IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly MainDbContext _dbContext;

        public RoleService(IMapper mapper, IRoleRepository repository, MainDbContext dbContext) : base(mapper, repository)
        {
            _roleRepository = repository;
            _dbContext = dbContext;
        }

        public async Task<List<RoleDto>> GetAllRolesWithPermissionsAsync()
        {
            var roles = await _dbContext.Roles
                .Include(r => r.Translations)
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .OrderBy(r => r.Code)
                .ToListAsync();

            return roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Code = r.Code,
                ModuleId = r.ModuleId,
                Translations = r.Translations.Select(t => new RoleTranslationDto
                {
                    RoleId = t.RoleId,
                    LanguageId = t.LanguageId,
                    Name = t.Name,
                    Description = t.Description,
                }).ToList(),
                Permissions = r.RolePermissions.Select(rp => rp.Permission.Code).ToList(),
            }).ToList();
        }

        public async Task<List<string>> GetUserRoleNamesAsync(Guid userId)
        {
            return await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_dbContext.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Code)
                .ToListAsync();
        }

        public async Task<List<string>> GetUserPermissionNamesAsync(Guid userId)
        {
            return await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_dbContext.Roles,
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
                    (permissionId, p) => p.Code)
                .Distinct()
                .ToListAsync();
        }

        public async Task AssignRolesToUserAsync(AssignRoleRequestDto request)
        {
            var user = await _dbContext.Users.FindAsync(request.UserId);
            if (user == null)
                throw new BusinessRuleException($"User with ID '{request.UserId}' not found");

            // Replace existing UserRole rows for this user.
            var existingRoles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == request.UserId)
                .ToListAsync();
            _dbContext.UserRoles.RemoveRange(existingRoles);

            var userRoles = request.RoleIds.Select(roleId => new UserRole
            {
                UserId = request.UserId,
                RoleId = roleId
            });
            await _dbContext.UserRoles.AddRangeAsync(userRoles);
            await _dbContext.SaveChangesAsync();
        }

    }
}
