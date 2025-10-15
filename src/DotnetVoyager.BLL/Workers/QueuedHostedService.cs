using DotnetVoyager.BLL.Options;
using DotnetVoyager.BLL.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetVoyager.BLL.Workers;

public class QueuedHostedService : BackgroundService
{
    private readonly ILogger<QueuedHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly WorkerOptions _workerOptions;

    public QueuedHostedService(
        IOptions<WorkerOptions> workerOptions,
        ILogger<QueuedHostedService> logger,
        IServiceProvider serviceProvider,
        IBackgroundTaskQueue taskQueue)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _taskQueue = taskQueue;
        _workerOptions = workerOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var concurentWorkers = _workerOptions.AnalysisConcurrentWorkers;

        _logger.LogInformation("Queued Hosted Service is running with {WorkerCount} concurrent workers.", concurentWorkers);

        var workerTasks = new List<Task>();

        for (int i = 0; i < concurentWorkers; i++)
        {
            workerTasks.Add(ProcessQueueAsync(stoppingToken));
        }

        await Task.WhenAll(workerTasks);

        _logger.LogInformation("Queued Hosted Service is stopping.");
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(_workerOptions.AnalysisTimeoutMinutes));

            // It will be cancelled if EITHER the original stoppingToken OR the timeoutCts token is cancelled.
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

            // We will use the linked token for all subsequent async operations.
            var linkedToken = linkedCts.Token;

            try
            {
                var task = await _taskQueue.DequeueAsync(linkedToken);

                using (var scope = _serviceProvider.CreateScope())
                {
                    _logger.LogInformation("Worker is processing task for Analysis ID: {AnalysisId}", task.AnalysisId);
                    
                    var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
                    var metadataService = scope.ServiceProvider.GetRequiredService<IMetadataReaderService>();
                    var statisticService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();

                    var assemblyPath = await storageService.FindAssemblyFilePathAsync(task.AnalysisId);

                    if (assemblyPath == null)
                    {
                        _logger.LogWarning("No assembly file found for Analysis ID: {AnalysisId}. Skipping task.", task.AnalysisId);
                        continue;
                    }

                    var metadata = await metadataService.GetAssemblyMetadataAsync(assemblyPath, linkedToken);
                    _logger.LogInformation("Extracted metadata for Analysis ID: {AnalysisId}", task.AnalysisId);
                    
                    await storageService.SaveDataAsync(task.AnalysisId, metadata, "metadata.json", linkedToken);
                    _logger.LogInformation("Saved metadata for Analysis ID: {AnalysisId}", task.AnalysisId);
                }
            }
            catch (OperationCanceledException)
            {
                // This exception is expected when the application is shutting down.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing a task.");
            }
        }
    }
}