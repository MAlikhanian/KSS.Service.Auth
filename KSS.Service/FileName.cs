using KSS.Data.DbContexts;

namespace KSS.Service
{
    public class UnitOfWorkERP
    {
        private readonly MainDbContext _mainDbContext;

        public UnitOfWorkERP(MainDbContext mainDbContext) => _mainDbContext = mainDbContext;

        public async Task<bool> Complete()
        {
            return await _mainDbContext.SaveChangesAsync() > 0;
        }

        public bool HasChanges()
        {
            return _mainDbContext.ChangeTracker.HasChanges();
        }
    }
}
