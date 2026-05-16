using KSS.Helper.CustomAttribute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace KSS.Helper.Authorization
{
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
            _fallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
            _fallbackPolicyProvider.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix))
            {
                var permission = policyName[HasPermissionAttribute.PolicyPrefix.Length..];
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permission))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}
