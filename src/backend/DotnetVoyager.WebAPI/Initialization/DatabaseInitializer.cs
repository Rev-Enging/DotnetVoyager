using DotnetVoyager.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace DotnetVoyager.DAL.Initialization;

public static class DatabaseInitializer
{
    public static async Task InitializeAndRecoverTasksAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var dbContext = services.GetRequiredService<AnalysisDbContext>();

            logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "CRITICAL: An error occurred during database initialization or recovery.");
            throw;
        }
    }
}