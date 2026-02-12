namespace KSS.Data.Configuration
{
    public class BaseConfiguration<T> where T : class
    {
        public static IQueryable<T> Navigation(IQueryable<T> queryable)
        {
            IQueryable<T>? result = queryable;

            if (result == null)
                return queryable;

            return result;
        }
    }
}