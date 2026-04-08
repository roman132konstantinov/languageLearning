using Application.Interfaces;
using Application.Options;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Infrastructure
{
    public static class StartupTasks
    {
        public static async Task InitializeDatabaseAsync(IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var databaseOptions = scopedServices.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var seedingOptions = scopedServices.GetRequiredService<IOptions<SeedingOptions>>().Value;
            var dbContext = scopedServices.GetRequiredService<LanguageLearningDbContext>();

            if (databaseOptions.ApplyMigrationsOnStartup)
            {
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied during startup.");
            }

            if (seedingOptions.EnableDemoData)
            {
                await DemoDataSeeder.SeedAsync(dbContext, logger);
            }
        }
    }
}
