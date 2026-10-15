namespace KSS.Service.IService
{
    /// <summary>
    /// How two permission sets relate under containment. Containment is a
    /// partial order: two sets may be Incomparable when each holds a permission
    /// the other lacks.
    /// </summary>
    public enum PrivilegeRelation
    {
        Equal,
        Dominates,
        DominatedBy,
        Incomparable
    }

    /// <summary>
    /// Pure comparison over permission-code sets. A set "covers" another when it
    /// is a superset of it (equal sets cover each other). No I/O.
    /// </summary>
    public static class PrivilegeOrder
    {
        public static bool Covers(IReadOnlySet<string> holder, IReadOnlySet<string> other)
            => holder.IsSupersetOf(other);

        public static PrivilegeRelation Compare(IReadOnlySet<string> a, IReadOnlySet<string> b)
        {
            var aCoversB = Covers(a, b);
            var bCoversA = Covers(b, a);

            if (aCoversB && bCoversA) return PrivilegeRelation.Equal;
            if (aCoversB) return PrivilegeRelation.Dominates;
            if (bCoversA) return PrivilegeRelation.DominatedBy;
            return PrivilegeRelation.Incomparable;
        }

        /// <summary>
        /// The sets that no other set strictly contains. Under a partial order
        /// there may be several. Duplicates are collapsed; the empty set is
        /// never maximal.
        /// </summary>
        public static IReadOnlyList<IReadOnlySet<string>> MaximalSets(IEnumerable<IReadOnlySet<string>> sets)
        {
            var distinct = new List<IReadOnlySet<string>>();
            foreach (var set in sets)
            {
                if (set.Count == 0) continue;
                if (distinct.Any(existing => existing.SetEquals(set))) continue;
                distinct.Add(set);
            }

            return distinct
                .Where(candidate => !distinct.Any(other =>
                    !ReferenceEquals(other, candidate) &&
                    other.IsProperSupersetOf(candidate)))
                .ToList();
        }

        /// <summary>
        /// Returns a currently-maximal set that would have no active holder covering
        /// it after the target's set changes, or null when every maximal set keeps a
        /// holder. <paramref name="targetSetAfter"/> is the target's set after the
        /// change, or null when the target will no longer be an active holder.
        /// </summary>
        public static IReadOnlySet<string>? FindOrphanedMaximalSet(
            IReadOnlyDictionary<Guid, IReadOnlySet<string>> activeSetsBefore,
            Guid targetUserId,
            IReadOnlySet<string>? targetSetAfter)
        {
            var maximal = MaximalSets(activeSetsBefore.Values);

            var after = activeSetsBefore
                .Where(entry => entry.Key != targetUserId)
                .Select(entry => entry.Value)
                .ToList();
            if (targetSetAfter is { Count: > 0 })
                after.Add(targetSetAfter);

            return maximal.FirstOrDefault(set => !after.Any(holder => Covers(holder, set)));
        }
    }

    /// <summary>
    /// Reads permission sets live from the database. A user's set is the union of
    /// the permission codes of every active role assignment:
    ///   UserRole (DeletedAt IS NULL) -> RolePermission (DeletedAt IS NULL) -> Permission.Code.
    ///
    /// This intentionally does not read permission claims from the caller's token.
    /// Claims are issued at sign-in and stay unchanged until the token expires, so a
    /// read at call time reflects a role change immediately. It also differs from the
    /// token-issuing read, which does not filter DeletedAt; the two definitions are
    /// not expected to match.
    /// </summary>
    public interface IPrivilegeComparer
    {
        /// <summary>Permission codes held by the user through active assignments. Empty if none.</summary>
        Task<IReadOnlySet<string>> GetUserPermissionsAsync(Guid userId);

        /// <summary>Whether the user exists, is active (IsActive) and is not soft-deleted (DeletedAt IS NULL).</summary>
        Task<bool> IsActiveUserAsync(Guid userId);

        /// <summary>
        /// Permission codes per role, keyed by role id, for the requested roles that
        /// exist. A requested id absent from the result is not a known role. An
        /// existing role with no active permissions maps to an empty set.
        /// </summary>
        Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetRolePermissionsAsync(IEnumerable<Guid> roleIds);

        /// <summary>
        /// Permission set of every active, non-soft-deleted user, keyed by user id.
        /// Users with no permissions are omitted.
        /// </summary>
        Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetActiveUserPermissionSetsAsync();
    }
}
