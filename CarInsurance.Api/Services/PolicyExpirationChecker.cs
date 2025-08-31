using CarInsurance.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CarInsurance.Api.Services
{
    public class PolicyExpirationChecker : BackgroundService
    {
        private readonly ILogger<PolicyExpirationChecker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public PolicyExpirationChecker(ILogger<PolicyExpirationChecker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Policy expiration checker service has started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking for expired policies...");

                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var expiredPolicies = await dbContext.Policies
                        .Where(p => p.EndDate < DateOnly.FromDateTime(DateTime.Today) && !p.IsExpirationLogged)
                        .ToListAsync(stoppingToken);

                    foreach (var policy in expiredPolicies)
                    {
                        _logger.LogWarning($"Policy with ID {policy.Id} has expired. End date: {policy.EndDate}");
                        policy.IsExpirationLogged = true;
                    }

                    if (expiredPolicies.Any())
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}