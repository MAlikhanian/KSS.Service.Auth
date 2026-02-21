using KSS.Entity;
using KSS.Repository.IRepository;
using AuthDbContext = KSS.Data.DbContexts.MainDbContext;

namespace KSS.Repository.Repository
{
    public class PermissionRepository : BaseRepository<AuthDbContext, Permission>, IPermissionRepository
    {
        public PermissionRepository(AuthDbContext dbContext) : base(dbContext)
        {
        }
    }
}
