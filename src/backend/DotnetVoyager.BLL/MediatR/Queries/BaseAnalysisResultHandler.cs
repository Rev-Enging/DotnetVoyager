using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Exceptions;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.DAL.Enums;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.Common;

public abstract class BaseAnalysisResultHandler<TQuery, TResult> : IRequestHandler<TQuery, Result<TResult>>
    where TQuery : IRequest<Result<TResult>>
    where TResult : class
{
    private readonly IStorageService _storageService;
    private readonly IAnalysisStatusService _statusService;
    private readonly ILogger _logger;

    protected BaseAnalysisResultHandler(
        IStorageService storageService,
        IAnalysisStatusService statusService,
        ILogger logger)
    {
        _storageService = storageService;
        _statusService = statusService;
        _logger = logger;
    }

    public abstract Task<Result<TResult>> Handle(TQuery request, CancellationToken cancellationToken);

    protected async Task<Result<TResult>> ProcessAnalysisResultAsync(
        string analysisId,
        string stepName,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            var stepStatus = await _statusService.GetStepStatusAsync(
                analysisId,
                stepName,
                cancellationToken);

            if (stepStatus.Status == AnalysisStepStatus.Failed)
            {
                return Result.Fail(new StepFailedError(stepName, stepStatus.ErrorMessage));
            }

            if (stepStatus.Status != AnalysisStepStatus.Completed)
            {
                return Result.Fail(new StepNotCompletedError(
                    stepName,
                    stepStatus.Status,
                    stepStatus.ErrorMessage));
            }

            var result = await _storageService.ReadDataAsync<TResult>(
                analysisId,
                fileName,
                cancellationToken);

            if (result == null)
            {
                _logger.LogError("{StepName} file missing for completed analysis {AnalysisId}", stepName, analysisId);
                return Result.Fail(new Error($"Internal error: {stepName} step completed but file is missing."));
            }

            return Result.Ok(result);
        }
        catch (AnalysisNotFoundException ex)
        {
            _logger.LogWarning(ex, "{StepName} requested for non-existent analysis: {AnalysisId}", stepName, ex.AnalysisId);
            return Result.Fail(new AnalysisNotFound(ex.AnalysisId));
        }
    }
}