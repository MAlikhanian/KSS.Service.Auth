using System.Reflection;
using System.Security.Claims;
using KSS.Api.Authorization;
using KSS.Api.ServiceExtention;
using KSS.Helper.Authorization;
using KSS.Helper.CustomAttribute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KSS.Api.Tests
{
    /// <summary>
    /// Shared construction for the authorization tests: action discovery, filter
    /// contexts, principals, and an authorization stack mirroring the one the API
    /// registers.
    /// </summary>
    internal static class AuthorizationHarness
    {
        public const string PermissionClaimType = "permission";

        public static IReadOnlyList<MethodInfo> ActionsOf(Type controller) =>
            controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Where(m => m.GetCustomAttributes(inherit: true).OfType<NonActionAttribute>().Any() == false)
                .OrderBy(m => m.Name, StringComparer.Ordinal)
                .ToList();

        public static string? DeclaredPermission(MethodInfo action)
        {
            var attribute = action
                .GetCustomAttributes(inherit: true)
                .OfType<HasPermissionAttribute>()
                .FirstOrDefault();

            if (attribute?.Policy == null)
                return null;

            return attribute.Policy.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal)
                ? attribute.Policy[HasPermissionAttribute.PolicyPrefix.Length..]
                : attribute.Policy;
        }

        public static bool Has<TAttribute>(MethodInfo action) where TAttribute : Attribute =>
            action.GetCustomAttributes(inherit: true).OfType<TAttribute>().Any();

        public static bool IsAnonymous(MethodInfo action) =>
            action.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any();

        public static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

        /// <summary>Authenticated caller carrying the supplied permission claims and nothing else.</summary>
        public static ClaimsPrincipal Caller(params string[] permissions)
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, "test.caller") };
            claims.AddRange(permissions.Select(p => new Claim(PermissionClaimType, p)));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
        }

        public static AuthorizationFilterContext FilterContext(
            Type controller,
            MethodInfo action,
            ClaimsPrincipal user,
            params IFilterMetadata[] filters)
        {
            var descriptor = new ControllerActionDescriptor
            {
                ActionName = action.Name,
                DisplayName = controller.Name + "." + action.Name,
                MethodInfo = action,
                ControllerName = controller.Name,
                ControllerTypeInfo = controller.GetTypeInfo(),
                Parameters = new List<ParameterDescriptor>()
            };

            var httpContext = new DefaultHttpContext { User = user };
            var actionContext = new ActionContext(httpContext, new RouteData(), descriptor);

            return new AuthorizationFilterContext(actionContext, filters.ToList());
        }

        public static void RunGate(AuthorizationFilterContext context) =>
            new RequireExplicitAuthorizationFilter()
                .OnAuthorizationAsync(context)
                .GetAwaiter()
                .GetResult();

        /// <summary>
        /// Builds the container from the API's own registration entry point and reports
        /// whether the gate can be resolved from it.
        /// </summary>
        public static bool ContainerResolvesGate()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=none;Trusted_Connection=True"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddBaseServiceExtention(configuration);

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            return scope.ServiceProvider.GetService<RequireExplicitAuthorizationFilter>() != null;
        }

        /// <summary>
        /// The policy provider and handler pair the API registers, built in isolation so
        /// a permission decision can be evaluated without an HTTP pipeline.
        /// </summary>
        public static ServiceProvider AuthorizationStack()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddAuthorization();
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            return services.BuildServiceProvider();
        }

        public static async Task<bool> IsGrantedAsync(string permission, ClaimsPrincipal user)
        {
            using var provider = AuthorizationStack();

            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
            var authorization = provider.GetRequiredService<IAuthorizationService>();

            var policy = await policyProvider.GetPolicyAsync(HasPermissionAttribute.PolicyPrefix + permission);
            if (policy == null)
                throw new InvalidOperationException("No policy resolved for " + permission);

            var result = await authorization.AuthorizeAsync(user, resource: null, policy);
            return result.Succeeded;
        }
    }
}
