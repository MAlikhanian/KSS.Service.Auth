using KSS.Helper;

namespace KSS.Api.ServiceExtention
{
    public static class BaseServiceExtention
    {
        public static IServiceCollection AddBaseServiceExtention(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddAuthServiceExtention(configuration);

            serviceCollection.AddScoped<GlobalProperty>();

            return serviceCollection;
        }
    }
}
