using DotnetVoyager.BLL.Constants;
using DotnetVoyager.DAL.Data;
using DotnetVoyager.DAL.Entities;
using DotnetVoyager.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.Services;

public interface IAnalysisStepService
{
    /// <summary>
    /// Initialize all required steps for a new analysis
    /// </summary>
    Task InitializeStepsAsync(string analysisId, CancellationToken token = default);

    /// <summary>
    /// Add a specific optional step (like ZipGeneration)
    /// </summary>
    Task<bool> TryAddOptionalStepAsync(
        string analysisId,
        string stepName,
        CancellationToken token = default);

    /// <summary>
    /// Get next pending step to execute
    /// </summary>
    Task<AnalysisStep?> GetNextPendingStepAsync(
        string analysisId,
        CancellationToken token = default);

    /// <summary>
    /// Mark step as started
    /// </summary>
    Task MarkStepAsProcessingAsync(
        int stepId,
        CancellationToken token = default);

    /// <summary>
    /// Mark step as completed
    /// </summary>
    Task MarkStepAsCompletedAsync(
        int stepId,
        CancellationToken token = default);

    /// <summary>
    /// Mark step as failed
    /// </summary>
    Task MarkStepAsFailedAsync(
        int stepId,
        string errorMessage,
        CancellationToken token = default);

    /// <summary>
    /// Get all steps for an analysis
    /// </summary>
    Task<List<AnalysisStep>> GetStepsAsync(
        string analysisId,
        CancellationToken token = default);

    /// <summary>
    /// Check if all required steps are completed
    /// </summary>
    Task<bool> AreAllRequiredStepsCompletedAsync(
        string analysisId,
        CancellationToken token = default);

    Task<bool> ResetStepForRetryAsync(
        string analysisId,
        string stepName,
        CancellationToken token = default);
}

public class AnalysisStepService : IAnalysisStepService
{
    private readonly AnalysisDbContext _db;
    private readonly ILogger<AnalysisStepService> _logger;

    public AnalysisStepService(
        AnalysisDbContext db,
        ILogger<AnalysisStepService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InitializeStepsAsync(string analysisId, CancellationToken token = default)
    {
        var steps = AnalysisStepNames.GetRequiredSteps()
            .Select((stepName, index) => new AnalysisStep
            {
                AnalysisId = analysisId,
                StepName = stepName,
                Status = AnalysisStepStatus.Pending,
            }).ToList();


        _db.AnalysisSteps.AddRange(steps);
        await _db.SaveChangesAsync(token);

        _logger.LogInformation(
            "Initialized {Count} steps for analysis {AnalysisId}",
            steps.Count, analysisId);
    }

    public async Task<bool> TryAddOptionalStepAsync(
        string analysisId,
        string stepName,
        CancellationToken token = default)
    {
        // Check if step already exists
        var exists = await _db.AnalysisSteps
            .AnyAsync(s => s.AnalysisId == analysisId && s.StepName == stepName, token);

        if (exists)
        {
            _logger.LogInformation(
                "Step {StepName} already exists for {AnalysisId}",
                stepName, analysisId);
            return false;
        }

        // Only allow if all required steps are completed
        var allRequiredCompleted = await AreAllRequiredStepsCompletedAsync(analysisId, token);
        if (!allRequiredCompleted)
        {
            _logger.LogWarning(
                "Cannot add optional step {StepName} - required steps not completed for {AnalysisId}",
                stepName, analysisId);
            return false;
        }

        var step = new AnalysisStep
        {
            AnalysisId = analysisId,
            StepName = stepName,
            Status = AnalysisStepStatus.Pending,
        };

        _db.AnalysisSteps.Add(step);
        await _db.SaveChangesAsync(token);

        _logger.LogInformation(
            "Added optional step {StepName} for {AnalysisId}",
            stepName, analysisId);
        return true;
    }

    public async Task<AnalysisStep?> GetNextPendingStepAsync(
        string analysisId,
        CancellationToken token = default)
    {
        return await _db.AnalysisSteps
            .AsNoTracking()
            .Where(s => s.AnalysisId == analysisId && s.Status == AnalysisStepStatus.Pending)
            .FirstOrDefaultAsync(token);
    }

    public async Task MarkStepAsProcessingAsync(int stepId, CancellationToken token = default)
    {
        await _db.AnalysisSteps
            .Where(s => s.Id == stepId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, AnalysisStepStatus.Processing)
                .SetProperty(s => s.StartedUtc, DateTime.UtcNow),
                cancellationToken: token);
    }

    public async Task MarkStepAsCompletedAsync(int stepId, CancellationToken token = default)
    {
        await _db.AnalysisSteps
            .Where(s => s.Id == stepId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, AnalysisStepStatus.Completed)
                .SetProperty(s => s.CompletedUtc, DateTime.UtcNow),
                cancellationToken: token);
    }

    public async Task MarkStepAsFailedAsync(
        int stepId,
        string errorMessage,
        CancellationToken token = default)
    {
        await _db.AnalysisSteps
            .Where(s => s.Id == stepId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, AnalysisStepStatus.Failed)
                .SetProperty(s => s.ErrorMessage, errorMessage)
                .SetProperty(s => s.CompletedUtc, DateTime.UtcNow),
                cancellationToken: token);
    }

    public async Task<List<AnalysisStep>> GetStepsAsync(
        string analysisId,
        CancellationToken token = default)
    {
        return await _db.AnalysisSteps
            .AsNoTracking()
            .Where(s => s.AnalysisId == analysisId)
            .ToListAsync(token);
    }

    public async Task<bool> AreAllRequiredStepsCompletedAsync(
        string analysisId,
        CancellationToken token = default)
    {
        var hasIncompleteRequired = await _db.AnalysisSteps
            .AsNoTracking()
            .AnyAsync(s =>
                s.AnalysisId == analysisId &&
                s.Status != AnalysisStepStatus.Completed,
                token);

        return !hasIncompleteRequired;
    }

    public async Task<bool> ResetStepForRetryAsync(
        string analysisId,
        string stepName,
        CancellationToken token = default)
    {
        var step = await _db.AnalysisSteps
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.AnalysisId == analysisId && s.StepName == stepName,
                token);

        if (step == null)
        {
            _logger.LogWarning(
                "Cannot reset step {StepName} - not found for {AnalysisId}",
                stepName, analysisId);
            return false;
        }

        if (step.Status != AnalysisStepStatus.Failed)
        {
            _logger.LogWarning(
                "Cannot reset step {StepName} - status is {Status}, not Failed",
                stepName, step.Status);
            return false;
        }

        step.Status = AnalysisStepStatus.Pending;
        step.ErrorMessage = null;
        step.StartedUtc = null;
        step.CompletedUtc = null;

        await _db.SaveChangesAsync(token);

        _logger.LogInformation(
            "Reset step {StepName} for retry for {AnalysisId}",
            stepName, analysisId);

        return true;
    }
}
