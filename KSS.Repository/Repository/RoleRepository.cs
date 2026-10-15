using KSS.Entity;
using KSS.Repository.IRepository;
using AuthDbContext = KSS.Data.DbContexts.MainDbContext;

namespace KSS.Repository.Repository
{
    public class RoleRepository : BaseRepository<AuthDbContext, Role>, IRoleRepository
    {
        public RoleRepository(AuthDbContext dbContext) : base(dbContext)
        {
        }
    }
}
