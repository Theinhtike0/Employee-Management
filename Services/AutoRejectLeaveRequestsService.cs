using HR_Products.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HR_Products.Services
{
    public class AutoRejectLeaveRequestsService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<AutoRejectLeaveRequestsService> _logger;
        private const int AutoRejectAfterDays = 3;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

        public AutoRejectLeaveRequestsService(
            IServiceProvider services,
            ILogger<AutoRejectLeaveRequestsService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoReject Leave Requests Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var cutoffDate = DateTime.UtcNow.AddDays(-AutoRejectAfterDays);

                    var pendingRequests = await dbContext.LEAV_REQUESTS
                        .Where(lr => lr.Status == "Pending" && lr.RequestedAt <= cutoffDate)
                        .ToListAsync(stoppingToken);

                    if (pendingRequests.Any())
                    {
                        foreach (var request in pendingRequests)
                        {
                            request.Status = "Rejected";
                            request.ApprovedAt = DateTime.UtcNow;
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Auto-rejected {pendingRequests.Count} leave requests.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while auto-rejecting leave requests");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}