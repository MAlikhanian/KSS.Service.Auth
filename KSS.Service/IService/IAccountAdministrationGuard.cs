namespace KSS.Service.IService
{
    /// <summary>Account-administration operations that act on a target user.</summary>
    public enum AdminOperation
    {
        AssignRoles,
        AdminResetPassword,
        Lock,
        Unlock,
        MarkEmailVerified,
        MarkPhoneVerified,
        SetActive,
        RevokeSessions
    }

    /// <summary>
    /// Policy switches for account administration.
    /// </summary>
    public class AccountAdministrationOptions
    {
        /// <summary>
        /// Operations refused when the caller targets their own account. By default
        /// every administration operation is listed: these endpoints act on other
        /// accounts, and changes to one's own account go through self-service (for
        /// example, a password change that requires the current password). Removing an
        /// operation from this set permits it on one's own account. Role assignment is
        /// always refused on one's own account regardless of this set.
        /// </summary>
        public ISet<AdminOperation> RefuseSelf { get; } = new HashSet<AdminOperation>(Enum.GetValues<AdminOperation>());

        /// <summary>
        /// When true, deactivating a user is refused if it would leave no active
        /// holder of a currently-maximal permission set.
        /// </summary>
        public bool GuardMaximalHoldersOnDeactivate { get; set; } = true;
    }

    /// <summary>
    /// Decides whether a caller may perform an account-administration operation on
    /// a target user. All decisions use permission sets read at call time.
    ///
    /// There is deliberately no override path here. A change the caller does not
    /// dominate (including any change to the account holding the broadest
    /// permission set) is made as an approved, audited database change, not through
    /// the API. A privileged endpoint that skipped these checks would allow
    /// unrestricted role and account changes, so none is provided.
    /// </summary>
    public interface IAccountAdministrationGuard
    {
        /// <summary>Throws when the caller may not perform <paramref name="operation"/> on the target.</summary>
        Task EnsureMayActOnAsync(Guid callerUserId, Guid targetUserId, AdminOperation operation);

        /// <summary>
        /// Throws when the caller may not replace the target's roles with
        /// <paramref name="roleIds"/>.
        /// </summary>
        Task EnsureMayAssignRolesAsync(Guid callerUserId, Guid targetUserId, IReadOnlyCollection<Guid> roleIds);

        /// <summary>Throws when deactivating the target would leave a maximal permission set with no active holder.</summary>
        Task EnsureMayDeactivateAsync(Guid targetUserId);
    }

    /// <summary>The caller is authenticated but may not perform this account operation (HTTP 403).</summary>
    public class AdminActionRefusedException : Exception
    {
        public string Code { get; }

        public AdminActionRefusedException(string code, string message) : base(message)
        {
            Code = code;
        }
    }

    /// <summary>
    /// The change would leave a maximal permission set with no active holder (HTTP 409).
    /// </summary>
    public class LastMaximalHolderException : Exception
    {
        public const string ErrorCode = "LAST_MAXIMAL_HOLDER";

        public IReadOnlyCollection<string> Permissions { get; }

        public LastMaximalHolderException(IReadOnlyCollection<string> permissions)
            : base("This change would leave no active user holding the permission set listed in 'permissions'. "
                 + "To proceed, first give another user a role set that includes all of these permissions, then repeat this change; "
                 + "or make the change as an approved database change.")
        {
            Permissions = permissions;
        }
    }
}
