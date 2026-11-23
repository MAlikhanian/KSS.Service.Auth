namespace KSS.Repository.IRepository
{
    /// <summary>
    /// A database transaction around several repository writes. Disposing it without CommitAsync rolls every write
    /// back.
    /// </summary>
    public interface IRepositoryTransaction : IAsyncDisposable
    {
        Task CommitAsync();
    }
}
