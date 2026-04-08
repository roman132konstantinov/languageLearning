using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API.Infrastructure
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly LanguageLearningDbContext _dbContext;

        public DatabaseHealthCheck(LanguageLearningDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
                return canConnect
                    ? HealthCheckResult.Healthy("Database is reachable.")
                    : HealthCheckResult.Unhealthy("Database is unavailable.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database health check failed.", ex);
            }
        }
    }
}
