using KSS.Dto;
using KSS.Entity;
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
            var connectionString = configuration.GetSection("ConnectionStrings")["DefaultConnection"];

            services.AddDbContext<MainDbContext>(options => options.UseSqlServer(connectionString));

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();

            // Role and Permission repositories and services
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IBaseRepository<Permission>, PermissionRepository>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IPermissionService, PermissionService>();
            // PermissionController has its own dedicated read-only API; no BaseService<Permission> binding needed.

            // Register Person API client (uses APIClient helper internally)
            services.AddScoped<IPersonApiClient, PersonApiClient>();

            // Resolves the signup hostname to its Company from TENANT_HOSTS.
            services.AddSingleton<ITenantHostResolver, TenantHostResolver>();

            // Register Captcha service client (verifies altcha payloads against KSS.Service.Captcha)
            services.AddScoped<ICaptchaClient, CaptchaClient>();

            return services;
        }
    }
}
