/*using DotnetVoyager.BLL.Enums;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Workers;*/
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

            await RecoverPendingTasksAsync(dbContext, services, logger);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "CRITICAL: An error occurred during database initialization or recovery.");
            throw;
        }
    }

    private static async Task RecoverPendingTasksAsync(
        AnalysisDbContext dbContext,
        IServiceProvider services,
        ILogger logger)
    {
        await Task.Delay(100);
        /*        var analysisQueue = services.GetRequiredService<IAnalysisTaskQueue>();
                var decompilationQueue = services.GetRequiredService<IDecompilationTaskQueue>();

                var analysisTasks = await dbContext.AnalysisStatuses
                    .AsNoTracking()
                    .Where(j => j.Status == AssemblyAnalysisStatus.Pending ||
                                j.Status == AssemblyAnalysisStatus.Processing)
                    .Select(j => j.AnalysisId)
                    .ToListAsync();

                foreach (var id in analysisTasks)
                {
                    await analysisQueue.EnqueueAsync(new AnalysisTask(id));
                }

                var decompTasks = await dbContext.AnalysisStatuses
                    .AsNoTracking()
                    .Where(j => j.ZipStatus == ZipGenerationStatus.Pending ||
                                j.ZipStatus == ZipGenerationStatus.Processing)
                    .Select(j => j.AnalysisId)
                    .ToListAsync();

                foreach (var id in decompTasks)
                {
                    await decompilationQueue.EnqueueAsync(new AnalysisTask(id));
                }

                logger.LogInformation(
                    "Database initialized. Recovered {AnalysisCount} analysis tasks and {DecompCount} decompilation tasks.",
                    analysisTasks.Count,
                    decompTasks.Count);*/
    }
}