using KSS.Helper;
using KSS.Helper.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace KSS.Api.ServiceExtention
{
    public static class BaseServiceExtention
    {
        public static IServiceCollection AddBaseServiceExtention(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddAuthServiceExtention(configuration);

            serviceCollection.AddScoped<GlobalProperty>();

            // Permission-based authorization
            serviceCollection.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            serviceCollection.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            serviceCollection.AddScoped<PermissionAuthorizationFilter>();

            return serviceCollection;
        }
    }
}
