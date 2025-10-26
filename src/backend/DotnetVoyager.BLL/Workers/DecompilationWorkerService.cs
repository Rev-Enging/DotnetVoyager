using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Enums;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Options;
using DotnetVoyager.BLL.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;

namespace DotnetVoyager.BLL.Workers;

public class DecompilationWorkerService : BackgroundService
{
    private readonly ILogger<DecompilationWorkerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDecompilationTaskQueue _taskQueue;
    private readonly int _concurrentWorkers;
    private readonly int _timeoutMinutes;

    public DecompilationWorkerService(
        IOptions<WorkerOptions> workerOptions,
        ILogger<DecompilationWorkerService> logger,
        IServiceProvider serviceProvider,
        IDecompilationTaskQueue taskQueue)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _taskQueue = taskQueue;
        _concurrentWorkers = 3;
        _timeoutMinutes = workerOptions.Value.AnalysisTimeoutMinutes;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Decompilation Worker Service is running with {WorkerCount} concurrent workers.", _concurrentWorkers);

        var workerTasks = new List<Task>();
        for (int i = 0; i < _concurrentWorkers; i++)
        {
            workerTasks.Add(ProcessQueueAsync(stoppingToken));
        }
        
        await Task.WhenAll(workerTasks);
        _logger.LogInformation("Decompilation Worker Service is stopping.");
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(_timeoutMinutes));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);
            var linkedToken = linkedCts.Token;

            AnalysisTask? task = null;

            try
            {
                task = await _taskQueue.DequeueAsync(linkedToken);

                using (var scope = _serviceProvider.CreateScope())
                {
                    await ProcessFullDecompilationAsync(scope, task.AnalysisId, linkedToken);
                }
            }
            catch (OperationCanceledException)
            {
                if (task != null && timeoutCts.IsCancellationRequested)
                {
                    _logger.LogWarning("Decompilation task timed out for Analysis ID: {AnalysisId}.", task.AnalysisId);
                    await UpdateZipStatusOnFailureAsync(task.AnalysisId, "Decompilation task timed out.", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing decompilation task ID: {AnalysisId}", task?.AnalysisId ?? "Unknown");
                if (task != null)
                {
                    await UpdateZipStatusOnFailureAsync(task.AnalysisId, ex.Message, CancellationToken.None);
                }
            }
        }
    }

    private async Task ProcessFullDecompilationAsync(IServiceScope scope, string analysisId, CancellationToken linkedToken)
    {
        var statusService = scope.ServiceProvider.GetRequiredService<IAnalysisStatusService>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        // You will need to create this service
        var fullDecompilerService = scope.ServiceProvider.GetRequiredService<IFullDecompilationService>();

        _logger.LogInformation("Starting full decompilation for Analysis ID: {AnalysisId}", analysisId);

        // 1. Set ZipStatus to Processing
        await statusService.SetZipStatusAsync(analysisId, ZipGenerationStatus.Processing, null, linkedToken);

        var assemblyPath = await storageService.FindAssemblyFilePathAsync(analysisId, linkedToken);
        if (assemblyPath == null)
        {
            _logger.LogWarning("No assembly file found for decompilation: {AnalysisId}", analysisId);
            await statusService.SetZipStatusAsync(analysisId, ZipGenerationStatus.Failed, "Assembly file not found.", linkedToken);
            return;
        }

        var analysisFolder = storageService.GetAnalysisDirectoryPath(analysisId);
        var tempSourcePath = Path.Combine(analysisFolder, "decompiled_source_temp");
        // Define this constant, e.g., "decompiled_source.zip"
        var zipPath = Path.Combine(analysisFolder, ProjectConstants.DecompiledZipFileName);

        try
        {
            if (Directory.Exists(tempSourcePath)) Directory.Delete(tempSourcePath, true);
            Directory.CreateDirectory(tempSourcePath);

            // 2. Perform heavy work (This is your custom logic)
            _logger.LogInformation("Decompiling to {TempPath}...", tempSourcePath);
            await fullDecompilerService.DecompileProjectAsync(assemblyPath, tempSourcePath, linkedToken);

            // 3. Create Zip
            _logger.LogInformation("Zipping decompiled source to {ZipPath}...", zipPath);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(tempSourcePath, zipPath, CompressionLevel.Fastest, false);

            // 4. Set status to Completed
            await statusService.SetZipStatusAsync(analysisId, ZipGenerationStatus.Completed, null, linkedToken);
            _logger.LogInformation("Successfully created decompiled ZIP for Analysis ID: {AnalysisId}", analysisId);
        }
        finally
        {
            // 5. Clean up temp folder regardless of success/failure
            if (Directory.Exists(tempSourcePath))
            {
                try { Directory.Delete(tempSourcePath, true); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to cleanup temp source directory: {TempPath}", tempSourcePath); }
            }
        }
    }

    private async Task UpdateZipStatusOnFailureAsync(string analysisId, string errorMessage, CancellationToken token)
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var statusService = scope.ServiceProvider.GetRequiredService<IAnalysisStatusService>();
                await statusService.SetZipStatusAsync(analysisId, ZipGenerationStatus.Failed, errorMessage, token);
                _logger.LogWarning("Set ZipStatus to 'Failed' for Analysis ID: {AnalysisId}", analysisId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRITICAL: Failed to update ZipStatus to 'Failed' for Analysis ID: {AnalysisId}", analysisId);
        }
    }
}
