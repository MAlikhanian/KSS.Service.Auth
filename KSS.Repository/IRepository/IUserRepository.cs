using KSS.Entity;

namespace KSS.Repository.IRepository
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<List<string>> GetUserRolesAsync(Guid userId);
        Task<List<string>> GetUserPermissionsAsync(Guid userId);
    }
}
