using System.Net;
using System.Text;
using KSS.Service.IService;
using KSS.Service.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KSS.Api.Tests
{
    /// <summary>
    /// Every captcha rejection writes exactly one warning naming its reason, and no log line ever carries the
    /// payload. The verdict itself is unchanged: every failure still returns false.
    /// </summary>
    public class CaptchaClientLoggingTests
    {
        private const string Payload = "PAYLOAD-MARKER-7f3a91";

        private sealed class CapturingLogger : ILogger<CaptchaClient>
        {
            public List<(LogLevel Level, string Message)> Entries { get; } = new();
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                Entries.Add((logLevel, formatter(state, exception) + (exception is null ? "" : " | " + exception)));
        }

        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
        {
            public int Calls { get; private set; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Calls++;
                return Task.FromResult(respond(request));
            }
        }

        private static (CaptchaClient Client, CapturingLogger Log, StubHandler Handler) Make(
            Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            var log = new CapturingLogger();
            var handler = new StubHandler(respond);
            return (new CaptchaClient(handler, new Uri("http://captcha.test/"), log), log, handler);
        }

        private static HttpResponseMessage Json(string body) =>
            new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        private static void AssertOneWarning(CapturingLogger log, string reason)
        {
            var warning = Assert.Single(log.Entries, e => e.Level >= LogLevel.Warning);
            Assert.Contains("Captcha verification failed", warning.Message);
            Assert.Contains("reason=" + reason, warning.Message);
            Assert.All(log.Entries, e => Assert.DoesNotContain(Payload, e.Message));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Empty_payload_is_rejected_without_a_call_and_logged(string? payload)
        {
            var (client, log, handler) = Make(_ => Json("{\"valid\":true}"));

            Assert.False(await client.VerifyAsync(payload!));

            Assert.Equal(0, handler.Calls);
            AssertOneWarning(log, "empty-payload");
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, "400")]
        [InlineData(HttpStatusCode.InternalServerError, "500")]
        public async Task Non_success_status_is_logged_with_its_code(HttpStatusCode status, string code)
        {
            var (client, log, _) = Make(_ => new HttpResponseMessage(status) { Content = new StringContent(Payload) });

            Assert.False(await client.VerifyAsync(Payload));

            AssertOneWarning(log, "http-status");
            Assert.Contains("status=" + code, log.Entries.Single(e => e.Level >= LogLevel.Warning).Message);
        }

        [Fact]
        public async Task Timeout_is_logged_as_a_timeout()
        {
            var (client, log, _) = Make(_ => throw new TaskCanceledException("timed out", new TimeoutException()));

            Assert.False(await client.VerifyAsync(Payload));

            AssertOneWarning(log, "timeout");
        }

        [Fact]
        public async Task Other_exception_is_logged_by_type_only()
        {
            var (client, log, _) = Make(_ => throw new HttpRequestException("connection refused " + Payload));

            Assert.False(await client.VerifyAsync(Payload));

            AssertOneWarning(log, "exception");
            var line = log.Entries.Single(e => e.Level >= LogLevel.Warning).Message;
            Assert.Contains("type=HttpRequestException", line);
            Assert.DoesNotContain("connection refused", line);
        }

        [Fact]
        public async Task Invalid_verdict_is_logged()
        {
            var (client, log, _) = Make(_ => Json("{\"valid\":false}"));

            Assert.False(await client.VerifyAsync(Payload));

            AssertOneWarning(log, "invalid");
        }

        [Fact]
        public async Task Valid_verdict_returns_true_and_logs_no_warning()
        {
            var (client, log, _) = Make(_ => Json("{\"valid\":true}"));

            Assert.True(await client.VerifyAsync(Payload));

            Assert.DoesNotContain(log.Entries, e => e.Level >= LogLevel.Warning);
            Assert.All(log.Entries, e => Assert.DoesNotContain(Payload, e.Message));
        }

        [Fact]
        public void The_container_still_builds_the_client_from_configuration()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["CaptchaApi:BaseUrl"] = "http://captcha.test/" })
                .Build());
            services.AddScoped<ICaptchaClient, CaptchaClient>();

            using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
            using var scope = provider.CreateScope();

            Assert.IsType<CaptchaClient>(scope.ServiceProvider.GetRequiredService<ICaptchaClient>());
        }
    }
}
