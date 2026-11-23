using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using KSS.Api.MappingProfile;
using KSS.Dto;
using KSS.Entity;
using KSS.Helper;
using KSS.Repository.IRepository;
using KSS.Service.IService;
using KSS.Service.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KSS.Api.Tests
{
    /// <summary>
    /// Self-registration writes the User inside one transaction BEFORE the Person is created, so a failure in the
    /// user's own write never leaves a Person behind, and a failed Person call never leaves a User behind. After the
    /// Person exists, a failure rolls the User back and logs the Person id once; nothing is ever deleted from Person.
    /// </summary>
    public class RegistrationOrderTests
    {
        // ── fakes ────────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>Routes interface calls to a handler; anything unhandled fails the test loudly.</summary>
        public class Proxy : DispatchProxy
        {
            public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = (m, _) => throw new NotSupportedException(m.Name);
            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
        }

        private sealed class World
        {
            public List<string> Events { get; } = new();
            public List<User> CommittedUsers { get; } = new();
            public User? Pending { get; set; }
            public Exception? FailUserInsert { get; set; }
            public Exception? FailPerson { get; set; }
            public Exception? FailRole { get; set; }
            public List<string> PersonCalls { get; } = new();
            public Guid PersonId { get; } = Guid.CreateVersion7();
        }

        private sealed class Tx(World w) : IRepositoryTransaction
        {
            private bool _committed;
            public Task CommitAsync()
            {
                w.Events.Add("commit");
                if (w.Pending != null) w.CommittedUsers.Add(w.Pending);
                _committed = true;
                return Task.CompletedTask;
            }
            public ValueTask DisposeAsync()
            {
                if (!_committed) { w.Events.Add("rollback"); w.Pending = null; }
                return ValueTask.CompletedTask;
            }
        }

        private static IUserRepository Users(World w)
        {
            var repo = DispatchProxy.Create<IUserRepository, Proxy>();
            ((Proxy)(object)repo).Handler = (m, a) =>
            {
                switch (m.Name)
                {
                    case "SingleOrDefaultAsync" when a?.Length == 1 && a[0] is Expression<Func<User, bool>>:
                        return Task.FromResult<User?>(null);
                    case "BeginRegistrationTransactionAsync":
                        w.Events.Add("begin");
                        return Task.FromResult<IRepositoryTransaction>(new Tx(w));
                    case "AddAsync" when a != null && a[0] is User u:
                        w.Events.Add("user-insert");
                        if (w.FailUserInsert != null) throw w.FailUserInsert;
                        if (u.Id == Guid.Empty) u.Id = Guid.CreateVersion7();
                        w.Pending = u;
                        return Task.CompletedTask;
                    case "Update" when a != null && a[0] is User:
                        w.Events.Add("user-update");
                        return null;
                    case "SaveChangesAsync":
                        w.Events.Add("save");
                        return Task.CompletedTask;
                    case "AssignDefaultRoleAsync":
                        w.Events.Add("role");
                        if (w.FailRole != null) throw w.FailRole;
                        return Task.CompletedTask;
                    default:
                        throw new NotSupportedException("unexpected repository call: " + m.Name);
                }
            };
            return repo;
        }

        private static IPersonApiClient People(World w)
        {
            var client = DispatchProxy.Create<IPersonApiClient, Proxy>();
            ((Proxy)(object)client).Handler = (m, a) =>
            {
                w.PersonCalls.Add(m.Name);
                w.Events.Add("person");
                if (w.FailPerson != null) throw w.FailPerson;
                var req = (CreatePersonRequestDto)a![0]!;
                return Task.FromResult(new PersonDto { Id = w.PersonId, NationalId = req.NationalId });
            };
            return client;
        }

        private sealed class TestLog : ILogger<UserService>
        {
            public List<(LogLevel Level, string Text)> Entries { get; } = new();
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) => Entries.Add((logLevel, formatter(state, exception)));
        }

        private static (UserService Service, World World, TestLog Log) Make()
        {
            var w = new World();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new BaseMappingProfile()), NullLoggerFactory.Instance).CreateMapper();
            var guard = DispatchProxy.Create<IAccountAdministrationGuard, Proxy>();
            var log = new TestLog();
            return (new UserService(mapper, Users(w), People(w), guard, log), w, log);
        }

        private static RegisterRequestDto Request() => new()
        {
            Username = "0012345678",
            Email = "new.user@example.test",
            Password = "a-good-password",
            FirstName = "First",
            LastName = "Last",
        };

        // ── tests ────────────────────────────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task A_user_insert_failure_never_creates_a_Person()
        {
            var (service, w, _) = Make();
            w.FailUserInsert = new InvalidOperationException("column too short");

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(Request()));

            Assert.Empty(w.PersonCalls);
            Assert.Empty(w.CommittedUsers);
            Assert.Equal(new[] { "begin", "user-insert", "rollback" }, w.Events);
        }

        [Fact]
        public async Task A_Person_failure_leaves_no_user_row_and_keeps_todays_message()
        {
            var (service, w, log) = Make();
            w.FailPerson = new BusinessRuleException("DUPLICATE_NATIONAL_ID");

            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.RegisterAsync(Request()));

            Assert.Equal("DUPLICATE_NATIONAL_ID", ex.Message);
            Assert.Empty(w.CommittedUsers);
            Assert.Contains("rollback", w.Events);
            Assert.DoesNotContain(log.Entries, e => e.Level >= LogLevel.Error); // no Person exists, nothing to report
        }

        [Fact]
        public async Task A_role_failure_after_the_Person_exists_rolls_back_and_logs_the_person_id_once()
        {
            var (service, w, log) = Make();
            w.FailRole = new InvalidOperationException("role write failed");

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(Request()));

            Assert.Empty(w.CommittedUsers);
            Assert.Equal("rollback", w.Events[^1]);
            var error = Assert.Single(log.Entries, e => e.Level >= LogLevel.Error);
            Assert.Contains(w.PersonId.ToString(), error.Text);
            Assert.DoesNotContain("0012345678", error.Text);     // no national id
            Assert.DoesNotContain("new.user@example.test", error.Text);
            Assert.DoesNotContain("First", error.Text);
            Assert.Equal(new[] { "CreatePersonAsync" }, w.PersonCalls); // no delete, no second call
        }

        [Fact]
        public async Task The_happy_path_inserts_first_then_links_the_person_assigns_the_role_and_commits()
        {
            var (service, w, log) = Make();

            var dto = await service.RegisterAsync(Request());

            Assert.Equal(new[] { "begin", "user-insert", "person", "user-update", "save", "role", "commit" }, w.Events);
            var user = Assert.Single(w.CommittedUsers);
            Assert.Equal(w.PersonId, user.PersonId);
            Assert.Equal(w.PersonId, dto.PersonId);
            Assert.DoesNotContain(log.Entries, e => e.Level >= LogLevel.Error);
        }

        [Fact]
        public void No_Auth_path_can_delete_a_Person()
        {
            var members = typeof(IPersonApiClient).GetMethods().Select(m => m.Name).ToList();
            Assert.Equal(new[] { "CreatePersonAsync" }, members);
            Assert.DoesNotContain(members, n => n.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                                             || n.Contains("Remove", StringComparison.OrdinalIgnoreCase)
                                             || n.Contains("Abandon", StringComparison.OrdinalIgnoreCase));
        }
    }
}
