using AutoMapper;
using KSS.Data.DbContexts;
using KSS.Entity;
using KSS.Dto;
using KSS.Repository.IRepository;
using KSS.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace KSS.Service.Service
{
    /// <summary>
    /// Read-only Permission service. Permission data is critical and only
    /// changed via database migrations — there are no Add/Update/Delete paths
    /// exposed.
    /// ToListAsync includes translations and sorts by Code (Module.Resource.Action),
    /// which gives Module → Resource → Action ordering naturally.
    /// </summary>
    public class PermissionService
        : BaseService<Permission, PermissionDto, PermissionDto, PermissionDto>,
          IPermissionService
    {
        private readonly MainDbContext _dbContext;

        public PermissionService(IMapper mapper, IPermissionRepository repository, MainDbContext dbContext)
            : base(mapper, repository)
        {
            _dbContext = dbContext;
        }

        public override async Task<IEnumerable<Permission>> ToListAsync()
        {
            return await _dbContext.Permissions
                .AsNoTracking()
                .Include(p => p.Translations)
                .OrderBy(p => p.Code)
                .ToListAsync();
        }
    }
}
