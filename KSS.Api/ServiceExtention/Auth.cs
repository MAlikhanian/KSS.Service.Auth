using KSS.Repository.IRepository;
using KSS.Repository.Repository;
using KSS.Service.IService;
using KSS.Service.Service;
using Microsoft.EntityFrameworkCore;
using KSS.Data.DbContexts;

namespace KSS.Api.ServiceExtention
{
    public static class AuthServiceExtention
    {
        public static IServiceCollection AddAuthServiceExtention(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetSection("ConnectionStrings")["KSSAuth"]
                ?? configuration.GetSection("ConnectionStrings")["KSSMain"];

            services.AddDbContext<MainDbContext>(options => options.UseSqlServer(connectionString));

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();

            // Register HttpClient for Person API
            services.AddHttpClient<IPersonApiClient, PersonApiClient>();

            return services;
        }
    }
}
