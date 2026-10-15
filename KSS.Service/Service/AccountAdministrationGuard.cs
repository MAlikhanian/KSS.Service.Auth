using KSS.Helper;
using KSS.Service.IService;

namespace KSS.Service.Service
{
    /// <summary>
    /// Enforces who may administer whose account. The caller must still hold the
    /// administration permission when the call is made, must be active, and must
    /// cover the target's permission set (equal or broader). Permission sets are
    /// read at call time, not taken from the caller's token. See
    /// <see cref="IAccountAdministrationGuard"/> for why there is no override path.
    /// </summary>
    public class AccountAdministrationGuard : IAccountAdministrationGuard
    {
        /// <summary>The permission that grants account administration.</summary>
        public const string AdministrationPermission = "Person.Security.Modify";

        private readonly IPrivilegeComparer _privileges;
        private readonly AccountAdministrationOptions _options;

        public AccountAdministrationGuard(IPrivilegeComparer privileges, AccountAdministrationOptions options)
        {
            _privileges = privileges;
            _options = options;
        }

        public async Task EnsureMayActOnAsync(Guid callerUserId, Guid targetUserId, AdminOperation operation)
        {
            var refuseSelf = operation == AdminOperation.AssignRoles || _options.RefuseSelf.Contains(operation);
            await EnsureCallerCoversTargetAsync(callerUserId, targetUserId, refuseSelf);
        }

        public async Task EnsureMayAssignRolesAsync(Guid callerUserId, Guid targetUserId, IReadOnlyCollection<Guid> roleIds)
        {
            var callerPermissions = await EnsureCallerCoversTargetAsync(callerUserId, targetUserId, refuseSelf: true);

            var rolePermissions = await _privileges.GetRolePermissionsAsync(roleIds);
            var unknown = roleIds.Distinct().Where(id => !rolePermissions.ContainsKey(id)).ToList();
            if (unknown.Count > 0)
                throw new BusinessRuleException("UNKNOWN_ROLE");

            foreach (var role in rolePermissions)
            {
                if (!PrivilegeOrder.Covers(callerPermissions, role.Value))
                    throw new AdminActionRefusedException(
                        "ROLE_NOT_COVERED",
                        "A requested role includes permissions you do not hold.");
            }

            var targetSetAfter = new HashSet<string>(rolePermissions.Values.SelectMany(set => set), StringComparer.Ordinal);
            var targetStaysActive = await _privileges.IsActiveUserAsync(targetUserId);
            await EnsureNoMaximalSetOrphanedAsync(targetUserId, targetStaysActive ? targetSetAfter : null);
        }

        public async Task EnsureMayDeactivateAsync(Guid targetUserId)
        {
            if (!_options.GuardMaximalHoldersOnDeactivate) return;
            await EnsureNoMaximalSetOrphanedAsync(targetUserId, targetSetAfter: null);
        }

        private async Task<IReadOnlySet<string>> EnsureCallerCoversTargetAsync(Guid callerUserId, Guid targetUserId, bool refuseSelf)
        {
            if (!await _privileges.IsActiveUserAsync(callerUserId))
                throw new AdminActionRefusedException("CALLER_INACTIVE", "Your account is not active.");

            var callerPermissions = await _privileges.GetUserPermissionsAsync(callerUserId);
            if (!callerPermissions.Contains(AdministrationPermission))
                throw new AdminActionRefusedException("PERMISSION_REVOKED", "You no longer hold the account administration permission.");

            if (refuseSelf && callerUserId == targetUserId)
                throw new AdminActionRefusedException("SELF_NOT_ALLOWED", "This operation cannot be performed on your own account.");

            var targetPermissions = await _privileges.GetUserPermissionsAsync(targetUserId);
            if (!PrivilegeOrder.Covers(callerPermissions, targetPermissions))
                throw new AdminActionRefusedException(
                    "TARGET_NOT_COVERED",
                    "The target account holds permissions you do not hold.");

            return callerPermissions;
        }

        private async Task EnsureNoMaximalSetOrphanedAsync(Guid targetUserId, IReadOnlySet<string>? targetSetAfter)
        {
            var activeSets = await _privileges.GetActiveUserPermissionSetsAsync();
            var orphaned = PrivilegeOrder.FindOrphanedMaximalSet(activeSets, targetUserId, targetSetAfter);
            if (orphaned != null)
                throw new LastMaximalHolderException(orphaned.OrderBy(code => code, StringComparer.Ordinal).ToList());
        }
    }
}
