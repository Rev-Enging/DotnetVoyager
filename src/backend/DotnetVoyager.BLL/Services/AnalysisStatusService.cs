using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Exceptions;
using DotnetVoyager.DAL.Data;
using DotnetVoyager.DAL.Entities;
using DotnetVoyager.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.Services;

public interface IAnalysisStatusService
{
    Task<bool> AnalysisExistsAsync(
        string analysisId, 
        CancellationToken token = default);

    Task<AnalysisStatusDto> GetStatusAsync(
        string analysisId,
        CancellationToken token = default);

    Task<StepStatusDto> GetStepStatusAsync(
        string analysisId,
        string stepName,
        CancellationToken token = default);

    Task CreateAnalysisAsync(
        string analysisId,
        string originalFileName,
        CancellationToken token = default);

    Task UpdateOverallStatusAsync(
        string analysisId,
        CancellationToken token = default);
}

public class DatabaseAnalysisStatusService : IAnalysisStatusService
{
    private readonly AnalysisDbContext _db;
    private readonly IAnalysisStepService _stepService;
    private readonly ILogger<DatabaseAnalysisStatusService> _logger;

    public DatabaseAnalysisStatusService(
        AnalysisDbContext db,
        IAnalysisStepService stepService,
        ILogger<DatabaseAnalysisStatusService> logger)
    {
        _db = db;
        _stepService = stepService;
        _logger = logger;
    }

    public async Task<bool> AnalysisExistsAsync(string analysisId, CancellationToken token = default)
    {
        return await _db.AnalysisStatuses
            .AnyAsync(a => a.AnalysisId == analysisId, token);
    }

    public async Task<AnalysisStatusDto> GetStatusAsync(
        string analysisId,
        CancellationToken token = default)
    {
        var analysis = await _db.AnalysisStatuses
            .Include(a => a.Steps)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AnalysisId == analysisId, token);

        if (analysis == null)
        {
            throw new AnalysisNotFoundException(analysisId);
        }

        return new AnalysisStatusDto
        {
            AnalysisId = analysis.AnalysisId,
            OverallStatus = analysis.OverallStatus,
            LastUpdatedUtc = analysis.LastUpdatedUtc,
            Steps = analysis.Steps.Select(s => new StepStatusDto
            {
                StepName = s.StepName,
                Status = s.Status,
                ErrorMessage = s.ErrorMessage,
                StartedUtc = s.StartedUtc,
                CompletedUtc = s.CompletedUtc
            }).ToList()
        };
    }

    public async Task<StepStatusDto> GetStepStatusAsync(
        string analysisId,
        string stepName,
        CancellationToken token = default)
    {
        var analysisExists = await _db.AnalysisStatuses
            .AnyAsync(a => a.AnalysisId == analysisId, token);

        AnalysisNotFoundException.ThrowIfAnalysisNotFound(analysisExists, analysisId);

        AnalysisStepNotExistException.ThrowIfStepNotExist(stepName);

        var step = await _db.AnalysisSteps
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.AnalysisId == analysisId && s.StepName == stepName,
                token);

        if (step == null)
        {
            return new StepStatusDto
            {
                StepName = stepName,
                Status = AnalysisStepStatus.NotProcessed,
                ErrorMessage = null,
                StartedUtc = null,
                CompletedUtc = null,
            };
        }

        return new StepStatusDto
        {
            StepName = step.StepName,
            Status = step.Status,
            ErrorMessage = step.ErrorMessage,
            StartedUtc = step.StartedUtc,
            CompletedUtc = step.CompletedUtc,
        };
    }

    public async Task CreateAnalysisAsync(
        string analysisId,
        string originalFileName,
        CancellationToken token = default)
    {
        var analysis = new AnalysisStatus
        {
            AnalysisId = analysisId,
            OriginalFileName = originalFileName,
            OverallStatus = AnalysisOverallStatus.Pending,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        };

        _db.AnalysisStatuses.Add(analysis);
        await _db.SaveChangesAsync(token);

        // Initialize required steps
        await _stepService.InitializeStepsAsync(analysisId, token);
    }

    public async Task UpdateOverallStatusAsync(
        string analysisId,
        CancellationToken token = default)
    {
        var steps = await _stepService.GetStepsAsync(analysisId, token);

        var allRequiredCompleted = steps
            .All(s => s.Status == AnalysisStepStatus.Completed);

        var anyRequiredFailed = steps
            .Any(s => s.Status == AnalysisStepStatus.Failed);

        var anyProcessing = steps.Any(s => s.Status == AnalysisStepStatus.Processing);

        AnalysisOverallStatus newStatus;

        if (anyProcessing)
        {
            newStatus = AnalysisOverallStatus.Processing;
        }
        else if (anyRequiredFailed)
        {
            newStatus = AnalysisOverallStatus.Failed;
        }
        else if (allRequiredCompleted)
        {
            newStatus = AnalysisOverallStatus.Completed;
        }
        else
        {
            // Some completed, some pending
            var anyCompleted = steps.Any(s => s.Status == AnalysisStepStatus.Completed);
            newStatus = anyCompleted
                ? AnalysisOverallStatus.PartiallyCompleted
                : AnalysisOverallStatus.Pending;
        }

        await _db.AnalysisStatuses
            .Where(a => a.AnalysisId == analysisId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.OverallStatus, newStatus)
                .SetProperty(a => a.LastUpdatedUtc, DateTime.UtcNow),
                cancellationToken: token);

        _logger.LogInformation(
            "Updated overall status to {Status} for {AnalysisId}",
            newStatus, analysisId);
    }
}
