using DotnetVoyager.BLL.Options;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetVoyager.BLL.Workers;

public class StorageCleanupWorker : BackgroundService
{
    private readonly ILogger<StorageCleanupWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly StorageOptions _storageOptions;

    public StorageCleanupWorker(
        ILogger<StorageCleanupWorker> logger,
        IServiceProvider serviceProvider,
        IOptions<StorageOptions> storageOptions)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _storageOptions = storageOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Storage Cleanup Worker starting. Cleanup interval: {Interval} minutes, File lifetime: {Lifetime} minutes",
            _storageOptions.CleanupIntervalMinutes,
            _storageOptions.FileLifetimeMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(_storageOptions.CleanupIntervalMinutes),
                    stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await CleanupExpiredAnalysesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Storage Cleanup Worker stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error in Storage Cleanup Worker");
            }
        }

        _logger.LogInformation("Storage Cleanup Worker stopped");
    }

    private async Task CleanupExpiredAnalysesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();

        var expirationThreshold = DateTime.UtcNow.AddMinutes(-_storageOptions.FileLifetimeMinutes);

        _logger.LogInformation(
            "Starting cleanup of analyses older than {Threshold}",
            expirationThreshold);

        // Get expired analyses from database
        var expiredAnalyses = await dbContext.AnalysisStatuses
            .Where(a => a.LastUpdatedUtc < expirationThreshold)
            .Select(a => new { a.AnalysisId, a.OriginalFileName, a.LastUpdatedUtc })
            .ToListAsync(cancellationToken);

        if (expiredAnalyses.Count == 0)
        {
            _logger.LogInformation("No expired analyses found");
            return;
        }

        _logger.LogInformation(
            "Found {Count} expired analyses to clean up",
            expiredAnalyses.Count);

        int successCount = 0;
        int failureCount = 0;

        foreach (var analysis in expiredAnalyses)
        {
            try
            {
                // Delete from database first (cascade will delete steps)
                await dbContext.AnalysisStatuses
                    .Where(a => a.AnalysisId == analysis.AnalysisId)
                    .ExecuteDeleteAsync(cancellationToken);

                // Delete files from storage
                await storageService.DeleteAnalysisAsync(analysis.AnalysisId, cancellationToken);

                successCount++;

                _logger.LogInformation(
                    "Cleaned up analysis {AnalysisId} (File: {FileName}, Last updated: {LastUpdated})",
                    analysis.AnalysisId,
                    analysis.OriginalFileName,
                    analysis.LastUpdatedUtc);
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(
                    ex,
                    "Failed to clean up analysis {AnalysisId}",
                    analysis.AnalysisId);
            }
        }

        _logger.LogInformation(
            "Cleanup completed. Success: {Success}, Failed: {Failed}",
            successCount,
            failureCount);
    }
}