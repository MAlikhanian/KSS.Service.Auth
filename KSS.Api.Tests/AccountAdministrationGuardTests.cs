using KSS.Helper;
using KSS.Service.IService;
using KSS.Service.Service;
using Xunit;

namespace KSS.Api.Tests
{
    /// <summary>
    /// In-memory stand-in for the live permission reads, so the guard's decisions can
    /// be tested without a database.
    /// </summary>
    internal sealed class FakePrivilegeComparer : IPrivilegeComparer
    {
        public Dictionary<Guid, HashSet<string>> UserPermissions { get; } = new();
        public HashSet<Guid> ActiveUsers { get; } = new();
        public Dictionary<Guid, HashSet<string>> RolePermissions { get; } = new();

        public Task<IReadOnlySet<string>> GetUserPermissionsAsync(Guid userId)
            => Task.FromResult<IReadOnlySet<string>>(UserPermissions.TryGetValue(userId, out var set)
                ? new HashSet<string>(set, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal));

        public Task<bool> IsActiveUserAsync(Guid userId) => Task.FromResult(ActiveUsers.Contains(userId));

        public Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetRolePermissionsAsync(IEnumerable<Guid> roleIds)
        {
            var result = new Dictionary<Guid, IReadOnlySet<string>>();
            foreach (var id in roleIds.Distinct())
                if (RolePermissions.TryGetValue(id, out var set))
                    result[id] = new HashSet<string>(set, StringComparer.Ordinal);
            return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlySet<string>>>(result);
        }

        public Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetActiveUserPermissionSetsAsync()
        {
            var result = new Dictionary<Guid, IReadOnlySet<string>>();
            foreach (var (userId, set) in UserPermissions)
                if (ActiveUsers.Contains(userId) && set.Count > 0)
                    result[userId] = new HashSet<string>(set, StringComparer.Ordinal);
            return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlySet<string>>>(result);
        }
    }

    public class AccountAdministrationGuardTests
    {
        // Permission codes, shaped like the real catalog.
        private const string Admin = AccountAdministrationGuard.AdministrationPermission; // Person.Security.Modify
        private const string Read = "Person.Security.Read";
        private const string PersonInfo = "Person.Information.Modify";
        private const string CompanyInfo = "Company.Information.Modify";
        private const string Developer = "Developer.Access";
        private const string Dms = "Dms.Project.Control";

        // Roles.
        private static readonly Guid TopRole = Guid.NewGuid();          // global role: everything except the DMS module
        private static readonly Guid PersonAdminRole = Guid.NewGuid();
        private static readonly Guid MemberRole = Guid.NewGuid();       // holds the administration permission
        private static readonly Guid ViewerRole = Guid.NewGuid();
        private static readonly Guid DmsRole = Guid.NewGuid();          // module-only, withheld from the top role
        private static readonly Guid DmsAdminRole = Guid.NewGuid();

        private static readonly string[] TopSet = { Admin, Read, PersonInfo, CompanyInfo, Developer };
        private static readonly string[] PersonAdminSet = { Admin, Read, PersonInfo };
        private static readonly string[] MemberSet = { Admin, Read };
        private static readonly string[] ViewerSet = { Read };
        private static readonly string[] DmsSet = { Dms };
        private static readonly string[] DmsAdminSet = { Admin, Dms };

        // Users.
        private readonly Guid _top = Guid.NewGuid();
        private readonly Guid _personAdmin = Guid.NewGuid();
        private readonly Guid _personAdminPeer = Guid.NewGuid();
        private readonly Guid _member = Guid.NewGuid();
        private readonly Guid _viewer = Guid.NewGuid();
        private readonly Guid _dmsOnly = Guid.NewGuid();
        private readonly Guid _dmsAdmin = Guid.NewGuid();
        private readonly Guid _inactiveAdmin = Guid.NewGuid();
        private readonly Guid _demoted = Guid.NewGuid(); // token still says admin; live set does not

        private readonly FakePrivilegeComparer _privileges = new();
        private readonly AccountAdministrationOptions _options = new();
        private readonly AccountAdministrationGuard _guard;

        public AccountAdministrationGuardTests()
        {
            Role(TopRole, TopSet);
            Role(PersonAdminRole, PersonAdminSet);
            Role(MemberRole, MemberSet);
            Role(ViewerRole, ViewerSet);
            Role(DmsRole, DmsSet);
            Role(DmsAdminRole, DmsAdminSet);

            User(_top, TopSet);
            User(_personAdmin, PersonAdminSet);
            User(_personAdminPeer, PersonAdminSet);
            User(_member, MemberSet);
            User(_viewer, ViewerSet);
            User(_dmsOnly, DmsSet);
            User(_dmsAdmin, DmsAdminSet);
            User(_inactiveAdmin, PersonAdminSet, active: false);
            User(_demoted, ViewerSet);

            _guard = new AccountAdministrationGuard(_privileges, _options);
        }

        private void Role(Guid id, string[] codes) => _privileges.RolePermissions[id] = new HashSet<string>(codes, StringComparer.Ordinal);

        private void User(Guid id, string[] codes, bool active = true)
        {
            _privileges.UserPermissions[id] = new HashSet<string>(codes, StringComparer.Ordinal);
            if (active) _privileges.ActiveUsers.Add(id);
        }

        public static IEnumerable<object[]> TargetedOperations =>
            Enum.GetValues<AdminOperation>().Where(op => op != AdminOperation.AssignRoles).Select(op => new object[] { op });

        private static async Task<string> RefusalCode(Func<Task> act)
            => (await Assert.ThrowsAsync<AdminActionRefusedException>(act)).Code;

        // ── Negative control: comparable pairs pass, so the refusals below are not vacuous ──

        [Theory]
        [MemberData(nameof(TargetedOperations))]
        public async Task Caller_who_dominates_the_target_is_allowed(AdminOperation op)
            => await _guard.EnsureMayActOnAsync(_personAdmin, _member, op);

        [Theory]
        [MemberData(nameof(TargetedOperations))]
        public async Task Caller_equal_to_the_target_is_allowed(AdminOperation op)
            => await _guard.EnsureMayActOnAsync(_personAdmin, _personAdminPeer, op);

        [Fact]
        public async Task Granting_a_role_within_the_callers_set_is_allowed()
            => await _guard.EnsureMayAssignRolesAsync(_personAdmin, _viewer, new[] { ViewerRole });

        [Fact]
        public async Task Granting_a_role_equal_to_the_callers_set_is_allowed()
            => await _guard.EnsureMayAssignRolesAsync(_personAdmin, _viewer, new[] { PersonAdminRole });

        // ── Target precondition ──

        [Theory]
        [MemberData(nameof(TargetedOperations))]
        public async Task Caller_dominated_by_the_target_is_refused(AdminOperation op)
            => Assert.Equal("TARGET_NOT_COVERED", await RefusalCode(() => _guard.EnsureMayActOnAsync(_member, _personAdmin, op)));

        [Theory]
        [MemberData(nameof(TargetedOperations))]
        public async Task Holder_of_the_administration_permission_cannot_act_on_the_top_account(AdminOperation op)
            => Assert.Equal("TARGET_NOT_COVERED", await RefusalCode(() => _guard.EnsureMayActOnAsync(_member, _top, op)));

        [Theory]
        [MemberData(nameof(TargetedOperations))]
        public async Task Top_account_cannot_act_on_a_module_account_it_does_not_contain(AdminOperation op)
            => Assert.Equal("TARGET_NOT_COVERED", await RefusalCode(() => _guard.EnsureMayActOnAsync(_top, _dmsOnly, op)));

        [Theory]
        [MemberData(nameof(TargetedOperations))]
        public async Task Module_administrator_cannot_act_on_the_top_account(AdminOperation op)
            => Assert.Equal("TARGET_NOT_COVERED", await RefusalCode(() => _guard.EnsureMayActOnAsync(_dmsAdmin, _top, op)));

        // ── Caller state is read live ──

        [Fact]
        public async Task Inactive_caller_is_refused()
            => Assert.Equal("CALLER_INACTIVE", await RefusalCode(() => _guard.EnsureMayActOnAsync(_inactiveAdmin, _viewer, AdminOperation.Lock)));

        [Fact]
        public async Task Caller_whose_live_set_lacks_the_administration_permission_is_refused()
            => Assert.Equal("PERMISSION_REVOKED", await RefusalCode(() => _guard.EnsureMayActOnAsync(_demoted, _viewer, AdminOperation.Lock)));

        // ── Self ──

        [Fact]
        public async Task Password_reset_on_ones_own_account_is_refused()
            => Assert.Equal("SELF_NOT_ALLOWED", await RefusalCode(() => _guard.EnsureMayActOnAsync(_personAdmin, _personAdmin, AdminOperation.AdminResetPassword)));

        [Theory]
        [MemberData(nameof(TargetedOperations))]
        public async Task Self_is_refused_by_default_on_every_operation(AdminOperation op)
            => Assert.Equal("SELF_NOT_ALLOWED", await RefusalCode(() => _guard.EnsureMayActOnAsync(_personAdmin, _personAdmin, op)));

        [Theory]
        [MemberData(nameof(TargetedOperations))]
        public async Task Self_refusal_can_be_carved_out_per_operation(AdminOperation op)
        {
            _options.RefuseSelf.Remove(op);
            await _guard.EnsureMayActOnAsync(_personAdmin, _personAdmin, op);
        }

        [Fact]
        public async Task Carving_out_one_operation_leaves_the_others_refused()
        {
            _options.RefuseSelf.Remove(AdminOperation.Lock);
            Assert.Equal("SELF_NOT_ALLOWED", await RefusalCode(() => _guard.EnsureMayActOnAsync(_personAdmin, _personAdmin, AdminOperation.SetActive)));
        }

        [Fact]
        public async Task Role_assignment_on_ones_own_account_is_refused_even_if_the_switch_is_cleared()
        {
            _options.RefuseSelf.Clear();
            Assert.Equal("SELF_NOT_ALLOWED", await RefusalCode(() => _guard.EnsureMayAssignRolesAsync(_top, _top, new[] { TopRole })));
        }

        // ── Role assignment ──

        [Fact]
        public async Task Administration_permission_holder_cannot_grant_the_top_role_to_themselves()
            => Assert.Equal("SELF_NOT_ALLOWED", await RefusalCode(() => _guard.EnsureMayAssignRolesAsync(_member, _member, new[] { TopRole })));

        [Fact]
        public async Task Administration_permission_holder_cannot_grant_the_top_role_to_anyone()
            => Assert.Equal("ROLE_NOT_COVERED", await RefusalCode(() => _guard.EnsureMayAssignRolesAsync(_member, _viewer, new[] { TopRole })));

        [Fact]
        public async Task Granting_a_role_broader_than_the_callers_set_is_refused()
            => Assert.Equal("ROLE_NOT_COVERED", await RefusalCode(() => _guard.EnsureMayAssignRolesAsync(_personAdmin, _viewer, new[] { ViewerRole, TopRole })));

        [Fact]
        public async Task Top_account_cannot_grant_a_module_role_it_does_not_contain()
            => Assert.Equal("ROLE_NOT_COVERED", await RefusalCode(() => _guard.EnsureMayAssignRolesAsync(_top, _viewer, new[] { DmsRole })));

        [Fact]
        public async Task Changing_the_roles_of_a_broader_account_is_refused()
            => Assert.Equal("TARGET_NOT_COVERED", await RefusalCode(() => _guard.EnsureMayAssignRolesAsync(_personAdmin, _top, Array.Empty<Guid>())));

        [Fact]
        public async Task Unknown_role_is_refused()
            => await Assert.ThrowsAsync<BusinessRuleException>(() => _guard.EnsureMayAssignRolesAsync(_personAdmin, _viewer, new[] { Guid.NewGuid() }));

        // ── Last holder of a maximal set ──

        [Fact]
        public async Task Deactivating_the_sole_holder_of_the_top_set_is_refused_and_names_the_set_and_remedy()
        {
            var ex = await Assert.ThrowsAsync<LastMaximalHolderException>(() => _guard.EnsureMayDeactivateAsync(_top));

            Assert.Equal(TopSet.OrderBy(c => c, StringComparer.Ordinal), ex.Permissions);
            Assert.Contains("first give another user", ex.Message);
            Assert.Contains("approved database change", ex.Message);
        }

        [Fact]
        public async Task Deactivating_one_of_two_holders_of_the_top_set_is_allowed()
        {
            User(Guid.NewGuid(), TopSet);
            await _guard.EnsureMayDeactivateAsync(_top);
        }

        [Fact]
        public async Task Deactivation_guard_is_switchable()
        {
            _options.GuardMaximalHoldersOnDeactivate = false;
            await _guard.EnsureMayDeactivateAsync(_top);
        }

        [Fact]
        public async Task Deactivating_the_sole_holder_of_a_module_only_maximal_set_is_refused()
        {
            // The module administrator's set strictly contains the module-only set, so remove it
            // to make the module-only set maximal and solely held.
            _privileges.ActiveUsers.Remove(_dmsAdmin);
            var ex = await Assert.ThrowsAsync<LastMaximalHolderException>(() => _guard.EnsureMayDeactivateAsync(_dmsOnly));
            Assert.Equal(DmsSet, ex.Permissions);
        }

        [Fact]
        public async Task Deactivating_a_holder_whose_set_is_covered_by_another_is_allowed()
            => await _guard.EnsureMayDeactivateAsync(_dmsOnly);

        [Fact]
        public async Task Removing_the_top_role_from_a_peer_top_account_is_allowed_while_the_caller_holds_it()
        {
            var peer = Guid.NewGuid();
            User(peer, TopSet);
            await _guard.EnsureMayAssignRolesAsync(_top, peer, new[] { ViewerRole });
        }
    }

    public class FindOrphanedMaximalSetTests
    {
        private static IReadOnlySet<string> Set(params string[] codes) => new HashSet<string>(codes, StringComparer.Ordinal);

        [Fact]
        public void Dropping_the_sole_top_holder_orphans_the_top_set()
        {
            var a = Guid.NewGuid();
            var before = new Dictionary<Guid, IReadOnlySet<string>> { [a] = Set("x", "y"), [Guid.NewGuid()] = Set("x") };
            var orphaned = PrivilegeOrder.FindOrphanedMaximalSet(before, a, null);
            Assert.NotNull(orphaned);
            Assert.True(orphaned!.SetEquals(Set("x", "y")));
        }

        [Fact]
        public void Dropping_one_of_two_top_holders_orphans_nothing()
        {
            var a = Guid.NewGuid();
            var before = new Dictionary<Guid, IReadOnlySet<string>> { [a] = Set("x", "y"), [Guid.NewGuid()] = Set("x", "y") };
            Assert.Null(PrivilegeOrder.FindOrphanedMaximalSet(before, a, null));
        }

        [Fact]
        public void Shrinking_the_sole_top_holder_orphans_the_top_set()
        {
            var a = Guid.NewGuid();
            var before = new Dictionary<Guid, IReadOnlySet<string>> { [a] = Set("x", "y") };
            Assert.NotNull(PrivilegeOrder.FindOrphanedMaximalSet(before, a, Set("x")));
        }

        [Fact]
        public void An_empty_new_set_counts_as_removal()
        {
            var a = Guid.NewGuid();
            var before = new Dictionary<Guid, IReadOnlySet<string>> { [a] = Set("x") };
            Assert.NotNull(PrivilegeOrder.FindOrphanedMaximalSet(before, a, Set()));
        }

        [Fact]
        public void Keeping_a_covering_set_orphans_nothing()
        {
            var a = Guid.NewGuid();
            var before = new Dictionary<Guid, IReadOnlySet<string>> { [a] = Set("x", "y") };
            Assert.Null(PrivilegeOrder.FindOrphanedMaximalSet(before, a, Set("x", "y", "z")));
        }

        [Fact]
        public void Every_maximal_set_is_protected_not_only_the_largest()
        {
            var top = Guid.NewGuid();
            var module = Guid.NewGuid();
            var before = new Dictionary<Guid, IReadOnlySet<string>> { [top] = Set("a", "b", "c"), [module] = Set("m") };
            var orphaned = PrivilegeOrder.FindOrphanedMaximalSet(before, module, null);
            Assert.NotNull(orphaned);
            Assert.True(orphaned!.SetEquals(Set("m")));
        }
    }
}
