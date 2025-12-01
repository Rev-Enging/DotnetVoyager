using DotnetVoyager.BLL.Services.AnalysisSteps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.Services;

/// <summary>
/// Orchestrates execution of analysis steps
/// </summary>
public interface IAnalysisOrchestrator
{
    /// <summary>
    /// Execute the next pending step for an analysis
    /// </summary>
    Task<bool> ExecuteNextStepAsync(
        string analysisId,
        CancellationToken cancellationToken);
}

public class AnalysisOrchestrator : IAnalysisOrchestrator
{
    private readonly IAnalysisStepService _stepService;
    private readonly IStorageService _storageService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnalysisOrchestrator> _logger;

    public AnalysisOrchestrator(
        IAnalysisStepService stepService,
        IStorageService storageService,
        IServiceProvider serviceProvider,
        ILogger<AnalysisOrchestrator> logger)
    {
        _stepService = stepService;
        _storageService = storageService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<bool> ExecuteNextStepAsync(
        string analysisId,
        CancellationToken cancellationToken)
    {
        // 1. Get next pending step
        var step = await _stepService.GetNextPendingStepAsync(analysisId, cancellationToken);
        if (step == null)
        {
            _logger.LogInformation("No pending steps for {AnalysisId}", analysisId);
            return false; // No more steps
        }

        // 2. Find assembly file
        var context = await _storageService.CreateAnalysisContextAsync(analysisId, cancellationToken);
        if (context == null)
        {
            _logger.LogError("Assembly file not found for {AnalysisId}", analysisId);
            await _stepService.MarkStepAsFailedAsync(
                step.Id, "Assembly file not found", cancellationToken);
            return false;
        }

        // 3. Get the step executor
        var stepExecutor = GetStepExecutor(step.StepName);
        if (stepExecutor == null)
        {
            _logger.LogError("No executor found for step {StepName}", step.StepName);
            await _stepService.MarkStepAsFailedAsync(
                step.Id, $"No executor found for step {step.StepName}", cancellationToken);
            return false;
        }

        // 4. Mark as processing
        await _stepService.MarkStepAsProcessingAsync(step.Id, cancellationToken);

        _logger.LogInformation(
            "Executing step {StepName} (ID: {StepId}) for {AnalysisId}",
            step.StepName, step.Id, analysisId);

        // 5. Execute the step
        bool success = await stepExecutor.ExecuteAsync(
            context, cancellationToken);

        // 6. Update status
        if (success)
        {
            await _stepService.MarkStepAsCompletedAsync(step.Id, cancellationToken);
            _logger.LogInformation(
                "Step {StepName} completed for {AnalysisId}",
                step.StepName, analysisId);
        }
        else
        {
            await _stepService.MarkStepAsFailedAsync(
                step.Id, "Step execution failed", cancellationToken);
            _logger.LogWarning(
                "Step {StepName} failed for {AnalysisId}",
                step.StepName, analysisId);
        }

        return success;
    }

    private IAnalysisStep? GetStepExecutor(string stepName)
    {
        // Use service provider to get the correct step implementation
        var allSteps = _serviceProvider.GetServices<IAnalysisStep>();
        return allSteps.FirstOrDefault(s => s.StepName == stepName);
    }
}

