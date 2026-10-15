using System.Reflection;
using KSS.Helper.CustomAttribute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KSS.Api.Authorization
{
    /// <summary>
    /// Requires every action on the controller this filter is applied to to state its
    /// own authorization. An action states it with exactly one of:
    ///   [AllowAnonymous]           reachable without authentication
    ///   [HasPermission("Code")]    requires the named permission claim
    ///   [CallerScoped]             acts only on the authenticated caller
    ///   [AuthenticatedOnly("why")] any authenticated caller, reason recorded
    ///
    /// An action stating none of these is denied, as is an action stating more than
    /// one, and as is a controller-wide anonymous declaration combined with an
    /// action-level requirement. Permission codes themselves are evaluated by the
    /// framework's authorization pipeline; this filter checks only that a statement
    /// was made.
    /// </summary>
    public class RequireExplicitAuthorizationFilter : IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
            {
                context.Result = new ForbidResult();
                return Task.CompletedTask;
            }

            var method = descriptor.MethodInfo;
            var controller = descriptor.ControllerTypeInfo;

            var anonymousOnController = controller
                .GetCustomAttributes(inherit: true)
                .OfType<IAllowAnonymous>()
                .Any();

            var hasPermission =
                method.GetCustomAttributes(inherit: true).OfType<HasPermissionAttribute>().Any() ||
                controller.GetCustomAttributes(inherit: true).OfType<HasPermissionAttribute>().Any();

            var callerScoped = method
                .GetCustomAttributes(inherit: true)
                .OfType<CallerScopedAttribute>()
                .Any();

            var authenticatedOnly = method
                .GetCustomAttributes(inherit: true)
                .OfType<AuthenticatedOnlyAttribute>()
                .Any();

            var statements =
                (hasPermission ? 1 : 0) +
                (callerScoped ? 1 : 0) +
                (authenticatedOnly ? 1 : 0);

            // A controller-wide anonymous declaration takes precedence over anything an
            // action states, so the combination is rejected rather than resolved silently.
            if (anonymousOnController && statements > 0)
            {
                context.Result = new ForbidResult();
                return Task.CompletedTask;
            }

            if (IsAnonymous(context, method))
            {
                // Anonymous access alongside a stated requirement is contradictory.
                if (statements > 0)
                    context.Result = new ForbidResult();

                return Task.CompletedTask;
            }

            if (statements != 1)
            {
                context.Result = new ForbidResult();
                return Task.CompletedTask;
            }

            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
                context.Result = new ChallengeResult();

            return Task.CompletedTask;
        }

        // Mirrors how the framework's own authorization stages locate an anonymous
        // declaration: attributes on the action, the MVC filter collection, and the
        // matched endpoint's metadata.
        private static bool IsAnonymous(AuthorizationFilterContext context, MethodInfo method)
        {
            if (method.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any())
                return true;

            if (context.Filters.OfType<IAllowAnonymousFilter>().Any())
                return true;

            return context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null;
        }
    }
}
