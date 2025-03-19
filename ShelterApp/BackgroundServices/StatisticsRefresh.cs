using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShelterApp.BackgroundServices
{
    public class StatisticsRefresh : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public StatisticsRefresh(IServiceProvider serviceProvider, ILogger<StatisticsRefresh> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Statistics refresh running at: {time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        await dbContext.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW totalstatistics;");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error when refreshing statistics.");
                }

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
