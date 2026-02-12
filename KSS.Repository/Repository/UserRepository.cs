using KSS.Entity;
using KSS.Repository.IRepository;
using AuthDbContext = KSS.Data.DbContexts.MainDbContext;

namespace KSS.Repository.Repository
{
    public class UserRepository : BaseRepository<AuthDbContext, User>, IUserRepository
    {
        public UserRepository(AuthDbContext dbContext) : base(dbContext)
        {
        }
    }
}
