using AutoMapper;
using KSS.Data.DbContexts;
using KSS.Dto;
using KSS.Entity;
using KSS.Repository.IRepository;
using KSS.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace KSS.Service.Service
{
    public class RoleService : BaseService<Role, RoleDto, RoleDto, RoleDto>, IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly MainDbContext _dbContext;

        public RoleService(IMapper mapper, IRoleRepository repository, MainDbContext dbContext) : base(mapper, repository)
        {
            _roleRepository = repository;
            _dbContext = dbContext;
        }

        public async Task<RoleDto> CreateRoleAsync(CreateRoleRequestDto request)
        {
            // Check if role name already exists
            var existingRole = await _roleRepository.SingleOrDefaultAsync(r => r.Name == request.Name);
            if (existingRole != null)
                throw new InvalidOperationException($"Role '{request.Name}' already exists");

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _roleRepository.AddAsync(role);

            // Assign permissions if provided
            if (request.PermissionIds.Any())
            {
                await AssignPermissionsToRoleAsync(role.Id, request.PermissionIds);
            }

            return await GetRoleWithPermissionsAsync(role.Id);
        }

        public async Task AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds)
        {
            // Remove existing permissions
            var existingPermissions = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();
            _dbContext.RolePermissions.RemoveRange(existingPermissions);

            // Add new permissions
            var rolePermissions = permissionIds.Select(permissionId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                AssignedAt = DateTime.UtcNow
            });

            await _dbContext.RolePermissions.AddRangeAsync(rolePermissions);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AssignRolesToUserAsync(AssignRoleRequestDto request)
        {
            // Check user exists
            var user = await _dbContext.Users.FindAsync(request.UserId);
            if (user == null)
                throw new ArgumentException($"User with ID '{request.UserId}' not found");

            // Remove existing roles
            var existingRoles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == request.UserId)
                .ToListAsync();
            _dbContext.UserRoles.RemoveRange(existingRoles);

            // Assign new roles
            var userRoles = request.RoleIds.Select(roleId => new UserRole
            {
                UserId = request.UserId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });

            await _dbContext.UserRoles.AddRangeAsync(userRoles);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveRolesFromUserAsync(Guid userId, List<Guid> roleIds)
        {
            var userRoles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
                .ToListAsync();
            _dbContext.UserRoles.RemoveRange(userRoles);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<RoleDto>> GetAllRolesWithPermissionsAsync()
        {
            var roles = await _dbContext.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Where(r => r.IsActive)
                .ToListAsync();

            return roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsActive = r.IsActive,
                Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList()
            }).ToList();
        }

        public async Task<List<string>> GetUserRoleNamesAsync(Guid userId)
        {
            return await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_dbContext.Roles.Where(r => r.IsActive),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name)
                .ToListAsync();
        }

        public async Task<List<string>> GetUserPermissionNamesAsync(Guid userId)
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

        private async Task<RoleDto> GetRoleWithPermissionsAsync(Guid roleId)
        {
            var role = await _dbContext.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstAsync(r => r.Id == roleId);

            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                Permissions = role.RolePermissions.Select(rp => rp.Permission.Name).ToList()
            };
        }
    }
}
