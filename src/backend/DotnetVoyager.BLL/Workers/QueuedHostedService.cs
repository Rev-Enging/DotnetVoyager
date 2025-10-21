using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Enums;
using DotnetVoyager.BLL.Models;
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
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);
            var linkedToken = linkedCts.Token;

            AnalysisTask? task = null; // <-- Визначте task поза try-блоком

            try
            {
                // 1. Отримати завдання
                task = await _taskQueue.DequeueAsync(linkedToken);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
                    var statusService = scope.ServiceProvider.GetRequiredService<IAnalysisStatusService>();
                    var metadataService = scope.ServiceProvider.GetRequiredService<IMetadataReaderService>();
                    var statisticService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
                    var structureService = scope.ServiceProvider.GetRequiredService<IStructureAnalyzerService>();
                    var analysisId = task.AnalysisId;

                    // Set "Processing" status
                    await statusService.SetStatusAsync(analysisId, AnalysisStatus.Processing, null, linkedToken);
                    _logger.LogInformation(
                        "Worker is processing task. Status set to 'Processing' for Analysis ID: {AnalysisId}", analysisId);

                    // 3. Виконати роботу
                    var assemblyPath = await storageService.FindAssemblyFilePathAsync(analysisId, linkedToken);

                    if (assemblyPath == null)
                    {
                        _logger.LogWarning(
                            "No assembly file found for Analysis ID: {AnalysisId}. Setting status to Failed.", analysisId);
                        await statusService.SetStatusAsync(analysisId, AnalysisStatus.Failed, "Assembly file not found.", linkedToken);
                        continue;
                    }

                    // Perform analysis
                    var metadata = await metadataService.GetAssemblyMetadataAsync(assemblyPath, linkedToken);
                    await storageService.SaveDataAsync(analysisId, metadata, ProjectConstants.AnalysisMetadataFileName, linkedToken);
                    _logger.LogInformation("Saved metadata for Analysis ID: {AnalysisId}", analysisId);

                    var statistics = await statisticService.GetAssemblyStatisticsAsync(assemblyPath);
                    await storageService.SaveDataAsync(analysisId, statistics, ProjectConstants.AnalysisStatisticsFileName, linkedToken);
                    _logger.LogInformation("Saved statistics for Analysis ID: {AnalysisId}", analysisId);

                    var structure = await structureService.AnalyzeStructureAsync(assemblyPath);
                    await storageService.SaveDataAsync(analysisId, structure, ProjectConstants.AnalysisNamespaceStructureFileName, linkedToken);
                    _logger.LogInformation("Saved structure for Analysis ID: {AnalysisId}", analysisId);

                    // Set "Completed" status
                    await statusService.SetStatusAsync(task.AnalysisId, AnalysisStatus.Completed, null, linkedToken);
                    _logger.LogInformation("Successfully processed task. Status set to 'Completed' for Analysis ID: {AnalysisId}", task.AnalysisId);
                }
            }
            catch (OperationCanceledException)
            {
                // Якщо скасування було через тайм-аут - це помилка
                if (task != null && timeoutCts.IsCancellationRequested)
                {
                    _logger.LogWarning("Task timed out for Analysis ID: {AnalysisId}. Setting status to Failed.", task.AnalysisId);
                    await UpdateStatusOnFailureAsync(task.AnalysisId, "Analysis task timed out.", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing task ID: {AnalysisId}", task?.AnalysisId ?? "Unknown");
                if (task != null)
                {
                    // Set status to Failed
                    await UpdateStatusOnFailureAsync(task.AnalysisId, ex.Message, CancellationToken.None);
                }
            }
        }
    }

    private async Task UpdateStatusOnFailureAsync(string analysisId, string errorMessage, CancellationToken token)
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var statusService = scope.ServiceProvider.GetRequiredService<IAnalysisStatusService>();
                await statusService.SetStatusAsync(analysisId, AnalysisStatus.Failed, errorMessage, token);
                _logger.LogWarning("Set status to 'Failed' for Analysis ID: {AnalysisId}", analysisId);
            }
        }
        catch (Exception ex)
        {
            // Якщо навіть оновлення статусу впало, ми нічого не можемо зробити, окрім логування.
            _logger.LogError(ex, "CRITICAL: Failed to update status to 'Failed' for Analysis ID: {AnalysisId}", analysisId);
        }
    }
}