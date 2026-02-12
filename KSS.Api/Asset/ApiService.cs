namespace KSS.Api.Asset
{
    public class ApiService : BackgroundService
    {
        public ApiService(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<ApiService>();
        }

        public ILogger Logger { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("KSS_API is starting.");

            stoppingToken.Register(() => Logger.LogInformation("KSS_API is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                Logger.LogInformation("KSS_API is doing background work.");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            Logger.LogInformation("KSS_API has stopped.");
        }
    }
}
