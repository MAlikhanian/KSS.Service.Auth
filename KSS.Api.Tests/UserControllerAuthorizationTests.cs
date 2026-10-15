using System.Reflection;
using KSS.Api.Authorization;
using KSS.Api.Controller;
using KSS.Helper.CustomAttribute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KSS.Api.Tests
{
    public class UserControllerAuthorizationTests
    {
        private const string Read = "Person.Security.Read";
        private const string Modify = "Person.Security.Modify";

        // The authorization each action states. Every action on the controller must
        // appear in exactly one of these four sets.
        private static readonly string[] Anonymous = { "Register", "Login" };
        private static readonly string[] CallerScoped = { "GetCurrentUser", "ChangePassword" };
        private static readonly string[] AuthenticatedOnly = { "MapPersonsToUsers" };

        private static readonly Dictionary<string, string> Permissioned = new(StringComparer.Ordinal)
        {
            ["ByPersonId"] = Read,
            ["UserRoles"] = Read,
            ["AdminResetPassword"] = Modify,
            ["AssignRoles"] = Modify,
            ["Lock"] = Modify,
            ["Unlock"] = Modify,
            ["MarkEmailVerified"] = Modify,
            ["MarkPhoneVerified"] = Modify,
            ["RevokeSessions"] = Modify,
            ["SetActive"] = Modify,
        };

        private static IReadOnlyList<MethodInfo> Actions => AuthorizationHarness.ActionsOf(typeof(UserController));

        public static IEnumerable<object[]> PermissionedActions =>
            Permissioned.Select(pair => new object[] { pair.Key, pair.Value });

        public static IEnumerable<object[]> CallerScopedActions =>
            CallerScoped.Select(name => new object[] { name });

        public static IEnumerable<object[]> AnonymousActions =>
            Anonymous.Select(name => new object[] { name });

        private static MethodInfo Action(string name) =>
            Actions.SingleOrDefault(a => a.Name == name)
            ?? throw new InvalidOperationException("No action named " + name);

        // ── A. Every action states its authorization, and states exactly one ──────

        [Fact]
        public void Every_action_states_exactly_one_authorization()
        {
            var undeclared = new List<string>();
            var ambiguous = new List<string>();

            foreach (var action in Actions)
            {
                var statements =
                    (AuthorizationHarness.DeclaredPermission(action) != null ? 1 : 0) +
                    (AuthorizationHarness.Has<CallerScopedAttribute>(action) ? 1 : 0) +
                    (AuthorizationHarness.Has<AuthenticatedOnlyAttribute>(action) ? 1 : 0) +
                    (AuthorizationHarness.IsAnonymous(action) ? 1 : 0);

                if (statements == 0) undeclared.Add(action.Name);
                if (statements > 1) ambiguous.Add(action.Name);
            }

            Assert.True(undeclared.Count == 0, "Actions stating no authorization: " + string.Join(", ", undeclared));
            Assert.True(ambiguous.Count == 0, "Actions stating more than one: " + string.Join(", ", ambiguous));
        }

        [Fact]
        public void Action_inventory_matches_the_expected_set()
        {
            var expected = Anonymous
                .Concat(CallerScoped)
                .Concat(AuthenticatedOnly)
                .Concat(Permissioned.Keys)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            var actual = Actions.Select(a => a.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

            Assert.Equal(expected, actual);
            Assert.Equal(15, actual.Length);
        }

        [Theory]
        [MemberData(nameof(PermissionedActions))]
        public void Permissioned_action_states_the_expected_code(string name, string expected)
        {
            Assert.Equal(expected, AuthorizationHarness.DeclaredPermission(Action(name)));
        }

        [Fact]
        public void AuthenticatedOnly_actions_record_a_reason()
        {
            foreach (var name in AuthenticatedOnly)
            {
                var attribute = Action(name)
                    .GetCustomAttributes(inherit: true)
                    .OfType<AuthenticatedOnlyAttribute>()
                    .Single();

                Assert.False(string.IsNullOrWhiteSpace(attribute.Reason));
            }
        }

        // ── The public surface is pinned ──────────────────────────────────────────
        // Stated separately from the inventory above, and deliberately so: the
        // inventory names which actions exist, and the natural way to satisfy it
        // after adding one is to add the new name to the list. These three pin which
        // actions are reachable without authentication, so widening the public
        // surface fails on its own terms rather than as a name-list difference.

        [Fact]
        public void AllowAnonymous_is_carried_by_exactly_the_two_public_actions()
        {
            var anonymous = Actions
                .Where(AuthorizationHarness.IsAnonymous)
                .Select(a => a.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            var expected = Anonymous.OrderBy(n => n, StringComparer.Ordinal).ToArray();

            Assert.Equal(expected, anonymous);
        }

        [Fact]
        public void Controller_itself_carries_no_anonymous_declaration()
        {
            var anonymous = typeof(UserController)
                .GetCustomAttributes(inherit: true)
                .OfType<IAllowAnonymous>()
                .Any();

            Assert.False(anonymous, "A controller-wide anonymous declaration would cover every action on it.");
        }

        [Fact]
        public void Only_the_public_actions_reach_the_service_layer_unauthenticated()
        {
            var reachable = new List<string>();

            foreach (var action in Actions)
            {
                var context = AuthorizationHarness.FilterContext(
                    typeof(UserController), action, AuthorizationHarness.Anonymous());

                AuthorizationHarness.RunGate(context);

                if (context.Result == null)
                    reachable.Add(action.Name);
            }

            Assert.Equal(
                Anonymous.OrderBy(n => n, StringComparer.Ordinal).ToArray(),
                reachable.OrderBy(n => n, StringComparer.Ordinal).ToArray());
        }

        // ── Negative control ──────────────────────────────────────────────────────
        // An action stating nothing is denied. This is the canary for the gate
        // itself: if it does not deny here, the gate is not doing its work and every
        // other denial below should be read as unproven rather than as passing.

        private class UndeclaredController : ControllerBase
        {
            public IActionResult Undeclared() => new OkResult();
        }

        [AllowAnonymous]
        private class AnonymousControllerWithPermissionedAction : ControllerBase
        {
            [HasPermission(Modify)]
            public IActionResult Contradictory() => new OkResult();
        }

        [Fact]
        public void NEGATIVE_CONTROL_action_stating_nothing_is_denied()
        {
            var action = typeof(UndeclaredController).GetMethod(nameof(UndeclaredController.Undeclared))!;

            var context = AuthorizationHarness.FilterContext(
                typeof(UndeclaredController), action, AuthorizationHarness.Caller(Modify));

            AuthorizationHarness.RunGate(context);

            Assert.IsType<ForbidResult>(context.Result);
        }

        [Fact]
        public void Controller_wide_anonymous_over_a_permissioned_action_is_denied()
        {
            var type = typeof(AnonymousControllerWithPermissionedAction);
            var action = type.GetMethod(nameof(AnonymousControllerWithPermissionedAction.Contradictory))!;

            var context = AuthorizationHarness.FilterContext(type, action, AuthorizationHarness.Caller(Modify));

            AuthorizationHarness.RunGate(context);

            Assert.IsType<ForbidResult>(context.Result);
        }

        // ── The gate is attached to the controller ────────────────────────────────

        [Fact]
        public void Gate_is_attached_to_UserController()
        {
            var attached = typeof(UserController)
                .GetCustomAttributes(inherit: true)
                .OfType<ServiceFilterAttribute>()
                .Any(f => f.ServiceType == typeof(RequireExplicitAuthorizationFilter));

            Assert.True(attached, "UserController does not carry the RequireExplicitAuthorizationFilter service filter.");
        }

        [Fact]
        public void Gate_is_registered_in_the_container()
        {
            Assert.True(
                AuthorizationHarness.ContainerResolvesGate(),
                "The container built by AddBaseServiceExtention does not resolve RequireExplicitAuthorizationFilter.");
        }

        // ── B. A caller without the right is denied, for all ten permissioned actions

        [Theory]
        [MemberData(nameof(PermissionedActions))]
        public async Task Caller_without_the_permission_is_denied(string name, string permission)
        {
            Assert.Equal(permission, AuthorizationHarness.DeclaredPermission(Action(name)));

            var granted = await AuthorizationHarness.IsGrantedAsync(permission, AuthorizationHarness.Caller());

            Assert.False(granted, name + " was granted to a caller holding no permission claims.");
        }

        [Theory]
        [MemberData(nameof(PermissionedActions))]
        public async Task Caller_holding_only_the_other_code_is_denied(string name, string permission)
        {
            var other = permission == Modify ? Read : Modify;

            var granted = await AuthorizationHarness.IsGrantedAsync(permission, AuthorizationHarness.Caller(other));

            Assert.False(granted, name + " was granted to a caller holding only " + other + ".");
        }

        [Fact]
        public async Task Unauthenticated_caller_is_denied_every_permission()
        {
            foreach (var permission in Permissioned.Values.Distinct())
            {
                Assert.False(await AuthorizationHarness.IsGrantedAsync(permission, AuthorizationHarness.Anonymous()));
            }
        }

        // ── C. A caller holding the right is granted ──────────────────────────────

        [Theory]
        [MemberData(nameof(PermissionedActions))]
        public async Task Caller_holding_the_permission_is_granted(string name, string permission)
        {
            var granted = await AuthorizationHarness.IsGrantedAsync(permission, AuthorizationHarness.Caller(permission));

            Assert.True(granted, name + " was denied to a caller holding " + permission + ".");
        }

        // ── D. The four exclusions remain reachable ───────────────────────────────

        [Theory]
        [MemberData(nameof(AnonymousActions))]
        public void Anonymous_action_passes_the_gate_without_authentication(string name)
        {
            var context = AuthorizationHarness.FilterContext(
                typeof(UserController), Action(name), AuthorizationHarness.Anonymous());

            AuthorizationHarness.RunGate(context);

            Assert.Null(context.Result);
        }

        [Theory]
        [MemberData(nameof(CallerScopedActions))]
        public void CallerScoped_action_passes_the_gate_for_a_caller_holding_no_permissions(string name)
        {
            var context = AuthorizationHarness.FilterContext(
                typeof(UserController), Action(name), AuthorizationHarness.Caller());

            AuthorizationHarness.RunGate(context);

            Assert.Null(context.Result);
        }

        [Fact]
        public void AuthenticatedOnly_action_passes_the_gate_for_a_caller_holding_no_permissions()
        {
            foreach (var name in AuthenticatedOnly)
            {
                var context = AuthorizationHarness.FilterContext(
                    typeof(UserController), Action(name), AuthorizationHarness.Caller());

                AuthorizationHarness.RunGate(context);

                Assert.Null(context.Result);
            }
        }

        [Theory]
        [MemberData(nameof(CallerScopedActions))]
        public void CallerScoped_action_is_denied_to_an_unauthenticated_caller(string name)
        {
            var context = AuthorizationHarness.FilterContext(
                typeof(UserController), Action(name), AuthorizationHarness.Anonymous());

            AuthorizationHarness.RunGate(context);

            Assert.IsType<ChallengeResult>(context.Result);
        }

        // A permissioned action passes the gate once it has stated a code, so the only
        // thing between it and the service layer is the permission decision itself.

        [Theory]
        [MemberData(nameof(PermissionedActions))]
        public void Permissioned_action_passes_the_gate_and_defers_to_the_policy(string name, string permission)
        {
            var context = AuthorizationHarness.FilterContext(
                typeof(UserController), Action(name), AuthorizationHarness.Caller(permission));

            AuthorizationHarness.RunGate(context);

            Assert.Null(context.Result);
        }
    }
}
