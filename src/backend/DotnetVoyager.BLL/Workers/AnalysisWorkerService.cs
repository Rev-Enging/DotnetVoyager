using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Options;
using DotnetVoyager.BLL.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetVoyager.BLL.Workers;

/// <summary>
/// Unified worker that processes ALL analysis steps (required and optional)
/// using the orchestrator pattern
/// </summary>
public class AnalysisWorkerService : BackgroundService
{
    private readonly ILogger<AnalysisWorkerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAnalysisTaskQueue _taskQueue;
    private readonly WorkerOptions _workerOptions;

    public AnalysisWorkerService(
        IOptions<WorkerOptions> workerOptions,
        ILogger<AnalysisWorkerService> logger,
        IServiceProvider serviceProvider,
        IAnalysisTaskQueue taskQueue)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _taskQueue = taskQueue;
        _workerOptions = workerOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var concurrentWorkers = _workerOptions.AnalysisConcurrentWorkers;

        _logger.LogInformation(
            "Analysis Worker Service starting with {WorkerCount} concurrent workers",
            concurrentWorkers);

        var workerTasks = Enumerable.Range(0, concurrentWorkers)
            .Select(_ => ProcessQueueAsync(stoppingToken))
            .ToList();

        await Task.WhenAll(workerTasks);

        _logger.LogInformation("Analysis Worker Service stopped");
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var timeoutCts = new CancellationTokenSource(
                TimeSpan.FromMinutes(_workerOptions.AnalysisTimeoutMinutes));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                stoppingToken, timeoutCts.Token);

            AnalysisTask? task = null;

            try
            {
                // 1. Dequeue task
                task = await _taskQueue.DequeueAsync(linkedCts.Token);

                _logger.LogInformation(
                    "Worker picked up task for Analysis ID: {AnalysisId}",
                    task.AnalysisId);

                // 2. Process ALL steps for this analysis using orchestrator
                await ProcessAnalysisAsync(task.AnalysisId, linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                if (task != null)
                {
                    _logger.LogWarning(
                        "Task timed out for Analysis ID: {AnalysisId}",
                        task.AnalysisId);
                    await HandleTimeoutAsync(task.AnalysisId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker shutting down gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error processing Analysis ID: {AnalysisId}",
                    task?.AnalysisId ?? "Unknown");
            }
        }
    }

    private async Task ProcessAnalysisAsync(string analysisId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IAnalysisOrchestrator>();
        var statusService = scope.ServiceProvider.GetRequiredService<IAnalysisStatusService>();

        try
        {
            // Execute steps one by one until no more pending steps
            bool hasMoreSteps = true;

            while (hasMoreSteps && !cancellationToken.IsCancellationRequested)
            {
                hasMoreSteps = await orchestrator.ExecuteNextStepAsync(
                    analysisId, cancellationToken);

                // Update overall status after each step
                await statusService.UpdateOverallStatusAsync(analysisId, cancellationToken);
            }

            _logger.LogInformation(
                "Completed processing all pending steps for Analysis ID: {AnalysisId}",
                analysisId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during orchestrated analysis for {AnalysisId}",
                analysisId);

            // Update overall status even on failure
            await statusService.UpdateOverallStatusAsync(analysisId, CancellationToken.None);
        }
    }

    private async Task HandleTimeoutAsync(string analysisId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var statusService = scope.ServiceProvider.GetRequiredService<IAnalysisStatusService>();

            // The orchestrator already marked the current step as failed
            // Just update the overall status
            await statusService.UpdateOverallStatusAsync(analysisId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "CRITICAL: Failed to handle timeout for Analysis ID: {AnalysisId}",
                analysisId);
        }
    }
}
