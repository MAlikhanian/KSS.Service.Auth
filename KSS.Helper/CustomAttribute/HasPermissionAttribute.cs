using Microsoft.AspNetCore.Authorization;

namespace KSS.Helper.CustomAttribute
{
    /// <summary>
    /// Requires the authenticated user to have the specified permission claim in their JWT token.
    /// Usage: [HasPermission("Service.Table.Action")] e.g. [HasPermission("Members.Brokerage.Create")]
    /// </summary>
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        public const string PolicyPrefix = "Permission_";

        public HasPermissionAttribute(string permission)
            : base(PolicyPrefix + permission)
        {
        }
    }
}
