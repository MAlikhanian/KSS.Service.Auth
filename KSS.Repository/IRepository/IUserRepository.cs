using KSS.Entity;

namespace KSS.Repository.IRepository
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<List<string>> GetUserRolesAsync(Guid userId);
        Task<List<Guid>> GetUserRoleIdsAsync(Guid userId);
        Task<List<string>> GetUserPermissionsAsync(Guid userId);
        Task AssignDefaultRoleAsync(Guid userId);

        /// <summary>Starts the transaction self-registration writes its user and role inside.</summary>
        Task<IRepositoryTransaction> BeginRegistrationTransactionAsync();
    }
}
