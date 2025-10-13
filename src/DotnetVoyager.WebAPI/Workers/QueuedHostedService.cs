using System.Runtime;

namespace DotnetVoyager.WebAPI.Workers;

public class QueuedHostedService : BackgroundService
{
    private readonly ILogger<QueuedHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBackgroundTaskQueue _taskQueue;

    // Define the number of concurrent workers (slots).
    // Let's make this configurable later.
    private const int MaxConcurrentWorkers = 5;

    public QueuedHostedService(
        ILogger<QueuedHostedService> logger,
        IServiceProvider serviceProvider,
        IBackgroundTaskQueue taskQueue)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _taskQueue = taskQueue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Queued Hosted Service is running with {WorkerCount} concurrent workers.", MaxConcurrentWorkers);

        // Create a list to hold all our worker tasks.
        var workerTasks = new List<Task>();

        // Start N workers.
        for (int i = 0; i < MaxConcurrentWorkers; i++)
        {
            // Each worker is a separate Task that runs the ProcessQueueAsync method.
            workerTasks.Add(ProcessQueueAsync(stoppingToken));
        }

        // Wait for all worker tasks to complete.
        // This will happen when the application is shutting down.
        await Task.WhenAll(workerTasks);

        _logger.LogInformation("Queued Hosted Service is stopping.");
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(_settings.AnalysisTimeoutMinutes));

            // ✅ Create a new, LINKED token source.
            // It will be cancelled if EITHER the original stoppingToken OR the timeoutCts token is cancelled.
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

            // We will use the linked token for all subsequent async operations.
            var linkedToken = linkedCts.Token;

            try
            {
                // Each worker independently dequeues a task from the shared queue.
                // The queue itself is thread-safe, so they won't interfere with each other.
                var task = await _taskQueue.DequeueAsync(linkedToken);

                // Create a new dependency injection scope for each task processing.
                // This is CRUCIAL for concurrency to prevent issues with shared services (like DbContext).
                using (var scope = _serviceProvider.CreateScope())
                {
                    var analysisService = scope.ServiceProvider.GetRequiredService<IAssemblyAnalysisService>();
                    _logger.LogInformation("Worker is processing task for Analysis ID: {AnalysisId}", task.AnalysisId);

                    // The actual work is performed here.
                    await analysisService.PerformAnalysisAsync(task);
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