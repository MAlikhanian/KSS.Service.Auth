using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using KSS.Helper.CustomAttribute;

namespace KSS.Helper.Authorization
{
    /// <summary>
    /// Authorization filter that automatically enforces CRUD permissions
    /// based on the controller's [PermissionGroup] and the action being invoked.
    ///
    /// Action name mapping:
    ///   Read   → FindAsync, SingleAsync, ToListAllAsync, ToListAsync, ToListByFilterAsync, ToListDtoAsync
    ///   (Custom actions like FindByCompanyIdAsync, FindByPersonIdAsync skip CRUD permission — only require [Authorize])
    ///   Create → AddAsync, AddDtoAsync, AddRangeAsync
    ///   Update → Update, UpdateDto, UpdateRange
    ///   Delete → Remove, RemoveRange
    /// </summary>
    public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly IAuthorizationService _authorizationService;

        // Map action names to section-level operations (Read or Manage).
        // Section-level permissions: <Module>.<Section>.<Read|Manage>.
        private static readonly Dictionary<string, string> ActionToOperation = new(StringComparer.OrdinalIgnoreCase)
        {
            // Read operations
            { "Find",                "Read" }, { "FindAsync",            "Read" },
            { "Single",              "Read" }, { "SingleAsync",          "Read" },
            { "ToListAll",           "Read" }, { "ToListAllAsync",       "Read" },
            { "ToList",              "Read" }, { "ToListAsync",          "Read" },
            { "ToListByFilter",      "Read" }, { "ToListByFilterAsync",  "Read" },
            { "ToListDto",           "Read" }, { "ToListDtoAsync",       "Read" },

            // Write operations all map to Modify (Create/Update/Delete merged).
            { "Add",                 "Modify" }, { "AddAsync",             "Modify" },
            { "AddDto",              "Modify" }, { "AddDtoAsync",          "Modify" },
            { "AddRange",            "Modify" }, { "AddRangeAsync",        "Modify" },
            { "Update",               "Modify" },
            { "UpdateDto",            "Modify" },
            { "UpdateRange",          "Modify" },
            { "Remove",               "Modify" },
            { "RemoveRange",          "Modify" },
        };

        public PermissionAuthorizationFilter(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Get the controller descriptor
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor == null) return;

            // Check if controller has [PermissionGroup] attribute
            var permissionGroup = controllerActionDescriptor.ControllerTypeInfo
                .GetCustomAttribute<PermissionGroupAttribute>();
            if (permissionGroup == null) return; // No group = skip this filter

            // Map action name to operation
            var actionName = controllerActionDescriptor.ActionName;
            if (!ActionToOperation.TryGetValue(actionName, out var operation))
                return; // Unknown action = skip (let other auth handle it)

            // Build the required permission: e.g., "Members.Brokerage.Create"
            const string ServicePrefix = "Person";
            var requiredPermission = $"{ServicePrefix}.{permissionGroup.Group}.{operation}";

            // Check authorization
            var requirement = new PermissionRequirement(requiredPermission);
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(requirement)
                .Build();

            var result = await _authorizationService.AuthorizeAsync(context.HttpContext.User, policy);

            if (!result.Succeeded)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
